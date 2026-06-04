using Confeccao.Api.Common.Events;
using Confeccao.Api.Common.Pipeline;
using Confeccao.Api.Infrastructure;
using Confeccao.Domain.Entities;
using Confeccao.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Confeccao.UnitTests;

public class StageCompletionServiceTests
{
    [Fact]
    public async Task Partial_completion_accumulates_without_spawning_next()
    {
        await using var db = NewDb();
        var (orderId, _, item) = await SeedOrderWithItemAtStage(db,
            stages: [StageCode.Cutting, StageCode.Interfacing, StageCode.Sewing],
            currentStage: StageCode.Interfacing,
            quantity: 30);

        var svc = NewService(db);
        var userId = Guid.NewGuid();

        var result = await svc.RecordAsync(item, userId, quantity: 10);
        await db.SaveChangesAsync();

        Assert.False(result.StageDone);
        Assert.Null(result.SpawnedNext);
        Assert.False(result.OrderCompleted);
        Assert.Equal(10, item.QuantityDone);
        Assert.Equal(PipelineItemStatus.InProgress, item.Status);

        // No next-stage item should have been created yet.
        var nextStageItems = await db.PipelineItems
            .Where(p => p.OrderId == orderId && p.Stage == StageCode.Sewing)
            .CountAsync();
        Assert.Equal(0, nextStageItems);
    }

    [Fact]
    public async Task Full_completion_spawns_next_stage_in_awaiting_dispatch()
    {
        await using var db = NewDb();
        var (orderId, _, item) = await SeedOrderWithItemAtStage(db,
            stages: [StageCode.Cutting, StageCode.Interfacing, StageCode.Sewing],
            currentStage: StageCode.Interfacing,
            quantity: 30);

        var svc = NewService(db);
        var userId = Guid.NewGuid();

        var result = await svc.RecordAsync(item, userId, quantity: 30);
        await db.SaveChangesAsync();

        Assert.True(result.StageDone);
        Assert.NotNull(result.SpawnedNext);
        Assert.Equal(StageCode.Sewing, result.SpawnedNext!.Stage);
        Assert.Equal(PipelineItemStatus.AwaitingDispatch, result.SpawnedNext.Status);
        Assert.Equal(30, result.SpawnedNext.QuantityTotal);
        Assert.False(result.OrderCompleted); // there are still stages ahead
        Assert.Equal(PipelineItemStatus.Done, item.Status);
        Assert.NotNull(item.CompletedAt);
    }

    [Fact]
    public async Task Completing_final_stage_marks_order_completed()
    {
        await using var db = NewDb();
        var (orderId, _, item) = await SeedOrderWithItemAtStage(db,
            stages: [StageCode.Cutting, StageCode.Pressing],
            currentStage: StageCode.Pressing,
            quantity: 5);

        var svc = NewService(db);
        var userId = Guid.NewGuid();

        var result = await svc.RecordAsync(item, userId, quantity: 5);
        await db.SaveChangesAsync();

        Assert.True(result.StageDone);
        Assert.Null(result.SpawnedNext);
        Assert.True(result.OrderCompleted);

        var order = await db.Orders.FirstAsync(o => o.Id == orderId);
        Assert.Equal(OrderStatus.Completed, order.Status);
        Assert.NotNull(order.CompletedAt);
    }

    [Fact]
    public async Task Order_not_completed_while_sibling_items_still_pending()
    {
        await using var db = NewDb();
        var (orderId, _, finishedItem) = await SeedOrderWithItemAtStage(db,
            stages: [StageCode.Cutting, StageCode.Pressing],
            currentStage: StageCode.Pressing,
            quantity: 5);

        // Add a sibling item for the same order, in mid-flow.
        var modelId = (await db.PipelineItems.FirstAsync()).ModelId;
        var siblingOrderItem = new OrderItem
        {
            Id = Guid.NewGuid(),
            OrderId = orderId,
            ModelId = modelId,
            Size = Size.G,
            PlannedQuantity = 10,
        };
        db.OrderItems.Add(siblingOrderItem);
        db.PipelineItems.Add(new PipelineItem
        {
            Id = Guid.NewGuid(),
            OrderId = orderId,
            OrderItemId = siblingOrderItem.Id,
            ModelId = modelId,
            Size = Size.G,
            ColorId = Guid.NewGuid(),
            Stage = StageCode.Pressing,
            Status = PipelineItemStatus.InProgress,
            QuantityTotal = 10,
            QuantityDone = 0,
        });
        await db.SaveChangesAsync();

        var svc = NewService(db);
        var result = await svc.RecordAsync(finishedItem, Guid.NewGuid(), quantity: 5);
        await db.SaveChangesAsync();

        Assert.True(result.StageDone);
        Assert.False(result.OrderCompleted); // sibling still in progress

        var order = await db.Orders.FirstAsync(o => o.Id == orderId);
        Assert.NotEqual(OrderStatus.Completed, order.Status);
    }

    [Fact]
    public async Task Cannot_complete_more_than_remaining()
    {
        await using var db = NewDb();
        var (_, _, item) = await SeedOrderWithItemAtStage(db,
            stages: [StageCode.Cutting, StageCode.Interfacing],
            currentStage: StageCode.Interfacing,
            quantity: 10);

        var svc = NewService(db);
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            svc.RecordAsync(item, Guid.NewGuid(), quantity: 11));
    }

    [Fact]
    public async Task Cannot_complete_item_not_in_progress()
    {
        await using var db = NewDb();
        var (_, _, item) = await SeedOrderWithItemAtStage(db,
            stages: [StageCode.Cutting, StageCode.Interfacing],
            currentStage: StageCode.Interfacing,
            quantity: 10);
        item.Status = PipelineItemStatus.AwaitingDispatch;
        await db.SaveChangesAsync();

        var svc = NewService(db);
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            svc.RecordAsync(item, Guid.NewGuid(), quantity: 1));
    }

    private static ConfeccaoDbContext NewDb()
    {
        var options = new DbContextOptionsBuilder<ConfeccaoDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        return new ConfeccaoDbContext(options);
    }

    private static StageCompletionService NewService(ConfeccaoDbContext db)
    {
        var flow = new PipelineFlowService(db);
        var events = new PipelineEventLog(db);
        return new StageCompletionService(db, flow, events);
    }

    private static async Task<(Guid OrderId, Guid ModelId, PipelineItem Item)> SeedOrderWithItemAtStage(
        ConfeccaoDbContext db,
        StageCode[] stages,
        StageCode currentStage,
        int quantity)
    {
        var colorId = Guid.NewGuid();
        var modelId = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        var orderItemId = Guid.NewGuid();
        var itemId = Guid.NewGuid();

        db.Colors.Add(new Color { Id = colorId, Name = "Test", HexCode = "#000000" });
        db.Models.Add(new Model { Id = modelId, Name = $"Test Model {modelId:N}" });
        for (var i = 0; i < stages.Length; i++)
            db.ModelStages.Add(new ModelStage { Id = Guid.NewGuid(), ModelId = modelId, Stage = stages[i], Sequence = i });

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
            PlannedQuantity = quantity,
        });

        var item = new PipelineItem
        {
            Id = itemId,
            OrderId = orderId,
            OrderItemId = orderItemId,
            ModelId = modelId,
            Size = Size.M,
            ColorId = colorId,
            ColorNameSnapshot = "Test",
            FabricCodeSnapshot = "T-1",
            Stage = currentStage,
            Status = PipelineItemStatus.InProgress,
            QuantityTotal = quantity,
            QuantityDone = 0,
        };
        db.PipelineItems.Add(item);
        await db.SaveChangesAsync();
        return (orderId, modelId, item);
    }
}
