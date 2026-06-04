using Confeccao.Domain.Enums;

namespace Confeccao.Domain.Entities;

public class Order
{
    public Guid Id { get; set; }

    /// <summary>Sequential, human-friendly order number (#1, #2, ...).</summary>
    public int Number { get; set; }

    /// <summary>Fabric code provided by the manager — corresponds to "código do tecido".</summary>
    public required string FabricCode { get; set; }

    public Guid ColorId { get; set; }
    public Color? Color { get; set; }

    public string? Instructions { get; set; }

    public OrderStatus Status { get; set; } = OrderStatus.AwaitingCutting;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }

    public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
}
