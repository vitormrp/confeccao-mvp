namespace Confeccao.Domain.Entities;

/// <summary>
/// One-off costs the factory tracks alongside operator credits — fabric purchases,
/// utility bills, anything outside the standard pipeline pricing.
/// </summary>
public class MiscCost
{
    public Guid Id { get; set; }
    public required string Description { get; set; }
    public decimal Amount { get; set; }
    public DateTimeOffset Date { get; set; }
    public string? Category { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
