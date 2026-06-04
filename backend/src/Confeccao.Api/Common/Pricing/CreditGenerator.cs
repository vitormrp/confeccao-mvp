using Confeccao.Api.Infrastructure;
using Confeccao.Domain.Entities;
using Confeccao.Domain.Enums;

namespace Confeccao.Api.Common.Pricing;

/// <summary>
/// Hook called by completion paths (StageCompletionService for non-cutting stages,
/// CutterEndpoints for cutting) to mint <see cref="Credit"/> rows from each unit
/// of work. Wraps <see cref="PricingEngine"/> with the bookkeeping of inserting
/// the right credit row into the change tracker.
///
/// Caller is responsible for SaveChanges.
/// </summary>
public class CreditGenerator
{
    private readonly ConfeccaoDbContext _db;
    private readonly PricingEngine _engine;

    public CreditGenerator(ConfeccaoDbContext db, PricingEngine engine)
    {
        _db = db;
        _engine = engine;
    }

    public async Task<Credit?> AccrueAsync(
        Guid userId,
        Guid orderId,
        Guid? pipelineItemId,
        StageCode stage,
        Guid modelId,
        Guid colorId,
        Size size,
        int quantity,
        DateTimeOffset occurredAt,
        CancellationToken ct = default)
    {
        if (quantity <= 0) return null;

        var amount = await _engine.ComputeAsync(userId, stage, modelId, colorId, quantity, occurredAt, ct);
        if (amount <= 0) return null; // No price configured — skip silently.

        var credit = new Credit
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            OrderId = orderId,
            PipelineItemId = pipelineItemId,
            Stage = stage,
            ModelId = modelId,
            Size = size,
            Quantity = quantity,
            Amount = amount,
            OccurredAt = occurredAt,
        };
        _db.Credits.Add(credit);
        return credit;
    }
}
