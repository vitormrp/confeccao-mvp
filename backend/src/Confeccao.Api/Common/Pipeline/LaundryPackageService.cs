using Confeccao.Api.Common.Events;
using Confeccao.Api.Common.Naming;
using Confeccao.Api.Infrastructure;
using Confeccao.Domain.Entities;
using Confeccao.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Confeccao.Api.Common.Pipeline;

/// <summary>
/// Bundles washing-stage AwaitingDispatch items into a laundry package and
/// processes the package as a unit when the lavanderia operator confirms.
/// Completion delegates per-item bookkeeping to <see cref="StageCompletionService"/>
/// so spawning the next stage (buttoning) and emitting StageCompleted events
/// goes through the same code path as every other stage.
///
/// Caller is responsible for SaveChanges.
/// </summary>
public class LaundryPackageService
{
    private readonly ConfeccaoDbContext _db;
    private readonly StageCompletionService _completion;
    private readonly PipelineEventLog _events;

    public LaundryPackageService(
        ConfeccaoDbContext db,
        StageCompletionService completion,
        PipelineEventLog events)
    {
        _db = db;
        _completion = completion;
        _events = events;
    }

    public class BundleResult
    {
        public required Guid PackageId { get; init; }
        public required int ItemCount { get; init; }
        public required int TotalQuantity { get; init; }
    }

    public class CompleteResult
    {
        public required Guid PackageId { get; init; }
        public required int ItemCount { get; init; }
        public required int TotalQuantity { get; init; }
        public required IReadOnlyList<Guid> CompletedOrderIds { get; init; }
    }

    public Task<BundleResult> BundleAsync(
        IReadOnlyList<PipelineItem> items,
        CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        if (items.Count == 0)
            throw new InvalidOperationException("A package must contain at least one item.");

        foreach (var item in items)
        {
            if (item.Stage != StageCode.Washing)
                throw new InvalidOperationException(
                    $"Pipeline item {item.Id} is at stage {item.Stage}, not washing.");
            if (item.Status != PipelineItemStatus.AwaitingDispatch)
                throw new InvalidOperationException(
                    $"Pipeline item {item.Id} is not awaiting dispatch (status: {item.Status}).");
        }

        var now = DateTimeOffset.UtcNow;
        var pkg = new LaundryPackage
        {
            Id = Guid.NewGuid(),
            Status = LaundryPackageStatus.Awaiting,
            SentAt = now,
        };
        _db.LaundryPackages.Add(pkg);

        foreach (var item in items)
        {
            item.Status = PipelineItemStatus.InProgress;
            item.DispatchedAt = now;
            item.LaundryPackageId = pkg.Id;
        }

        var totalQty = items.Sum(i => i.QuantityTotal - i.QuantityDone);

        _events.Record(
            PipelineEventTypes.LaundryPackageSent,
            new
            {
                packageId = pkg.Id,
                itemCount = items.Count,
                totalQuantity = totalQty,
                pipelineItemIds = items.Select(i => i.Id),
            });

        return Task.FromResult(new BundleResult
        {
            PackageId = pkg.Id,
            ItemCount = items.Count,
            TotalQuantity = totalQty,
        });
    }

    public async Task<CompleteResult> CompleteAsync(
        LaundryPackage pkg,
        Guid userId,
        CancellationToken ct = default)
    {
        if (pkg.Status != LaundryPackageStatus.Awaiting)
            throw new InvalidOperationException(
                $"Laundry package {pkg.Id} is not awaiting completion (status: {pkg.Status}).");

        var items = await _db.PipelineItems
            .Where(p => p.LaundryPackageId == pkg.Id)
            .ToListAsync(ct);

        if (items.Count == 0)
            throw new InvalidOperationException(
                $"Laundry package {pkg.Id} has no items linked to it.");

        var now = DateTimeOffset.UtcNow;
        var completedOrders = new HashSet<Guid>();
        var totalQty = 0;

        foreach (var item in items)
        {
            var remaining = item.QuantityTotal - item.QuantityDone;
            if (remaining <= 0) continue; // already done somehow; skip
            var result = await _completion.RecordAsync(item, userId, remaining, ct);
            totalQty += result.QuantityCompleted;
            if (result.OrderCompleted) completedOrders.Add(item.OrderId);
        }

        pkg.Status = LaundryPackageStatus.Completed;
        pkg.CompletedAt = now;
        pkg.CompletedByUserId = userId;

        _events.Record(
            PipelineEventTypes.LaundryPackageCompleted,
            new
            {
                packageId = pkg.Id,
                itemCount = items.Count,
                totalQuantity = totalQty,
                completedByUserId = userId,
            },
            userId: userId);

        return new CompleteResult
        {
            PackageId = pkg.Id,
            ItemCount = items.Count,
            TotalQuantity = totalQty,
            CompletedOrderIds = completedOrders.ToList(),
        };
    }
}
