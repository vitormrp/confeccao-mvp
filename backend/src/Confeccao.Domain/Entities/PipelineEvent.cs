namespace Confeccao.Domain.Entities;

/// <summary>
/// Append-only audit log of pipeline-relevant events. Reports + future notifications
/// read from this table rather than reconstructing history from current state.
/// </summary>
public class PipelineEvent
{
    public Guid Id { get; set; }

    public Guid? OrderId { get; set; }
    public Order? Order { get; set; }

    public Guid? PipelineItemId { get; set; }
    public PipelineItem? PipelineItem { get; set; }

    public Guid? UserId { get; set; }
    public User? User { get; set; }

    /// <summary>Event-type discriminator. See <see cref="PipelineEventTypes"/> for the canonical set.</summary>
    public required string EventType { get; set; }

    /// <summary>Free-form JSON blob for type-specific payload. Stored as Postgres jsonb.</summary>
    public required string PayloadJson { get; set; }

    public DateTimeOffset OccurredAt { get; set; }
}

public static class PipelineEventTypes
{
    public const string OrderCreated = "OrderCreated";
    public const string CuttingRegistered = "CuttingRegistered";
    public const string Dispatched = "Dispatched";
    public const string StageCompleted = "StageCompleted";
    public const string PartialCompletion = "PartialCompletion";
    public const string LaundryPackageSent = "LaundryPackageSent";
    public const string LaundryPackageCompleted = "LaundryPackageCompleted";
    public const string OrderCompleted = "OrderCompleted";
}
