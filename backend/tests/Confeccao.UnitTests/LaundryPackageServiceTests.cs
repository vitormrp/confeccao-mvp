using Confeccao.Api.Common.Events;
using Confeccao.Api.Common.Pipeline;
using Confeccao.Api.Infrastructure;
using Confeccao.Domain.Entities;
using Confeccao.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Confeccao.UnitTests;

public class LaundryPackageServiceTests
{
    [Fact]
    public async Task Bundle_creates_package_and_flips_items()
    {
        await using var db = NewDb();
        var ctx = await SeedWashingItems(db, quantities: [20, 30]);
        var svc = NewService(db);

        var result = await svc.BundleAsync(ctx.Items);
        await db.SaveChangesAsync();

        Assert.Equal(2, result.ItemCount);
        Assert.Equal(50, result.TotalQuantity);

        var pkg = await db.LaundryPackages.FirstAsync();
        Assert.Equal(LaundryPackageStatus.Awaiting, pkg.Status);

        foreach (var item in ctx.Items)
        {
            Assert.Equal(PipelineItemStatus.InProgress, item.Status);
            Assert.Equal(pkg.Id, item.LaundryPackageId);
            Assert.NotNull(item.DispatchedAt);
        }
    }

    [Fact]
    public async Task Bundle_rejects_empty_selection()
    {
        await using var db = NewDb();
        var svc = NewService(db);
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            svc.BundleAsync(Array.Empty<PipelineItem>()));
    }

    [Fact]
    public async Task Bundle_rejects_non_washing_item()
    {
        await using var db = NewDb();
        var ctx = await SeedWashingItems(db, quantities: [10]);
        ctx.Items[0].Stage = StageCode.Buttoning;
        await db.SaveChangesAsync();

        var svc = NewService(db);
        await Assert.ThrowsAsync<InvalidOperationException>(() => svc.BundleAsync(ctx.Items));
    }

    [Fact]
    public async Task Bundle_rejects_item_not_awaiting()
    {
        await using var db = NewDb();
        var ctx = await SeedWashingItems(db, quantities: [10]);
        ctx.Items[0].Status = PipelineItemStatus.InProgress;
        await db.SaveChangesAsync();

        var svc = NewService(db);
        await Assert.ThrowsAsync<InvalidOperationException>(() => svc.BundleAsync(ctx.Items));
    }

    [Fact]
    public async Task Complete_marks_items_done_and_spawns_buttoning()
    {
        await using var db = NewDb();
        var ctx = await SeedWashingItems(db, quantities: [20, 30]);
        var svc = NewService(db);

        var bundle = await svc.BundleAsync(ctx.Items);
        await db.SaveChangesAsync();

        var pkg = await db.LaundryPackages.FirstAsync();
        var result = await svc.CompleteAsync(pkg, ctx.WasherUserId);
        await db.SaveChangesAsync();

        Assert.Equal(2, result.ItemCount);
        Assert.Equal(50, result.TotalQuantity);

        // Both washing items are Done.
        foreach (var item in ctx.Items)
        {
            Assert.Equal(PipelineItemStatus.Done, item.Status);
            Assert.Equal(item.QuantityTotal, item.QuantityDone);
        }

        // Buttoning rows were spawned (one per washing item).
        var buttoningRows = await db.PipelineItems
            .Where(p => p.Stage == StageCode.Buttoning)
            .ToListAsync();
        Assert.Equal(2, buttoningRows.Count);
        Assert.All(buttoningRows, b => Assert.Equal(PipelineItemStatus.AwaitingDispatch, b.Status));
        Assert.Equal(50, buttoningRows.Sum(b => b.QuantityTotal));

        // Package marked completed by the washer.
        Assert.Equal(LaundryPackageStatus.Completed, pkg.Status);
        Assert.NotNull(pkg.CompletedAt);
        Assert.Equal(ctx.WasherUserId, pkg.CompletedByUserId);
    }

    [Fact]
    public async Task Complete_rejects_already_completed_package()
    {
        await using var db = NewDb();
        var ctx = await SeedWashingItems(db, quantities: [10]);
        var svc = NewService(db);

        await svc.BundleAsync(ctx.Items);
        await db.SaveChangesAsync();
        var pkg = await db.LaundryPackages.FirstAsync();
        await svc.CompleteAsync(pkg, ctx.WasherUserId);
        await db.SaveChangesAsync();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            svc.CompleteAsync(pkg, ctx.WasherUserId));
    }

    private record SeedContext(IReadOnlyList<PipelineItem> Items, Guid WasherUserId);

    private static ConfeccaoDbContext NewDb()
    {
        var options = new DbContextOptionsBuilder<ConfeccaoDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new ConfeccaoDbContext(options);
    }

    private static LaundryPackageService NewService(ConfeccaoDbContext db)
    {
        var flow = new PipelineFlowService(db);
        var events = new PipelineEventLog(db);
        var completion = new StageCompletionService(db, flow, events);
        return new LaundryPackageService(db, completion, events);
    }

    private static async Task<SeedContext> SeedWashingItems(ConfeccaoDbContext db, int[] quantities)
    {
        var colorId = Guid.NewGuid();
        var modelId = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        var washerUserId = Guid.NewGuid();

        db.Colors.Add(new Color { Id = colorId, Name = "Test", HexCode = "#000" });
        db.Models.Add(new Model { Id = modelId, Name = "Test Model" });
        // Flow: Cutting → Sewing → Washing → Buttoning (so completing washing spawns buttoning).
        db.ModelStages.AddRange(
            new ModelStage { Id = Guid.NewGuid(), ModelId = modelId, Stage = StageCode.Cutting, Sequence = 0 },
            new ModelStage { Id = Guid.NewGuid(), ModelId = modelId, Stage = StageCode.Sewing, Sequence = 1 },
            new ModelStage { Id = Guid.NewGuid(), ModelId = modelId, Stage = StageCode.Washing, Sequence = 2 },
            new ModelStage { Id = Guid.NewGuid(), ModelId = modelId, Stage = StageCode.Buttoning, Sequence = 3 });
        db.Orders.Add(new Order
        {
            Id = orderId,
            Number = 1,
            FabricCode = "T-1",
            ColorId = colorId,
            Status = OrderStatus.InProduction,
        });
        db.Users.Add(new User
        {
            Id = washerUserId,
            Name = "Washer",
            Role = UserRole.Washing,
        });

        var items = new List<PipelineItem>();
        foreach (var qty in quantities)
        {
            var orderItemId = Guid.NewGuid();
            db.OrderItems.Add(new OrderItem
            {
                Id = orderItemId,
                OrderId = orderId,
                ModelId = modelId,
                Size = Size.M,
                PlannedQuantity = qty,
            });
            var item = new PipelineItem
            {
                Id = Guid.NewGuid(),
                OrderId = orderId,
                OrderItemId = orderItemId,
                ModelId = modelId,
                Size = Size.M,
                ColorId = colorId,
                ColorNameSnapshot = "Test",
                FabricCodeSnapshot = "T-1",
                Stage = StageCode.Washing,
                Status = PipelineItemStatus.AwaitingDispatch,
                QuantityTotal = qty,
                QuantityDone = 0,
            };
            db.PipelineItems.Add(item);
            items.Add(item);
        }
        await db.SaveChangesAsync();
        return new SeedContext(items, washerUserId);
    }
}
