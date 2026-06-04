using Confeccao.Domain.Enums;

namespace Confeccao.Domain.Entities;

/// <summary>
/// One row of the cutting map: how many pieces of (model, size) the manager wants
/// the cutter to attempt. The actual cut quantity comes back as a PipelineItem
/// with stage=Cutting and is recorded by the cutter.
/// </summary>
public class OrderItem
{
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }
    public Order? Order { get; set; }

    public Guid ModelId { get; set; }
    public Model? Model { get; set; }

    public Size Size { get; set; }

    /// <summary>Planned quantity the cutter should aim for ("múltiplo" in the prototype).</summary>
    public int PlannedQuantity { get; set; }
}
