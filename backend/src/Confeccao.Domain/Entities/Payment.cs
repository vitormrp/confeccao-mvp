namespace Confeccao.Domain.Entities;

/// <summary>
/// A payment made to an operator covering a set of <see cref="Credit"/> entries.
/// Amount is derived from the sum of covered credits at creation time.
/// </summary>
public class Payment
{
    public Guid Id { get; set; }

    /// <summary>Sequential, human-friendly payment number (#1, #2, ...).</summary>
    public int Number { get; set; }

    public Guid UserId { get; set; }
    public User? User { get; set; }

    public decimal Amount { get; set; }

    public DateTimeOffset PaidAt { get; set; }
    public string? Note { get; set; }

    public ICollection<Credit> Credits { get; set; } = new List<Credit>();
}
