using Confeccao.Api.Common.Events;
using Confeccao.Api.Common.Naming;
using Confeccao.Api.Common.Pipeline;
using Confeccao.Api.Infrastructure;
using Confeccao.Domain.Entities;
using Confeccao.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Confeccao.Api.Features.Orders;

public static class OrdersEndpoints
{
    public static void MapOrdersEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/orders");

        group.MapPost("/", CreateOrder);
        group.MapGet("/", ListOrders);
        group.MapGet("/{id:guid}", GetOrder);
    }

    private static async Task<IResult> CreateOrder(
        CreateOrderRequest request,
        ConfeccaoDbContext db,
        PipelineFlowService flow,
        PipelineEventLog events,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.FabricCode))
            return Results.BadRequest(new { error = "fabricCode is required." });

        if (request.Items is null || request.Items.Count == 0)
            return Results.BadRequest(new { error = "At least one order item is required." });

        if (request.Items.Any(i => i.PlannedQuantity <= 0))
            return Results.BadRequest(new { error = "PlannedQuantity must be positive on every item." });

        var color = await db.Colors
            .Where(c => c.Id == request.ColorId && c.Active)
            .FirstOrDefaultAsync(ct);
        if (color is null) return Results.BadRequest(new { error = "Unknown or inactive colorId." });

        var modelIds = request.Items.Select(i => i.ModelId).Distinct().ToList();
        var models = await db.Models
            .Where(m => modelIds.Contains(m.Id) && m.Active)
            .ToDictionaryAsync(m => m.Id, ct);
        if (models.Count != modelIds.Count)
            return Results.BadRequest(new { error = "One or more modelIds are unknown or inactive." });

        // Detect duplicate (model, size) entries — DB will reject, but the friendlier error is better.
        var dupes = request.Items
            .GroupBy(i => (i.ModelId, i.Size))
            .Where(g => g.Count() > 1)
            .ToList();
        if (dupes.Count > 0)
            return Results.BadRequest(new { error = "Duplicate (modelId, size) entries are not allowed." });

        var orderId = Guid.NewGuid();
        var order = new Order
        {
            Id = orderId,
            FabricCode = request.FabricCode.Trim(),
            ColorId = color.Id,
            Instructions = string.IsNullOrWhiteSpace(request.Instructions) ? null : request.Instructions.Trim(),
            Status = OrderStatus.AwaitingCutting,
        };
        db.Orders.Add(order);

        foreach (var itemReq in request.Items)
        {
            var orderItem = new OrderItem
            {
                Id = Guid.NewGuid(),
                OrderId = orderId,
                ModelId = itemReq.ModelId,
                Size = itemReq.Size,
                PlannedQuantity = itemReq.PlannedQuantity,
            };
            db.OrderItems.Add(orderItem);

            // Cutting items start InProgress: the cutter gets them directly, no dispatch needed.
            var firstStage = await flow.GetFirstStageAsync(itemReq.ModelId, ct);
            db.PipelineItems.Add(new PipelineItem
            {
                Id = Guid.NewGuid(),
                OrderId = orderId,
                OrderItemId = orderItem.Id,
                ModelId = itemReq.ModelId,
                Size = itemReq.Size,
                ColorId = color.Id,
                ColorNameSnapshot = color.Name,
                FabricCodeSnapshot = order.FabricCode,
                Stage = firstStage,
                Status = PipelineItemStatus.InProgress,
                QuantityTotal = itemReq.PlannedQuantity,
                QuantityDone = 0,
                DispatchedAt = DateTimeOffset.UtcNow,
            });
        }

        events.Record(
            PipelineEventTypes.OrderCreated,
            new
            {
                orderId,
                fabricCode = order.FabricCode,
                colorName = color.Name,
                items = request.Items.Select(i => new { i.ModelId, size = i.Size.ToString(), i.PlannedQuantity }),
            },
            orderId: orderId);

        await db.SaveChangesAsync(ct);

        return Results.Created($"/api/v1/orders/{orderId}", await BuildOrderDto(db, orderId, ct));
    }

    private static async Task<IResult> ListOrders(ConfeccaoDbContext db, string? status, CancellationToken ct)
    {
        var query = db.Orders.AsQueryable();
        if (!string.IsNullOrWhiteSpace(status))
        {
            if (!Enum.TryParse<OrderStatus>(status, ignoreCase: true, out var parsed))
                return Results.BadRequest(new { error = $"Unknown status '{status}'." });
            query = query.Where(o => o.Status == parsed);
        }

        var rows = await query
            .OrderByDescending(o => o.Number)
            .Select(o => new
            {
                o.Id,
                o.Number,
                o.FabricCode,
                ColorName = o.Color!.Name,
                ColorHex = o.Color.HexCode,
                o.Status,
                o.CreatedAt,
                ItemCount = o.Items.Count,
                PlannedTotal = o.Items.Sum(i => i.PlannedQuantity),
            })
            .ToListAsync(ct);

        var orders = rows.Select(r => new OrderSummaryDto(
            r.Id, r.Number, r.FabricCode, r.ColorName, r.ColorHex,
            KebabCase.From(r.Status), r.CreatedAt, r.ItemCount, r.PlannedTotal)).ToList();

        return Results.Ok(orders);
    }

    private static async Task<IResult> GetOrder(Guid id, ConfeccaoDbContext db, CancellationToken ct)
    {
        var dto = await BuildOrderDto(db, id, ct);
        return dto is null ? Results.NotFound() : Results.Ok(dto);
    }

    private static async Task<OrderDetailDto?> BuildOrderDto(ConfeccaoDbContext db, Guid id, CancellationToken ct)
    {
        var order = await db.Orders
            .Include(o => o.Color)
            .Include(o => o.Items).ThenInclude(i => i.Model)
            .FirstOrDefaultAsync(o => o.Id == id, ct);

        if (order is null) return null;

        return new OrderDetailDto(
            order.Id,
            order.Number,
            order.FabricCode,
            order.Color!.Name,
            order.Color.HexCode,
            order.Color.HasLining,
            order.Instructions,
            KebabCase.From(order.Status),
            order.CreatedAt,
            order.CompletedAt,
            order.Items.Select(i => new OrderItemDto(
                i.Id,
                i.ModelId,
                i.Model!.Name,
                i.Size.ToString(),
                i.PlannedQuantity
            )).ToList()
        );
    }
}

public record CreateOrderRequest(
    string FabricCode,
    Guid ColorId,
    string? Instructions,
    List<CreateOrderItem> Items);

public record CreateOrderItem(Guid ModelId, Size Size, int PlannedQuantity);

public record OrderSummaryDto(
    Guid Id,
    int Number,
    string FabricCode,
    string ColorName,
    string ColorHex,
    string Status,
    DateTimeOffset CreatedAt,
    int ItemCount,
    int PlannedTotal);

public record OrderDetailDto(
    Guid Id,
    int Number,
    string FabricCode,
    string ColorName,
    string ColorHex,
    bool ColorHasLining,
    string? Instructions,
    string Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset? CompletedAt,
    IReadOnlyList<OrderItemDto> Items);

public record OrderItemDto(Guid Id, Guid ModelId, string ModelName, string Size, int PlannedQuantity);
