namespace Confeccao.Domain.Entities;

/// <summary>
/// Price configuration that became effective for a user at <see cref="EffectiveFrom"/>.
/// Prices are vigência-based: when computing a credit, the row chosen is the latest
/// for that user with EffectiveFrom &lt;= the credit timestamp.
///
/// Field applicability depends on the user's role:
/// - <see cref="Amount"/>: base per-piece amount (used by all roles except Buttoning).
/// - <see cref="Tiers"/>: volume tiers — only used by Cutting roles. When tiers are
///   present they replace <see cref="Amount"/> for that role.
/// - <see cref="LiningExtra"/>: extra paid per piece when the order's color has lining.
///   Applies to most stages.
/// - <see cref="InterfacingExtra"/>: extra paid per piece for Sewing roles when the
///   model's flow includes Interfacing.
/// - <see cref="CoveredButtonPrice"/> / <see cref="ReadyButtonPrice"/>: per-button rates
///   for Buttoning role; covered = encapado (color-matched), ready = pronto (off-the-shelf
///   for Black/White colors).
/// </summary>
public class Price
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public User? User { get; set; }

    public decimal Amount { get; set; }
    public decimal? LiningExtra { get; set; }
    public decimal? InterfacingExtra { get; set; }
    public decimal? CoveredButtonPrice { get; set; }
    public decimal? ReadyButtonPrice { get; set; }

    public DateTimeOffset EffectiveFrom { get; set; }
    public string? Note { get; set; }

    public ICollection<PriceTier> Tiers { get; set; } = new List<PriceTier>();
}
