using Confeccao.Api.Common.Events;
using Confeccao.Api.Common.Naming;
using Confeccao.Api.Common.Pricing;
using Confeccao.Api.Infrastructure;
using Confeccao.Domain.Entities;
using Confeccao.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Confeccao.Api.Common.Pipeline;

/// <summary>
/// Records completion of work at a pipeline stage. Reused by every operator
/// flow (interfacing, sewing, buttoning, labeling, pressing, and the laundry
/// package completion). Cutting is excluded — it has a multi-item batch flow
/// of its own.
///
/// Caller is responsible for SaveChanges; this service only mutates the
/// change tracker.
/// </summary>
public class StageCompletionService
{
    private readonly ConfeccaoDbContext _db;
    private readonly PipelineFlowService _flow;
    private readonly PipelineEventLog _events;
    private readonly CreditGenerator? _credits;

    public StageCompletionService(
        ConfeccaoDbContext db,
        PipelineFlowService flow,
        PipelineEventLog events,
        CreditGenerator? credits = null)
    {
        _db = db;
        _flow = flow;
        _events = events;
        _credits = credits;
    }

    public class CompletionResult
    {
        public required PipelineItem Item { get; init; }
        public required int QuantityCompleted { get; init; }
        public required bool StageDone { get; init; }
        public PipelineItem? SpawnedNext { get; init; }
        public bool OrderCompleted { get; init; }
    }

    /// <summary>
    /// Records that <paramref name="quantity"/> units of work were completed by
    /// <paramref name="userId"/> on <paramref name="pipelineItem"/>. Throws on
    /// invalid inputs; returns metadata about what happened.
    /// </summary>
    public async Task<CompletionResult> RecordAsync(
        PipelineItem pipelineItem,
        Guid userId,
        int quantity,
        CancellationToken ct = default)
    {
        if (pipelineItem.Status != PipelineItemStatus.InProgress)
            throw new InvalidOperationException(
                $"Pipeline item {pipelineItem.Id} is not in progress (status: {pipelineItem.Status}).");

        var remaining = pipelineItem.QuantityTotal - pipelineItem.QuantityDone;
        if (quantity <= 0)
            throw new InvalidOperationException("Completion quantity must be positive.");
        if (quantity > remaining)
            throw new InvalidOperationException(
                $"Completion quantity {quantity} exceeds remaining {remaining}.");

        var now = DateTimeOffset.UtcNow;
        pipelineItem.QuantityDone += quantity;
        pipelineItem.AssignedUserId = userId;

        // Accrue a credit for the user covering this chunk of work.
        if (_credits is not null)
        {
            await _credits.AccrueAsync(
                userId: userId,
                orderId: pipelineItem.OrderId,
                pipelineItemId: pipelineItem.Id,
                stage: pipelineItem.Stage,
                modelId: pipelineItem.ModelId,
                colorId: pipelineItem.ColorId,
                size: pipelineItem.Size,
                quantity: quantity,
                occurredAt: now,
                ct: ct);
        }

        var stageDone = pipelineItem.QuantityDone == pipelineItem.QuantityTotal;
        PipelineItem? spawned = null;
        var orderCompleted = false;

        if (stageDone)
        {
            pipelineItem.Status = PipelineItemStatus.Done;
            pipelineItem.CompletedAt = now;

            var nextStage = await _flow.GetNextStageAsync(pipelineItem.ModelId, pipelineItem.Stage, ct);
            if (nextStage is not null)
            {
                spawned = new PipelineItem
                {
                    Id = Guid.NewGuid(),
                    OrderId = pipelineItem.OrderId,
                    OrderItemId = pipelineItem.OrderItemId,
                    ModelId = pipelineItem.ModelId,
                    Size = pipelineItem.Size,
                    ColorId = pipelineItem.ColorId,
                    ColorNameSnapshot = pipelineItem.ColorNameSnapshot,
                    FabricCodeSnapshot = pipelineItem.FabricCodeSnapshot,
                    Stage = nextStage.Value,
                    Status = PipelineItemStatus.AwaitingDispatch,
                    QuantityTotal = pipelineItem.QuantityTotal,
                    QuantityDone = 0,
                };
                _db.PipelineItems.Add(spawned);
            }
            else
            {
                // No next stage: this is the final stage for this order item.
                // The order completes when every PipelineItem under it is Done.
                orderCompleted = await TryCompleteOrderAsync(pipelineItem.OrderId, now, ct);
            }

            _events.Record(
                PipelineEventTypes.StageCompleted,
                new
                {
                    pipelineItemId = pipelineItem.Id,
                    orderId = pipelineItem.OrderId,
                    stage = KebabCase.From(pipelineItem.Stage),
                    nextStage = nextStage is null ? null : KebabCase.From(nextStage.Value),
                    quantity,
                    userId,
                },
                orderId: pipelineItem.OrderId,
                pipelineItemId: pipelineItem.Id,
                userId: userId);
        }
        else
        {
            _events.Record(
                PipelineEventTypes.PartialCompletion,
                new
                {
                    pipelineItemId = pipelineItem.Id,
                    orderId = pipelineItem.OrderId,
                    stage = KebabCase.From(pipelineItem.Stage),
                    quantity,
                    cumulativeDone = pipelineItem.QuantityDone,
                    quantityTotal = pipelineItem.QuantityTotal,
                    userId,
                },
                orderId: pipelineItem.OrderId,
                pipelineItemId: pipelineItem.Id,
                userId: userId);
        }

        return new CompletionResult
        {
            Item = pipelineItem,
            QuantityCompleted = quantity,
            StageDone = stageDone,
            SpawnedNext = spawned,
            OrderCompleted = orderCompleted,
        };
    }

    private async Task<bool> TryCompleteOrderAsync(Guid orderId, DateTimeOffset now, CancellationToken ct)
    {
        // Flush the in-flight item-status update so the AnyAsync query reflects it.
        // (EF Core queries always read from the store, not the change tracker.)
        await _db.SaveChangesAsync(ct);

        // Order completes when every PipelineItem for the order is Done.
        var allDone = !await _db.PipelineItems
            .AnyAsync(p => p.OrderId == orderId && p.Status != PipelineItemStatus.Done, ct);

        if (!allDone) return false;

        var order = await _db.Orders.FirstAsync(o => o.Id == orderId, ct);
        if (order.Status == OrderStatus.Completed) return false; // idempotent

        order.Status = OrderStatus.Completed;
        order.CompletedAt = now;

        _events.Record(
            PipelineEventTypes.OrderCompleted,
            new { orderId, completedAt = now },
            orderId: orderId);

        return true;
    }
}
