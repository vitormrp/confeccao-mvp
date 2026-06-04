using Confeccao.Api.Infrastructure;
using Confeccao.Domain.Entities;
using Confeccao.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Confeccao.Api.Features.Cadastros;

public static class CadastrosEndpoints
{
    public static void MapCadastrosEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/cadastros");

        group.MapGet("/colors", GetColors);
        group.MapPost("/colors", CreateColor);
        group.MapPut("/colors/{id:guid}", UpdateColor);

        group.MapGet("/models", GetModels);
        group.MapPost("/models", CreateModel);
        group.MapPut("/models/{id:guid}", UpdateModel);

        group.MapGet("/users", GetUsers);
        group.MapPost("/users", CreateUser);
        group.MapPut("/users/{id:guid}", UpdateUser);

        group.MapGet("/prices", GetPrices);
    }

    // ---- Colors --------------------------------------------------------------

    private static async Task<IResult> GetColors(ConfeccaoDbContext db, CancellationToken ct)
    {
        var colors = await db.Colors
            .OrderBy(c => c.Name)
            .Select(c => new ColorDto(c.Id, c.Name, c.HexCode, c.HasLining, c.Active))
            .ToListAsync(ct);
        return Results.Ok(colors);
    }

    private static async Task<IResult> CreateColor(
        ColorUpsertRequest request,
        ConfeccaoDbContext db,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return Results.BadRequest(new { error = "Name is required." });
        if (string.IsNullOrWhiteSpace(request.HexCode))
            return Results.BadRequest(new { error = "HexCode is required." });

        var name = request.Name.Trim();
        if (await db.Colors.AnyAsync(c => c.Name == name, ct))
            return Results.BadRequest(new { error = $"Color '{name}' already exists." });

        var color = new Color
        {
            Id = Guid.NewGuid(),
            Name = name,
            HexCode = request.HexCode.Trim(),
            HasLining = request.HasLining,
            Active = request.Active ?? true,
        };
        db.Colors.Add(color);
        await db.SaveChangesAsync(ct);
        return Results.Created($"/api/v1/cadastros/colors/{color.Id}",
            new ColorDto(color.Id, color.Name, color.HexCode, color.HasLining, color.Active));
    }

    private static async Task<IResult> UpdateColor(
        Guid id,
        ColorUpsertRequest request,
        ConfeccaoDbContext db,
        CancellationToken ct)
    {
        var color = await db.Colors.FirstOrDefaultAsync(c => c.Id == id, ct);
        if (color is null) return Results.NotFound();

        if (string.IsNullOrWhiteSpace(request.Name))
            return Results.BadRequest(new { error = "Name is required." });
        if (string.IsNullOrWhiteSpace(request.HexCode))
            return Results.BadRequest(new { error = "HexCode is required." });

        var name = request.Name.Trim();
        if (name != color.Name && await db.Colors.AnyAsync(c => c.Name == name && c.Id != id, ct))
            return Results.BadRequest(new { error = $"Color '{name}' already exists." });

        color.Name = name;
        color.HexCode = request.HexCode.Trim();
        color.HasLining = request.HasLining;
        if (request.Active is not null) color.Active = request.Active.Value;

        await db.SaveChangesAsync(ct);
        return Results.Ok(new ColorDto(color.Id, color.Name, color.HexCode, color.HasLining, color.Active));
    }

    // ---- Models --------------------------------------------------------------

    private static async Task<IResult> GetModels(ConfeccaoDbContext db, CancellationToken ct)
    {
        var rows = await db.Models
            .Include(m => m.Stages.OrderBy(s => s.Sequence))
            .OrderBy(m => m.Name)
            .Select(m => new
            {
                m.Id,
                m.Name,
                m.ButtonCount,
                m.Active,
                Stages = m.Stages.OrderBy(s => s.Sequence).Select(s => s.Stage).ToList(),
            })
            .ToListAsync(ct);

        var models = rows.Select(r => new ModelDto(
            r.Id,
            r.Name,
            r.ButtonCount,
            r.Active,
            r.Stages.Select(s => s.ToString().ToLowerInvariant()).ToList())).ToList();

        return Results.Ok(models);
    }

    private static async Task<IResult> CreateModel(
        ModelUpsertRequest request,
        ConfeccaoDbContext db,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return Results.BadRequest(new { error = "Name is required." });
        if (request.ButtonCount < 0)
            return Results.BadRequest(new { error = "ButtonCount cannot be negative." });
        if (request.Flow is null || request.Flow.Count == 0)
            return Results.BadRequest(new { error = "Flow must include at least one stage." });

        if (!TryParseFlow(request.Flow, out var stages, out var problem)) return problem;

        var name = request.Name.Trim();
        if (await db.Models.AnyAsync(m => m.Name == name, ct))
            return Results.BadRequest(new { error = $"Model '{name}' already exists." });

        var modelId = Guid.NewGuid();
        var model = new Model
        {
            Id = modelId,
            Name = name,
            ButtonCount = request.ButtonCount,
            Active = request.Active ?? true,
        };
        db.Models.Add(model);
        for (var i = 0; i < stages.Count; i++)
        {
            db.ModelStages.Add(new ModelStage
            {
                Id = Guid.NewGuid(),
                ModelId = modelId,
                Stage = stages[i],
                Sequence = i,
            });
        }
        await db.SaveChangesAsync(ct);

        return Results.Created($"/api/v1/cadastros/models/{modelId}", new ModelDto(
            modelId,
            model.Name,
            model.ButtonCount,
            model.Active,
            stages.Select(s => s.ToString().ToLowerInvariant()).ToList()));
    }

    private static async Task<IResult> UpdateModel(
        Guid id,
        ModelUpsertRequest request,
        ConfeccaoDbContext db,
        CancellationToken ct)
    {
        var model = await db.Models
            .Include(m => m.Stages)
            .FirstOrDefaultAsync(m => m.Id == id, ct);
        if (model is null) return Results.NotFound();

        if (string.IsNullOrWhiteSpace(request.Name))
            return Results.BadRequest(new { error = "Name is required." });
        if (request.ButtonCount < 0)
            return Results.BadRequest(new { error = "ButtonCount cannot be negative." });
        if (request.Flow is null || request.Flow.Count == 0)
            return Results.BadRequest(new { error = "Flow must include at least one stage." });
        if (!TryParseFlow(request.Flow, out var stages, out var problem)) return problem;

        var name = request.Name.Trim();
        if (name != model.Name && await db.Models.AnyAsync(m => m.Name == name && m.Id != id, ct))
            return Results.BadRequest(new { error = $"Model '{name}' already exists." });

        model.Name = name;
        model.ButtonCount = request.ButtonCount;
        if (request.Active is not null) model.Active = request.Active.Value;

        // Replace flow rows. Existing in-flight orders keep their pipeline_items pointing
        // at this model — they're driven by the stages already spawned, so changes only
        // affect future orders.
        db.ModelStages.RemoveRange(model.Stages);
        for (var i = 0; i < stages.Count; i++)
        {
            db.ModelStages.Add(new ModelStage
            {
                Id = Guid.NewGuid(),
                ModelId = id,
                Stage = stages[i],
                Sequence = i,
            });
        }

        await db.SaveChangesAsync(ct);

        return Results.Ok(new ModelDto(
            model.Id,
            model.Name,
            model.ButtonCount,
            model.Active,
            stages.Select(s => s.ToString().ToLowerInvariant()).ToList()));
    }

    private static bool TryParseFlow(IReadOnlyList<string> flow, out List<StageCode> stages, out IResult problem)
    {
        problem = null!;
        stages = new List<StageCode>(flow.Count);
        var seen = new HashSet<StageCode>();
        foreach (var raw in flow)
        {
            if (!Enum.TryParse<StageCode>(raw.Replace("-", string.Empty), ignoreCase: true, out var stage))
            {
                problem = Results.BadRequest(new { error = $"Unknown stage '{raw}' in flow." });
                return false;
            }
            if (!seen.Add(stage))
            {
                problem = Results.BadRequest(new { error = $"Stage '{raw}' appears more than once in flow." });
                return false;
            }
            stages.Add(stage);
        }
        if (stages[0] != StageCode.Cutting)
        {
            problem = Results.BadRequest(new { error = "Flow must start with the cutting stage." });
            return false;
        }
        return true;
    }

    // ---- Users ---------------------------------------------------------------

    private static async Task<IResult> GetUsers(ConfeccaoDbContext db, string? role, CancellationToken ct)
    {
        var query = db.Users.AsQueryable();
        if (!string.IsNullOrWhiteSpace(role))
        {
            if (!Enum.TryParse<UserRole>(role, ignoreCase: true, out var parsed))
                return Results.BadRequest(new { error = $"Unknown role '{role}'." });
            query = query.Where(u => u.Role == parsed);
        }

        var users = await query
            .OrderBy(u => u.Name)
            .Select(u => new UserDto(u.Id, u.Name, u.Role.ToString().ToLowerInvariant(), u.Active))
            .ToListAsync(ct);
        return Results.Ok(users);
    }

    private static async Task<IResult> CreateUser(
        UserUpsertRequest request,
        ConfeccaoDbContext db,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return Results.BadRequest(new { error = "Name is required." });
        if (!Enum.TryParse<UserRole>(request.Role, ignoreCase: true, out var role))
            return Results.BadRequest(new { error = $"Unknown role '{request.Role}'." });

        var user = new User
        {
            Id = Guid.NewGuid(),
            Name = request.Name.Trim(),
            Role = role,
            Active = request.Active ?? true,
        };
        db.Users.Add(user);
        await db.SaveChangesAsync(ct);
        return Results.Created($"/api/v1/cadastros/users/{user.Id}",
            new UserDto(user.Id, user.Name, user.Role.ToString().ToLowerInvariant(), user.Active));
    }

    private static async Task<IResult> UpdateUser(
        Guid id,
        UserUpsertRequest request,
        ConfeccaoDbContext db,
        CancellationToken ct)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == id, ct);
        if (user is null) return Results.NotFound();

        if (string.IsNullOrWhiteSpace(request.Name))
            return Results.BadRequest(new { error = "Name is required." });
        if (!Enum.TryParse<UserRole>(request.Role, ignoreCase: true, out var role))
            return Results.BadRequest(new { error = $"Unknown role '{request.Role}'." });

        user.Name = request.Name.Trim();
        user.Role = role;
        if (request.Active is not null) user.Active = request.Active.Value;

        await db.SaveChangesAsync(ct);
        return Results.Ok(new UserDto(user.Id, user.Name, user.Role.ToString().ToLowerInvariant(), user.Active));
    }

    // ---- Prices (read-only for now — vigência-based, edits add new rows) ----

    private static async Task<IResult> GetPrices(ConfeccaoDbContext db, Guid? userId, CancellationToken ct)
    {
        var query = db.Prices.Include(p => p.Tiers).AsQueryable();
        if (userId is not null) query = query.Where(p => p.UserId == userId);

        var prices = await query
            .OrderByDescending(p => p.EffectiveFrom)
            .Select(p => new PriceDto(
                p.Id,
                p.UserId,
                p.Amount,
                p.LiningExtra,
                p.InterfacingExtra,
                p.CoveredButtonPrice,
                p.ReadyButtonPrice,
                p.EffectiveFrom,
                p.Note,
                p.Tiers.OrderBy(t => t.UpToQuantity)
                    .Select(t => new PriceTierDto(t.Id, t.UpToQuantity, t.Amount))
                    .ToList()
            ))
            .ToListAsync(ct);
        return Results.Ok(prices);
    }
}

public record ColorDto(Guid Id, string Name, string HexCode, bool HasLining, bool Active);
public record ColorUpsertRequest(string Name, string HexCode, bool HasLining, bool? Active);

public record ModelDto(Guid Id, string Name, int ButtonCount, bool Active, IReadOnlyList<string> Flow);
public record ModelUpsertRequest(string Name, int ButtonCount, IReadOnlyList<string> Flow, bool? Active);

public record UserDto(Guid Id, string Name, string Role, bool Active);
public record UserUpsertRequest(string Name, string Role, bool? Active);

public record PriceDto(
    Guid Id,
    Guid UserId,
    decimal Amount,
    decimal? LiningExtra,
    decimal? InterfacingExtra,
    decimal? CoveredButtonPrice,
    decimal? ReadyButtonPrice,
    DateTimeOffset EffectiveFrom,
    string? Note,
    IReadOnlyList<PriceTierDto> Tiers);

public record PriceTierDto(Guid Id, int UpToQuantity, decimal Amount);
