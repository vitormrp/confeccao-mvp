using Confeccao.Domain.Enums;

namespace Confeccao.Domain.Entities;

/// <summary>
/// One unit of work moving through the pipeline. Each (OrderItem × Stage × dispatch)
/// is a row; partial completions accumulate in <see cref="QuantityDone"/>.
///
/// State machine:
///  - Cutting items are created in <see cref="PipelineItemStatus.InProgress"/> when
///    the order is placed (no upstream stage to wait for).
///  - Other items are created in <see cref="PipelineItemStatus.AwaitingDispatch"/>
///    when the previous stage completes; the manager dispatches them to the next
///    operator (or, for laundry, bundles them into a package).
///  - When a stage completes, a new row at the next stage is created in
///    <see cref="PipelineItemStatus.AwaitingDispatch"/>.
///
/// Snapshots (color name, fabric code) are denormalized so historical PipelineItems
/// keep showing the right context even if the underlying Color/Order is edited.
/// </summary>
public class PipelineItem
{
    public Guid Id { get; set; }

    public Guid OrderId { get; set; }
    public Order? Order { get; set; }

    public Guid OrderItemId { get; set; }
    public OrderItem? OrderItem { get; set; }

    public Guid ModelId { get; set; }
    public Model? Model { get; set; }

    public Size Size { get; set; }

    public Guid ColorId { get; set; }
    public string ColorNameSnapshot { get; set; } = string.Empty;
    public string FabricCodeSnapshot { get; set; } = string.Empty;

    public StageCode Stage { get; set; }
    public PipelineItemStatus Status { get; set; }

    public int QuantityTotal { get; set; }
    public int QuantityDone { get; set; }

    /// <summary>Set when status transitions to InProgress; null while AwaitingDispatch.</summary>
    public DateTimeOffset? DispatchedAt { get; set; }

    /// <summary>Set when status reaches Done.</summary>
    public DateTimeOffset? CompletedAt { get; set; }

    /// <summary>
    /// User assigned to do this work. For sewing this is set on dispatch (the manager
    /// picks a specific seamstress). For other stages it's set when the operator
    /// confirms completion (so credits land on the right user).
    /// </summary>
    public Guid? AssignedUserId { get; set; }
    public User? AssignedUser { get; set; }

    /// <summary>Populated for washing items that were bundled into a laundry package.</summary>
    public Guid? LaundryPackageId { get; set; }
    public LaundryPackage? LaundryPackage { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}
