using Confeccao.Domain.Enums;

namespace Confeccao.Domain.Entities;

/// <summary>
/// A monetary credit owed to a user for work completed at a pipeline stage.
/// Generated automatically on every <c>StageCompleted</c> / partial completion
/// (and on cutter registration) by the pricing engine. <see cref="PaymentId"/>
/// becomes non-null once the credit is included in a payment.
/// </summary>
public class Credit
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }
    public User? User { get; set; }

    public Guid OrderId { get; set; }
    public Order? Order { get; set; }

    public Guid? PipelineItemId { get; set; }
    public PipelineItem? PipelineItem { get; set; }

    public StageCode Stage { get; set; }
    public Guid ModelId { get; set; }
    public Model? Model { get; set; }
    public Size Size { get; set; }

    public int Quantity { get; set; }
    public decimal Amount { get; set; }

    public DateTimeOffset OccurredAt { get; set; }

    public Guid? PaymentId { get; set; }
    public Payment? Payment { get; set; }
}
