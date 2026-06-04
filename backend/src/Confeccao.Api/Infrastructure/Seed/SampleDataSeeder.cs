using Confeccao.Api.Common.Pipeline;
using Confeccao.Domain.Entities;
using Confeccao.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Confeccao.Api.Infrastructure.Seed;

/// <summary>
/// Optional sample data: when <c>SEED_SAMPLE_DATA=true</c>, inserts a couple of
/// demo orders sitting at <see cref="OrderStatus.AwaitingCutting"/> so the
/// dashboard isn't empty on a fresh boot. Idempotent — checks for a sentinel
/// fabric code prefix before inserting.
/// </summary>
public static class SampleDataSeeder
{
    private const string DemoPrefix = "DEMO-";

    public static async Task SeedAsync(
        ConfeccaoDbContext db,
        PipelineFlowService flow,
        CancellationToken ct = default)
    {
        if (await db.Orders.AnyAsync(o => o.FabricCode.StartsWith(DemoPrefix), ct))
            return; // Already seeded.

        var color = await db.Colors.FirstAsync(c => c.Name == "Verde militar", ct);
        var pantalona = await db.Models.FirstAsync(m => m.Name == "Calça pantalona", ct);
        var chemise = await db.Models.FirstAsync(m => m.Name == "Vestido chemise", ct);
        var coleteColor = await db.Colors.FirstAsync(c => c.Name == "Preto", ct);
        var colete = await db.Models.FirstAsync(m => m.Name == "Colete alfaiataria", ct);

        await SeedOrderAsync(db, flow, "DEMO-001", color, [
            (pantalona, Size.M, 30),
            (chemise, Size.P, 20),
        ], ct);

        await SeedOrderAsync(db, flow, "DEMO-002", coleteColor, [
            (colete, Size.G, 15),
        ], ct);

        await db.SaveChangesAsync(ct);
    }

    private static async Task SeedOrderAsync(
        ConfeccaoDbContext db,
        PipelineFlowService flow,
        string fabricCode,
        Color color,
        (Model Model, Size Size, int Qty)[] items,
        CancellationToken ct)
    {
        var orderId = Guid.NewGuid();
        var order = new Order
        {
            Id = orderId,
            FabricCode = fabricCode,
            ColorId = color.Id,
            Instructions = "Pedido de demonstração — seed inicial.",
            Status = OrderStatus.AwaitingCutting,
        };
        db.Orders.Add(order);

        foreach (var (model, size, qty) in items)
        {
            var orderItemId = Guid.NewGuid();
            db.OrderItems.Add(new OrderItem
            {
                Id = orderItemId,
                OrderId = orderId,
                ModelId = model.Id,
                Size = size,
                PlannedQuantity = qty,
            });
            var firstStage = await flow.GetFirstStageAsync(model.Id, ct);
            db.PipelineItems.Add(new PipelineItem
            {
                Id = Guid.NewGuid(),
                OrderId = orderId,
                OrderItemId = orderItemId,
                ModelId = model.Id,
                Size = size,
                ColorId = color.Id,
                ColorNameSnapshot = color.Name,
                FabricCodeSnapshot = fabricCode,
                Stage = firstStage,
                Status = PipelineItemStatus.InProgress,
                QuantityTotal = qty,
                QuantityDone = 0,
                DispatchedAt = DateTimeOffset.UtcNow,
            });
        }
    }
}
