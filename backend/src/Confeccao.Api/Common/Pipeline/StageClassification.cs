using Confeccao.Domain.Enums;

namespace Confeccao.Api.Common.Pipeline;

/// <summary>
/// Categorizes stages by how dispatch works at them. The pipeline state machine is
/// uniform, but the manager's dispatch flow differs:
///
/// - <see cref="StageDispatchKind.None"/>: no dispatch step — cutting items start
///   InProgress when the order is placed.
/// - <see cref="StageDispatchKind.Generic"/>: single-click send (interfacing,
///   buttoning, labeling, pressing). Manager confirms; item moves to InProgress.
/// - <see cref="StageDispatchKind.PerUser"/>: sewing — manager picks a specific
///   seamstress (Phase 4).
/// - <see cref="StageDispatchKind.Package"/>: washing — items are bundled into a
///   laundry package (Phase 5).
/// </summary>
public enum StageDispatchKind
{
    None,
    Generic,
    PerUser,
    Package,
}

public static class StageClassification
{
    public static StageDispatchKind DispatchKind(StageCode stage) => stage switch
    {
        StageCode.Cutting => StageDispatchKind.None,
        StageCode.Sewing => StageDispatchKind.PerUser,
        StageCode.Washing => StageDispatchKind.Package,
        StageCode.Interfacing or StageCode.Buttoning or StageCode.Labeling or StageCode.Pressing
            => StageDispatchKind.Generic,
        _ => throw new InvalidOperationException($"Unknown stage {stage}."),
    };

    /// <summary>
    /// True for stages whose operator queue + completion flow is uniform —
    /// interfacing, buttoning, labeling, pressing. Sewing follows the same
    /// completion flow but is dispatched per-user; the completion endpoint
    /// accepts both kinds.
    /// </summary>
    public static bool IsGenericQueueStage(StageCode stage) =>
        stage is StageCode.Interfacing or StageCode.Buttoning or StageCode.Labeling or StageCode.Pressing;

    /// <summary>
    /// True for stages where an operator records completion against a single
    /// PipelineItem (full or partial). All non-cutting stages qualify.
    /// </summary>
    public static bool AllowsItemCompletion(StageCode stage) =>
        stage != StageCode.Cutting;
}
