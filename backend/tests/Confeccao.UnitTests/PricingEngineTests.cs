using Confeccao.Api.Common.Pricing;
using Confeccao.Api.Infrastructure;
using Confeccao.Domain.Entities;
using Confeccao.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Confeccao.UnitTests;

public class PricingEngineTests
{
    [Fact]
    public async Task Base_amount_times_quantity_for_generic_stage()
    {
        await using var db = NewDb();
        var ctx = await Seed(db, configure: p => p.Amount = 1.20m);

        var amount = await new PricingEngine(db).ComputeAsync(
            ctx.UserId, StageCode.Washing, ctx.ModelId, ctx.PlainColorId, 50, ctx.Now);

        Assert.Equal(60.00m, amount);
    }

    [Fact]
    public async Task Sewing_adds_lining_extra_when_color_has_lining()
    {
        await using var db = NewDb();
        var ctx = await Seed(db, configure: p =>
        {
            p.Amount = 8.00m;
            p.LiningExtra = 2.00m;
        });

        var withLining = await new PricingEngine(db).ComputeAsync(
            ctx.UserId, StageCode.Sewing, ctx.ModelId, ctx.LinedColorId, 10, ctx.Now);
        var withoutLining = await new PricingEngine(db).ComputeAsync(
            ctx.UserId, StageCode.Sewing, ctx.ModelId, ctx.PlainColorId, 10, ctx.Now);

        Assert.Equal((8.00m + 2.00m) * 10, withLining);
        Assert.Equal(8.00m * 10, withoutLining);
    }

    [Fact]
    public async Task Sewing_adds_interfacing_extra_when_model_flow_includes_interfacing()
    {
        await using var db = NewDb();
        var ctx = await Seed(db, configure: p =>
        {
            p.Amount = 8.00m;
            p.InterfacingExtra = 1.50m;
        }, modelHasInterfacing: true);

        var amount = await new PricingEngine(db).ComputeAsync(
            ctx.UserId, StageCode.Sewing, ctx.ModelId, ctx.PlainColorId, 10, ctx.Now);

        Assert.Equal((8.00m + 1.50m) * 10, amount);
    }

    [Fact]
    public async Task Sewing_extras_do_not_apply_to_other_stages()
    {
        await using var db = NewDb();
        var ctx = await Seed(db, configure: p =>
        {
            p.Amount = 1.00m;
            p.LiningExtra = 99.00m;
            p.InterfacingExtra = 99.00m;
        }, modelHasInterfacing: true);

        var amount = await new PricingEngine(db).ComputeAsync(
            ctx.UserId, StageCode.Pressing, ctx.ModelId, ctx.LinedColorId, 5, ctx.Now);

        Assert.Equal(5.00m, amount);
    }

    [Fact]
    public async Task Cutter_uses_tier_amount_picking_smallest_fitting_tier()
    {
        await using var db = NewDb();
        var ctx = await Seed(db, configure: p =>
        {
            p.Amount = 0m;
            p.Tiers.Add(new PriceTier { Id = Guid.NewGuid(), UpToQuantity = 50, Amount = 3.00m });
            p.Tiers.Add(new PriceTier { Id = Guid.NewGuid(), UpToQuantity = 200, Amount = 2.50m });
            p.Tiers.Add(new PriceTier { Id = Guid.NewGuid(), UpToQuantity = int.MaxValue, Amount = 2.00m });
        });

        var engine = new PricingEngine(db);
        Assert.Equal(3.00m * 30, await engine.ComputeAsync(ctx.UserId, StageCode.Cutting, ctx.ModelId, ctx.PlainColorId, 30, ctx.Now));
        Assert.Equal(2.50m * 150, await engine.ComputeAsync(ctx.UserId, StageCode.Cutting, ctx.ModelId, ctx.PlainColorId, 150, ctx.Now));
        Assert.Equal(2.00m * 500, await engine.ComputeAsync(ctx.UserId, StageCode.Cutting, ctx.ModelId, ctx.PlainColorId, 500, ctx.Now));
    }

    [Fact]
    public async Task Buttoning_uses_covered_price_for_non_blackwhite_color()
    {
        await using var db = NewDb();
        var ctx = await Seed(db,
            configure: p =>
            {
                p.Amount = 999m; // should be ignored
                p.CoveredButtonPrice = 0.80m;
                p.ReadyButtonPrice = 0.30m;
            },
            modelButtons: 2);

        var amount = await new PricingEngine(db).ComputeAsync(
            ctx.UserId, StageCode.Buttoning, ctx.ModelId, ctx.PlainColorId, 10, ctx.Now);

        // 0.80 * 2 buttons * 10 pieces = 16.00
        Assert.Equal(0.80m * 2 * 10, amount);
    }

    [Fact]
    public async Task Buttoning_uses_ready_price_for_preto_or_branco()
    {
        await using var db = NewDb();
        var ctx = await Seed(db,
            configure: p =>
            {
                p.CoveredButtonPrice = 0.80m;
                p.ReadyButtonPrice = 0.30m;
            },
            modelButtons: 2,
            addColorNamed: "Preto");

        var amount = await new PricingEngine(db).ComputeAsync(
            ctx.UserId, StageCode.Buttoning, ctx.ModelId, ctx.NamedColorId!.Value, 10, ctx.Now);

        Assert.Equal(0.30m * 2 * 10, amount);
    }

    [Fact]
    public async Task Buttoning_returns_zero_when_model_has_no_buttons()
    {
        await using var db = NewDb();
        var ctx = await Seed(db,
            configure: p =>
            {
                p.CoveredButtonPrice = 0.80m;
                p.ReadyButtonPrice = 0.30m;
            },
            modelButtons: 0);

        var amount = await new PricingEngine(db).ComputeAsync(
            ctx.UserId, StageCode.Buttoning, ctx.ModelId, ctx.PlainColorId, 10, ctx.Now);

        Assert.Equal(0m, amount);
    }

    [Fact]
    public async Task Picks_latest_price_effective_at_timestamp()
    {
        await using var db = NewDb();
        var ctx = await Seed(db, configure: p => p.Amount = 5.00m);

        // Add a newer price that's effective from later.
        var newer = new Price
        {
            Id = Guid.NewGuid(),
            UserId = ctx.UserId,
            Amount = 7.00m,
            EffectiveFrom = ctx.Now.AddDays(1),
        };
        db.Prices.Add(newer);
        await db.SaveChangesAsync();

        var engine = new PricingEngine(db);
        var before = await engine.ComputeAsync(ctx.UserId, StageCode.Pressing, ctx.ModelId, ctx.PlainColorId, 1, ctx.Now);
        var after = await engine.ComputeAsync(ctx.UserId, StageCode.Pressing, ctx.ModelId, ctx.PlainColorId, 1, ctx.Now.AddDays(2));

        Assert.Equal(5.00m, before);
        Assert.Equal(7.00m, after);
    }

    [Fact]
    public async Task Returns_zero_when_no_price_configured()
    {
        await using var db = NewDb();
        var ctx = await Seed(db, configure: null);

        var amount = await new PricingEngine(db).ComputeAsync(
            ctx.UserId, StageCode.Pressing, ctx.ModelId, ctx.PlainColorId, 5, ctx.Now);

        Assert.Equal(0m, amount);
    }

    private record SeedContext(
        Guid UserId,
        Guid ModelId,
        Guid PlainColorId,
        Guid LinedColorId,
        Guid? NamedColorId,
        DateTimeOffset Now);

    private static ConfeccaoDbContext NewDb()
    {
        var options = new DbContextOptionsBuilder<ConfeccaoDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new ConfeccaoDbContext(options);
    }

    private static async Task<SeedContext> Seed(
        ConfeccaoDbContext db,
        Action<Price>? configure,
        bool modelHasInterfacing = false,
        int modelButtons = 0,
        string? addColorNamed = null)
    {
        var now = new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);
        var userId = Guid.NewGuid();
        var modelId = Guid.NewGuid();
        var plainColorId = Guid.NewGuid();
        var linedColorId = Guid.NewGuid();
        Guid? namedColorId = null;

        db.Users.Add(new User { Id = userId, Name = "Op", Role = UserRole.Sewing });
        db.Colors.Add(new Color { Id = plainColorId, Name = "Verde", HexCode = "#000", HasLining = false });
        db.Colors.Add(new Color { Id = linedColorId, Name = "Branco_lined", HexCode = "#fff", HasLining = true });
        if (addColorNamed is not null)
        {
            namedColorId = Guid.NewGuid();
            db.Colors.Add(new Color { Id = namedColorId.Value, Name = addColorNamed, HexCode = "#000" });
        }
        db.Models.Add(new Model { Id = modelId, Name = "M", ButtonCount = modelButtons });
        db.ModelStages.Add(new ModelStage
        {
            Id = Guid.NewGuid(), ModelId = modelId, Stage = StageCode.Sewing, Sequence = 0,
        });
        if (modelHasInterfacing)
        {
            db.ModelStages.Add(new ModelStage
            {
                Id = Guid.NewGuid(), ModelId = modelId, Stage = StageCode.Interfacing, Sequence = 1,
            });
        }
        if (configure is not null)
        {
            var price = new Price
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                EffectiveFrom = now.AddDays(-1),
            };
            configure(price);
            db.Prices.Add(price);
        }
        await db.SaveChangesAsync();
        return new SeedContext(userId, modelId, plainColorId, linedColorId, namedColorId, now);
    }
}
