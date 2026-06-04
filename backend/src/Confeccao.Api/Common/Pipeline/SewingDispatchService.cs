using Confeccao.Api.Common.Events;
using Confeccao.Api.Common.Naming;
using Confeccao.Api.Infrastructure;
using Confeccao.Domain.Entities;
using Confeccao.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Confeccao.Api.Common.Pipeline;

/// <summary>
/// Manager dispatch for sewing items. Unlike the generic dispatch (one-click send),
/// sewing items are assigned to a specific seamstress and can be split across
/// multiple seamstresses by sending fewer pieces than the awaiting total.
///
/// Split semantics: dispatching N of a M-piece AwaitingDispatch row creates a
/// new InProgress row owned by the seamstress, sized N; the original row stays
/// AwaitingDispatch with QuantityTotal reduced to (M − N).
/// </summary>
public class SewingDispatchService
{
    private readonly ConfeccaoDbContext _db;
    private readonly PipelineEventLog _events;

    public SewingDispatchService(ConfeccaoDbContext db, PipelineEventLog events)
    {
        _db = db;
        _events = events;
    }

    public class DispatchResult
    {
        public required Guid DispatchedItemId { get; init; }
        public required int Quantity { get; init; }
        public required bool WasSplit { get; init; }
        public Guid? RemainingItemId { get; init; }
        public int RemainingQuantity { get; init; }
    }

    public Task<DispatchResult> DispatchAsync(
        PipelineItem awaitingItem,
        User seamstress,
        int quantity,
        CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        if (awaitingItem.Stage != StageCode.Sewing)
            throw new InvalidOperationException(
                $"SewingDispatch only handles sewing items (got {awaitingItem.Stage}).");
        if (awaitingItem.Status != PipelineItemStatus.AwaitingDispatch)
            throw new InvalidOperationException(
                $"Pipeline item is not awaiting dispatch (status: {awaitingItem.Status}).");
        if (seamstress.Role != UserRole.Sewing)
            throw new InvalidOperationException(
                $"Target user must have role Sewing (got {seamstress.Role}).");
        if (!seamstress.Active)
            throw new InvalidOperationException(
                $"Target user is inactive.");
        if (quantity <= 0)
            throw new InvalidOperationException("Dispatch quantity must be positive.");

        var available = awaitingItem.QuantityTotal - awaitingItem.QuantityDone;
        if (quantity > available)
            throw new InvalidOperationException(
                $"Dispatch quantity {quantity} exceeds available {available}.");

        var now = DateTimeOffset.UtcNow;
        var wasSplit = quantity < available;

        PipelineItem dispatched;
        Guid? remainingId = null;
        var remainingQty = 0;

        if (wasSplit)
        {
            // Create a brand new InProgress row sized to the dispatched quantity,
            // and reduce the original (still AwaitingDispatch) by that amount.
            dispatched = new PipelineItem
            {
                Id = Guid.NewGuid(),
                OrderId = awaitingItem.OrderId,
                OrderItemId = awaitingItem.OrderItemId,
                ModelId = awaitingItem.ModelId,
                Size = awaitingItem.Size,
                ColorId = awaitingItem.ColorId,
                ColorNameSnapshot = awaitingItem.ColorNameSnapshot,
                FabricCodeSnapshot = awaitingItem.FabricCodeSnapshot,
                Stage = StageCode.Sewing,
                Status = PipelineItemStatus.InProgress,
                QuantityTotal = quantity,
                QuantityDone = 0,
                AssignedUserId = seamstress.Id,
                DispatchedAt = now,
            };
            _db.PipelineItems.Add(dispatched);
            awaitingItem.QuantityTotal -= quantity;
            remainingId = awaitingItem.Id;
            remainingQty = awaitingItem.QuantityTotal;
        }
        else
        {
            // Dispatch the whole awaiting row to this seamstress.
            awaitingItem.Status = PipelineItemStatus.InProgress;
            awaitingItem.AssignedUserId = seamstress.Id;
            awaitingItem.DispatchedAt = now;
            dispatched = awaitingItem;
        }

        _events.Record(
            PipelineEventTypes.Dispatched,
            new
            {
                pipelineItemId = dispatched.Id,
                orderId = dispatched.OrderId,
                stage = KebabCase.From(StageCode.Sewing),
                quantity,
                kind = "sewing",
                assignedUserId = seamstress.Id,
                split = wasSplit,
                remainingItemId = remainingId,
                remainingQuantity = remainingQty,
            },
            orderId: dispatched.OrderId,
            pipelineItemId: dispatched.Id,
            userId: seamstress.Id);

        return Task.FromResult(new DispatchResult
        {
            DispatchedItemId = dispatched.Id,
            Quantity = quantity,
            WasSplit = wasSplit,
            RemainingItemId = remainingId,
            RemainingQuantity = remainingQty,
        });
    }
}
