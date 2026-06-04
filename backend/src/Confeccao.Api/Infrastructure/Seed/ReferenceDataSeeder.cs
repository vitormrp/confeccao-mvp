using Confeccao.Domain.Entities;
using Confeccao.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Confeccao.Api.Infrastructure.Seed;

/// <summary>
/// Idempotent reference-data seeder. Runs on every startup; only inserts rows that
/// don't already exist (matched by Name for colors/models/users). Safe to run repeatedly.
///
/// Domain reference data (colors, models, model_stages) is always seeded so the system
/// is functional. Sample users + default prices are also seeded so operator flows are
/// testable without manually creating users — when the Cadastros CRUD UI lands the
/// manager will edit these.
/// </summary>
public static class ReferenceDataSeeder
{
    public static async Task SeedAsync(ConfeccaoDbContext db, CancellationToken ct = default)
    {
        await SeedColorsAsync(db, ct);
        await SeedModelsAndStagesAsync(db, ct);
        var userByRole = await SeedUsersAsync(db, ct);
        await SeedPricesAsync(db, userByRole, ct);
        await db.SaveChangesAsync(ct);
    }

    private static async Task SeedColorsAsync(ConfeccaoDbContext db, CancellationToken ct)
    {
        var defaults = new[]
        {
            ("Verde militar", "#4A5E3A", false),
            ("Marrom",        "#6B4C2A", false),
            ("Vinho",         "#722F37", false),
            ("Branco",        "#F5F5F0", true),
            ("Preto",         "#1A1A1A", false),
            ("Cru",           "#E8DFC8", false),
            ("Azul petróleo", "#1B4F6A", false),
            ("Azul claro",    "#7BB3D4", true),
        };

        var existing = await db.Colors.Select(c => c.Name).ToListAsync(ct);
        foreach (var (name, hex, hasLining) in defaults)
        {
            if (existing.Contains(name)) continue;
            db.Colors.Add(new Color
            {
                Id = Guid.NewGuid(),
                Name = name,
                HexCode = hex,
                HasLining = hasLining,
                Active = true,
            });
        }
    }

    private static async Task SeedModelsAndStagesAsync(ConfeccaoDbContext db, CancellationToken ct)
    {
        var defaults = new (string Name, int Buttons, StageCode[] Flow)[]
        {
            ("Calça pantalona",          1, [StageCode.Cutting, StageCode.Sewing, StageCode.Washing, StageCode.Buttoning, StageCode.Labeling, StageCode.Pressing]),
            ("Calça reta",               1, [StageCode.Cutting, StageCode.Sewing, StageCode.Washing, StageCode.Buttoning, StageCode.Labeling, StageCode.Pressing]),
            ("Short/bermuda alfaiataria",1, [StageCode.Cutting, StageCode.Sewing, StageCode.Washing, StageCode.Buttoning, StageCode.Labeling, StageCode.Pressing]),
            ("Vestido chemise",          0, [StageCode.Cutting, StageCode.Interfacing, StageCode.Sewing, StageCode.Washing, StageCode.Buttoning, StageCode.Labeling, StageCode.Pressing]),
            ("Vestido midi frielas",     0, [StageCode.Cutting, StageCode.Sewing, StageCode.Washing, StageCode.Buttoning, StageCode.Labeling, StageCode.Pressing]),
            ("Colete alfaiataria",       0, [StageCode.Cutting, StageCode.Sewing, StageCode.Labeling, StageCode.Pressing]),
        };

        var existingModels = await db.Models
            .Include(m => m.Stages)
            .ToDictionaryAsync(m => m.Name, ct);

        foreach (var (name, buttons, flow) in defaults)
        {
            if (existingModels.TryGetValue(name, out var existing))
            {
                // Reseed stages if they don't match (handles future flow tweaks).
                var existingFlow = existing.Stages.OrderBy(s => s.Sequence).Select(s => s.Stage).ToArray();
                if (!existingFlow.SequenceEqual(flow))
                {
                    db.ModelStages.RemoveRange(existing.Stages);
                    foreach (var (stage, idx) in flow.Select((s, i) => (s, i)))
                    {
                        db.ModelStages.Add(new ModelStage
                        {
                            Id = Guid.NewGuid(),
                            ModelId = existing.Id,
                            Stage = stage,
                            Sequence = idx,
                        });
                    }
                }
                continue;
            }

            var modelId = Guid.NewGuid();
            db.Models.Add(new Model
            {
                Id = modelId,
                Name = name,
                ButtonCount = buttons,
                Active = true,
            });
            foreach (var (stage, idx) in flow.Select((s, i) => (s, i)))
            {
                db.ModelStages.Add(new ModelStage
                {
                    Id = Guid.NewGuid(),
                    ModelId = modelId,
                    Stage = stage,
                    Sequence = idx,
                });
            }
        }
    }

    private static async Task<Dictionary<UserRole, Guid>> SeedUsersAsync(ConfeccaoDbContext db, CancellationToken ct)
    {
        // One sample user per role, plus 2 extra Sewing operators to demonstrate multi-user.
        var defaults = new[]
        {
            ("Gerente",       UserRole.Manager),
            ("Cortador",      UserRole.Cutting),
            ("Entretela Op.", UserRole.Interfacing),
            ("Costureira A",  UserRole.Sewing),
            ("Costureira B",  UserRole.Sewing),
            ("Costureira C",  UserRole.Sewing),
            ("Lavanderia",    UserRole.Washing),
            ("Botão Op.",     UserRole.Buttoning),
            ("Etiqueta Op.",  UserRole.Labeling),
            ("Passadeira",    UserRole.Pressing),
        };

        var existing = await db.Users.ToDictionaryAsync(u => u.Name, ct);
        foreach (var (name, role) in defaults)
        {
            if (existing.ContainsKey(name)) continue;
            db.Users.Add(new User
            {
                Id = Guid.NewGuid(),
                Name = name,
                Role = role,
                Active = true,
            });
        }
        await db.SaveChangesAsync(ct);

        // Return one representative user per role for use by the price seeder.
        return await db.Users
            .GroupBy(u => u.Role)
            .Select(g => new { Role = g.Key, Id = g.OrderBy(u => u.Name).First().Id })
            .ToDictionaryAsync(x => x.Role, x => x.Id, ct);
    }

    private static async Task SeedPricesAsync(
        ConfeccaoDbContext db,
        Dictionary<UserRole, Guid> userByRole,
        CancellationToken ct)
    {
        // Default prices — placeholders. Once the Cadastros CRUD UI lands the manager
        // will edit these. We only insert if no price exists for the user yet so
        // re-runs don't pile up duplicates.
        var defaults = new (UserRole Role, Action<Price> Configure, Action<List<PriceTier>>? Tiers)[]
        {
            (UserRole.Cutting, p =>
            {
                p.Amount = 0;
            }, tiers =>
            {
                tiers.Add(new PriceTier { UpToQuantity = 50,  Amount = 3.00m });
                tiers.Add(new PriceTier { UpToQuantity = 200, Amount = 2.50m });
                tiers.Add(new PriceTier { UpToQuantity = int.MaxValue, Amount = 2.00m });
            }),
            (UserRole.Interfacing, p =>
            {
                p.Amount = 1.50m;
            }, null),
            (UserRole.Sewing, p =>
            {
                p.Amount = 8.00m;
                p.LiningExtra = 2.00m;
                p.InterfacingExtra = 1.50m;
            }, null),
            (UserRole.Washing, p =>
            {
                p.Amount = 1.20m;
            }, null),
            (UserRole.Buttoning, p =>
            {
                p.Amount = 0;
                p.CoveredButtonPrice = 0.80m;
                p.ReadyButtonPrice = 0.30m;
            }, null),
            (UserRole.Labeling, p =>
            {
                p.Amount = 0.50m;
            }, null),
            (UserRole.Pressing, p =>
            {
                p.Amount = 1.00m;
            }, null),
        };

        var usersWithPrices = await db.Prices.Select(p => p.UserId).Distinct().ToListAsync(ct);
        var seedTime = DateTimeOffset.UtcNow;

        foreach (var (role, configure, tiers) in defaults)
        {
            if (!userByRole.TryGetValue(role, out var userId)) continue;
            if (usersWithPrices.Contains(userId)) continue;

            var price = new Price
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                EffectiveFrom = seedTime,
                Note = "Default seed price — adjust via Cadastros.",
            };
            configure(price);

            if (tiers != null)
            {
                var list = new List<PriceTier>();
                tiers(list);
                foreach (var tier in list)
                {
                    tier.Id = Guid.NewGuid();
                    tier.PriceId = price.Id;
                    price.Tiers.Add(tier);
                }
            }

            db.Prices.Add(price);
        }
    }
}
