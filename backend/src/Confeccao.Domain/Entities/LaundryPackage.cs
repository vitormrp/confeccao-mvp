using Confeccao.Domain.Enums;

namespace Confeccao.Domain.Entities;

/// <summary>
/// A bundle of washing-stage PipelineItems sent to lavanderia as a single unit.
/// The constituent items are linked via <see cref="PipelineItem.LaundryPackageId"/>;
/// when the manager marks the package completed every item completes as a batch
/// and spawns its respective next-stage row.
/// </summary>
public class LaundryPackage
{
    public Guid Id { get; set; }

    /// <summary>Sequential, human-friendly package number (#1, #2, ...).</summary>
    public int Number { get; set; }

    public LaundryPackageStatus Status { get; set; } = LaundryPackageStatus.Awaiting;

    public DateTimeOffset SentAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }

    /// <summary>User who completed the package (the lavanderia operator). Null until done.</summary>
    public Guid? CompletedByUserId { get; set; }
    public User? CompletedByUser { get; set; }
}
