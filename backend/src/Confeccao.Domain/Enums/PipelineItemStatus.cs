namespace Confeccao.Domain.Enums;

/// <summary>
/// Lifecycle of a piece (or batch of pieces) at a particular pipeline stage.
///
/// - <see cref="AwaitingDispatch"/>: completed the previous stage, waiting for the
///   manager to send it onward. (What the prototype called a "lote pronto".)
/// - <see cref="InProgress"/>: dispatched to an operator (or a laundry package);
///   work is happening or queued at that stage.
/// - <see cref="Done"/>: this stage finished; if there's a next stage, a new
///   PipelineItem at that next stage was created in <see cref="AwaitingDispatch"/>.
///
/// Cutting is special: there's no upstream stage, so cutting items are created
/// directly in <see cref="InProgress"/> when the order is placed.
/// </summary>
public enum PipelineItemStatus
{
    AwaitingDispatch = 1,
    InProgress = 2,
    Done = 3,
}
