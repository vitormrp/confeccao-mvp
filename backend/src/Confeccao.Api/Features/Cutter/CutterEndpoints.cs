using Confeccao.Api.Common.CurrentUser;
using Confeccao.Api.Common.Events;
using Confeccao.Api.Common.Pipeline;
using Confeccao.Api.Common.Pricing;
using Confeccao.Api.Infrastructure;
using Confeccao.Domain.Entities;
using Confeccao.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Confeccao.Api.Features.Cutter;

public static class CutterEndpoints
{
    public static void MapCutterEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/cutting");
        group.MapGet("/queue", GetQueue);
        group.MapPost("/orders/{orderId:guid}/register", RegisterCut);
    }

    private static async Task<IResult> GetQueue(ConfeccaoDbContext db, CancellationToken ct)
    {
        var orders = await db.Orders
            .Where(o => o.Status == OrderStatus.AwaitingCutting)
            .Include(o => o.Color)
            .Include(o => o.Items).ThenInclude(i => i.Model)
            .OrderBy(o => o.Number)
            .ToListAsync(ct);

        var orderIds = orders.Select(o => o.Id).ToList();
        var cuttingItems = await db.PipelineItems
            .Where(p => orderIds.Contains(p.OrderId) && p.Stage == StageCode.Cutting)
            .ToListAsync(ct);

        // Pair each OrderItem with its (single) cutting PipelineItem.
        var pipelineByOrderItem = cuttingItems.ToDictionary(p => p.OrderItemId);

        var dtos = orders.Select(o => new CutterOrderDto(
            o.Id,
            o.Number,
            o.FabricCode,
            o.Color!.Name,
            o.Color.HexCode,
            o.Color.HasLining,
            o.Instructions,
            o.Items
                .OrderBy(i => i.Model!.Name).ThenBy(i => i.Size)
                .Select(i =>
                {
                    var pip = pipelineByOrderItem[i.Id];
                    return new CutterOrderItemDto(
                        i.Id,
                        pip.Id,
                        i.Model!.Name,
                        i.Size.ToString(),
                        i.PlannedQuantity);
                })
                .ToList()
        )).ToList();

        return Results.Ok(dtos);
    }

    private static async Task<IResult> RegisterCut(
        Guid orderId,
        RegisterCutRequest request,
        ConfeccaoDbContext db,
        ICurrentUserContext currentUser,
        PipelineFlowService flow,
        PipelineEventLog events,
        CreditGenerator credits,
        CancellationToken ct)
    {
        var userId = currentUser.UserId;
        if (userId is null) return Results.BadRequest(new { error = "X-User-Id header is required." });

        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId, ct);
        if (user is null || user.Role != UserRole.Cutting)
            return Results.BadRequest(new { error = "Acting user must have role 'cutting'." });

        if (request.Cuts is null || request.Cuts.Count == 0)
            return Results.BadRequest(new { error = "At least one cut entry is required." });

        if (request.Cuts.Any(c => c.QuantityCut < 0))
            return Results.BadRequest(new { error = "QuantityCut cannot be negative." });

        var order = await db.Orders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == orderId, ct);
        if (order is null) return Results.NotFound();
        if (order.Status != OrderStatus.AwaitingCutting)
            return Results.BadRequest(new { error = $"Order is not awaiting cutting (status: {order.Status})." });

        var orderItemIds = order.Items.Select(i => i.Id).ToHashSet();
        if (request.Cuts.Any(c => !orderItemIds.Contains(c.OrderItemId)))
            return Results.BadRequest(new { error = "One or more orderItemIds do not belong to this order." });

        var cuttingItems = await db.PipelineItems
            .Where(p => p.OrderId == orderId && p.Stage == StageCode.Cutting)
            .ToListAsync(ct);
        var cuttingByOrderItem = cuttingItems.ToDictionary(p => p.OrderItemId);

        // Items not mentioned in the request → treated as cut=0 (skipped onward).
        var cutsByOrderItem = request.Cuts.ToDictionary(c => c.OrderItemId, c => c.QuantityCut);

        var now = DateTimeOffset.UtcNow;
        var totalCut = 0;
        var spawned = new List<(StageCode stage, int qty)>();

        foreach (var orderItem in order.Items)
        {
            var cuttingItem = cuttingByOrderItem[orderItem.Id];
            cutsByOrderItem.TryGetValue(orderItem.Id, out var quantityCut);

            cuttingItem.QuantityTotal = quantityCut;
            cuttingItem.QuantityDone = quantityCut;
            cuttingItem.Status = PipelineItemStatus.Done;
            cuttingItem.AssignedUserId = userId;
            cuttingItem.CompletedAt = now;
            totalCut += quantityCut;

            if (quantityCut == 0) continue;

            // Cutter credit for this item — uses tier pricing.
            await credits.AccrueAsync(
                userId: userId.Value,
                orderId: orderId,
                pipelineItemId: cuttingItem.Id,
                stage: StageCode.Cutting,
                modelId: orderItem.ModelId,
                colorId: cuttingItem.ColorId,
                size: orderItem.Size,
                quantity: quantityCut,
                occurredAt: now,
                ct: ct);

            var nextStage = await flow.GetNextStageAsync(orderItem.ModelId, StageCode.Cutting, ct);
            if (nextStage is null) continue;

            db.PipelineItems.Add(new PipelineItem
            {
                Id = Guid.NewGuid(),
                OrderId = orderId,
                OrderItemId = orderItem.Id,
                ModelId = orderItem.ModelId,
                Size = orderItem.Size,
                ColorId = cuttingItem.ColorId,
                ColorNameSnapshot = cuttingItem.ColorNameSnapshot,
                FabricCodeSnapshot = cuttingItem.FabricCodeSnapshot,
                Stage = nextStage.Value,
                Status = PipelineItemStatus.AwaitingDispatch,
                QuantityTotal = quantityCut,
                QuantityDone = 0,
            });
            spawned.Add((nextStage.Value, quantityCut));
        }

        if (totalCut == 0)
            return Results.BadRequest(new { error = "Total cut quantity is zero — nothing to register." });

        order.Status = OrderStatus.InProduction;

        events.Record(
            PipelineEventTypes.CuttingRegistered,
            new
            {
                orderId,
                userId,
                totalCut,
                cuts = request.Cuts.Select(c => new { c.OrderItemId, c.QuantityCut }),
                spawnedNext = spawned.Select(s => new { stage = s.stage.ToString().ToLowerInvariant(), qty = s.qty }),
            },
            orderId: orderId,
            userId: userId);

        await db.SaveChangesAsync(ct);

        return Results.Ok(new RegisterCutResponse(orderId, totalCut, spawned.Count));
    }
}

public record RegisterCutRequest(List<RegisterCutEntry> Cuts);
public record RegisterCutEntry(Guid OrderItemId, int QuantityCut);
public record RegisterCutResponse(Guid OrderId, int TotalCut, int SpawnedNextStageItems);

public record CutterOrderDto(
    Guid OrderId,
    int Number,
    string FabricCode,
    string ColorName,
    string ColorHex,
    bool ColorHasLining,
    string? Instructions,
    IReadOnlyList<CutterOrderItemDto> Items);

public record CutterOrderItemDto(
    Guid OrderItemId,
    Guid PipelineItemId,
    string ModelName,
    string Size,
    int PlannedQuantity);
