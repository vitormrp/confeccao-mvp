using Confeccao.Api.Infrastructure;
using Confeccao.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Confeccao.Api.Common.Pricing;

/// <summary>
/// Pure pricing engine — replays the rules from the prototype's
/// <c>calcularCredito</c> against the active <see cref="Domain.Entities.Price"/>
/// row for a user at a given timestamp.
///
/// Rules (mirrors the seed data shape):
/// <list type="bullet">
///   <item><b>Base</b>: <c>price.Amount * quantity</c>.</item>
///   <item><b>Tiers</b> (cutting only): if any <see cref="Domain.Entities.PriceTier"/>
///     rows exist, pick the lowest <c>UpToQuantity</c> that's ≥ <paramref name="quantity"/>;
///     that tier's amount replaces the base.</item>
///   <item><b>Lining extra</b> (sewing): if the color has lining and the price has
///     <c>LiningExtra</c>, add <c>liningExtra * quantity</c>.</item>
///   <item><b>Interfacing extra</b> (sewing): if the model's flow includes interfacing
///     and the price has <c>InterfacingExtra</c>, add <c>interfacingExtra * quantity</c>.</item>
///   <item><b>Buttoning</b>: completely overrides the base computation —
///     <c>buttonPrice * model.ButtonCount * quantity</c>, where <c>buttonPrice</c>
///     is <c>ReadyButtonPrice</c> when the color is Preto/Branco, else
///     <c>CoveredButtonPrice</c>. Returns 0 if model has no buttons.</item>
/// </list>
///
/// Returns 0 if no price is configured for the user at the given time.
/// </summary>
public class PricingEngine
{
    private readonly ConfeccaoDbContext _db;

    public PricingEngine(ConfeccaoDbContext db) => _db = db;

    public async Task<decimal> ComputeAsync(
        Guid userId,
        StageCode stage,
        Guid modelId,
        Guid colorId,
        int quantity,
        DateTimeOffset at,
        CancellationToken ct = default)
    {
        if (quantity <= 0) return 0m;

        var price = await _db.Prices
            .Include(p => p.Tiers)
            .Where(p => p.UserId == userId && p.EffectiveFrom <= at)
            .OrderByDescending(p => p.EffectiveFrom)
            .FirstOrDefaultAsync(ct);
        if (price is null) return 0m;

        var color = await _db.Colors.FirstOrDefaultAsync(c => c.Id == colorId, ct);
        var model = await _db.Models.FirstOrDefaultAsync(m => m.Id == modelId, ct);
        if (model is null) return 0m;

        // Buttoning overrides everything — only the button-specific calculation applies.
        if (stage == StageCode.Buttoning)
        {
            if (model.ButtonCount <= 0) return 0m;
            var isReady = color is not null && (color.Name == "Preto" || color.Name == "Branco");
            var perButton = isReady
                ? price.ReadyButtonPrice
                : price.CoveredButtonPrice;
            return (perButton ?? 0m) * model.ButtonCount * quantity;
        }

        // Base, possibly replaced by a volume tier for cutting.
        var basePerPiece = price.Amount;
        if (stage == StageCode.Cutting && price.Tiers.Count > 0)
        {
            // Walk tiers smallest-first; pick the first whose UpToQuantity ≥ quantity.
            // Fall back to the largest tier if quantity exceeds them all.
            var tier = price.Tiers
                .OrderBy(t => t.UpToQuantity)
                .FirstOrDefault(t => quantity <= t.UpToQuantity);
            tier ??= price.Tiers.OrderByDescending(t => t.UpToQuantity).First();
            basePerPiece = tier.Amount;
        }

        var amount = basePerPiece * quantity;

        if (stage == StageCode.Sewing)
        {
            if (color is { HasLining: true } && price.LiningExtra is { } liningExtra)
                amount += liningExtra * quantity;

            // Does the model's flow include Interfacing?
            var hasInterfacing = await _db.ModelStages
                .AnyAsync(s => s.ModelId == modelId && s.Stage == StageCode.Interfacing, ct);
            if (hasInterfacing && price.InterfacingExtra is { } interfacingExtra)
                amount += interfacingExtra * quantity;
        }

        return amount;
    }
}
