namespace Confeccao.Domain.Entities;

/// <summary>
/// Volume-based tier for a <see cref="Price"/> — only meaningful for Cutting roles.
/// Pick the tier with the smallest <see cref="UpToQuantity"/> that's &gt;= the cut quantity;
/// its <see cref="Amount"/> replaces the base <see cref="Price.Amount"/>.
/// </summary>
public class PriceTier
{
    public Guid Id { get; set; }
    public Guid PriceId { get; set; }
    public Price? Price { get; set; }

    public int UpToQuantity { get; set; }
    public decimal Amount { get; set; }
}
