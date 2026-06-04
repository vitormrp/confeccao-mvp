using Confeccao.Api.Common.Events;
using Confeccao.Api.Common.Pipeline;
using Confeccao.Api.Infrastructure;
using Confeccao.Domain.Entities;
using Confeccao.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Confeccao.UnitTests;

public class SewingDispatchServiceTests
{
    [Fact]
    public async Task Full_dispatch_mutates_existing_row_to_in_progress()
    {
        await using var db = NewDb();
        var (item, seamstress) = await Seed(db, awaitingQty: 20);
        var svc = NewService(db);

        var result = await svc.DispatchAsync(item, seamstress, quantity: 20);
        await db.SaveChangesAsync();

        Assert.False(result.WasSplit);
        Assert.Equal(item.Id, result.DispatchedItemId);
        Assert.Equal(PipelineItemStatus.InProgress, item.Status);
        Assert.Equal(seamstress.Id, item.AssignedUserId);
        Assert.Equal(20, item.QuantityTotal);
        Assert.NotNull(item.DispatchedAt);

        // No new row should have been created.
        var rowsForOrder = await db.PipelineItems
            .Where(p => p.OrderId == item.OrderId && p.Stage == StageCode.Sewing)
            .CountAsync();
        Assert.Equal(1, rowsForOrder);
    }

    [Fact]
    public async Task Partial_dispatch_splits_into_new_row()
    {
        await using var db = NewDb();
        var (item, seamstress) = await Seed(db, awaitingQty: 48);
        var svc = NewService(db);

        var result = await svc.DispatchAsync(item, seamstress, quantity: 20);
        await db.SaveChangesAsync();

        Assert.True(result.WasSplit);
        Assert.NotEqual(item.Id, result.DispatchedItemId);
        Assert.Equal(item.Id, result.RemainingItemId);
        Assert.Equal(28, result.RemainingQuantity);

        // Original row reduced to 28, still AwaitingDispatch.
        Assert.Equal(PipelineItemStatus.AwaitingDispatch, item.Status);
        Assert.Equal(28, item.QuantityTotal);
        Assert.Null(item.AssignedUserId);

        // New row created with quantity 20, InProgress, assigned to the seamstress.
        var newRow = await db.PipelineItems.FirstAsync(p => p.Id == result.DispatchedItemId);
        Assert.Equal(PipelineItemStatus.InProgress, newRow.Status);
        Assert.Equal(20, newRow.QuantityTotal);
        Assert.Equal(seamstress.Id, newRow.AssignedUserId);
        Assert.Equal(item.OrderItemId, newRow.OrderItemId);
        Assert.Equal(item.ColorId, newRow.ColorId);
        Assert.NotNull(newRow.DispatchedAt);
    }

    [Fact]
    public async Task Cannot_dispatch_more_than_available()
    {
        await using var db = NewDb();
        var (item, seamstress) = await Seed(db, awaitingQty: 10);
        var svc = NewService(db);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            svc.DispatchAsync(item, seamstress, quantity: 11));
    }

    [Fact]
    public async Task Cannot_dispatch_zero_or_negative_quantity()
    {
        await using var db = NewDb();
        var (item, seamstress) = await Seed(db, awaitingQty: 10);
        var svc = NewService(db);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            svc.DispatchAsync(item, seamstress, quantity: 0));
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            svc.DispatchAsync(item, seamstress, quantity: -5));
    }

    [Fact]
    public async Task Cannot_dispatch_to_non_sewing_user()
    {
        await using var db = NewDb();
        var (item, _) = await Seed(db, awaitingQty: 10);
        var notSeamstress = new User
        {
            Id = Guid.NewGuid(),
            Name = "Cortador",
            Role = UserRole.Cutting,
        };
        db.Users.Add(notSeamstress);
        await db.SaveChangesAsync();

        var svc = NewService(db);
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            svc.DispatchAsync(item, notSeamstress, quantity: 5));
    }

    [Fact]
    public async Task Cannot_dispatch_item_not_awaiting()
    {
        await using var db = NewDb();
        var (item, seamstress) = await Seed(db, awaitingQty: 10);
        item.Status = PipelineItemStatus.InProgress;
        await db.SaveChangesAsync();

        var svc = NewService(db);
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            svc.DispatchAsync(item, seamstress, quantity: 5));
    }

    [Fact]
    public async Task Cannot_dispatch_non_sewing_stage_item()
    {
        await using var db = NewDb();
        var (item, seamstress) = await Seed(db, awaitingQty: 10);
        item.Stage = StageCode.Interfacing;
        await db.SaveChangesAsync();

        var svc = NewService(db);
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            svc.DispatchAsync(item, seamstress, quantity: 5));
    }

    private static ConfeccaoDbContext NewDb()
    {
        var options = new DbContextOptionsBuilder<ConfeccaoDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new ConfeccaoDbContext(options);
    }

    private static SewingDispatchService NewService(ConfeccaoDbContext db) =>
        new SewingDispatchService(db, new PipelineEventLog(db));

    private static async Task<(PipelineItem Item, User Seamstress)> Seed(
        ConfeccaoDbContext db,
        int awaitingQty)
    {
        var colorId = Guid.NewGuid();
        var modelId = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        var orderItemId = Guid.NewGuid();

        db.Colors.Add(new Color { Id = colorId, Name = "Test", HexCode = "#000" });
        db.Models.Add(new Model { Id = modelId, Name = "Test Model" });
        db.Orders.Add(new Order
        {
            Id = orderId,
            Number = 1,
            FabricCode = "T-1",
            ColorId = colorId,
            Status = OrderStatus.InProduction,
        });
        db.OrderItems.Add(new OrderItem
        {
            Id = orderItemId,
            OrderId = orderId,
            ModelId = modelId,
            Size = Size.M,
            PlannedQuantity = awaitingQty,
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
            Stage = StageCode.Sewing,
            Status = PipelineItemStatus.AwaitingDispatch,
            QuantityTotal = awaitingQty,
            QuantityDone = 0,
        };
        db.PipelineItems.Add(item);

        var seamstress = new User
        {
            Id = Guid.NewGuid(),
            Name = "Costureira X",
            Role = UserRole.Sewing,
        };
        db.Users.Add(seamstress);

        await db.SaveChangesAsync();
        return (item, seamstress);
    }
}
