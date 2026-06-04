using Confeccao.Api.Common.Events;
using Confeccao.Api.Common.Naming;
using Confeccao.Api.Common.Pipeline;
using Confeccao.Api.Infrastructure;
using Confeccao.Domain.Entities;
using Confeccao.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Confeccao.Api.Features.Dispatch;

public static class DispatchEndpoints
{
    public static void MapDispatchEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/dispatch");
        group.MapGet("/awaiting", GetAwaiting);
        group.MapPost("/generic", DispatchGeneric);
        group.MapPost("/sewing", DispatchSewing);
    }

    private static async Task<IResult> DispatchSewing(
        DispatchSewingRequest request,
        ConfeccaoDbContext db,
        SewingDispatchService sewing,
        CancellationToken ct)
    {
        var item = await db.PipelineItems.FirstOrDefaultAsync(p => p.Id == request.PipelineItemId, ct);
        if (item is null) return Results.NotFound(new { error = "pipelineItemId not found." });

        var seamstress = await db.Users.FirstOrDefaultAsync(u => u.Id == request.UserId, ct);
        if (seamstress is null) return Results.NotFound(new { error = "userId not found." });

        try
        {
            var result = await sewing.DispatchAsync(item, seamstress, request.Quantity, ct);
            await db.SaveChangesAsync(ct);
            return Results.Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }
    }

    private static async Task<IResult> GetAwaiting(ConfeccaoDbContext db, CancellationToken ct)
    {
        var rows = await db.PipelineItems
            .Where(p => p.Status == PipelineItemStatus.AwaitingDispatch)
            .OrderBy(p => p.Stage).ThenBy(p => p.CreatedAt)
            .Select(p => new
            {
                p.Id,
                p.OrderId,
                OrderNumber = p.Order!.Number,
                p.Stage,
                p.QuantityTotal,
                ModelName = p.Model!.Name,
                Size = p.Size,
                p.ColorNameSnapshot,
                ColorHex = p.Order!.Color!.HexCode,
                p.FabricCodeSnapshot,
                p.ColorId,
                LiningFlag = p.Order!.Color!.HasLining,
                p.CreatedAt,
            })
            .ToListAsync(ct);

        var dtos = rows.Select(r => new AwaitingItemDto(
            r.Id,
            r.OrderId,
            r.OrderNumber,
            KebabCase.From(r.Stage),
            KebabCase.From(StageClassification.DispatchKind(r.Stage)),
            r.QuantityTotal,
            r.ModelName,
            r.Size.ToString(),
            r.ColorNameSnapshot,
            r.ColorHex,
            r.FabricCodeSnapshot,
            r.LiningFlag,
            r.CreatedAt)).ToList();

        return Results.Ok(dtos);
    }

    private static async Task<IResult> DispatchGeneric(
        DispatchGenericRequest request,
        ConfeccaoDbContext db,
        PipelineEventLog events,
        CancellationToken ct)
    {
        var item = await db.PipelineItems
            .FirstOrDefaultAsync(p => p.Id == request.PipelineItemId, ct);
        if (item is null) return Results.NotFound();

        if (item.Status != PipelineItemStatus.AwaitingDispatch)
            return Results.BadRequest(new
            {
                error = $"Pipeline item is not awaiting dispatch (status: {item.Status}).",
            });

        if (StageClassification.DispatchKind(item.Stage) != StageDispatchKind.Generic)
            return Results.BadRequest(new
            {
                error = $"Stage {item.Stage} requires a specialized dispatch flow (sewing, washing, or none for cutting).",
            });

        item.Status = PipelineItemStatus.InProgress;
        item.DispatchedAt = DateTimeOffset.UtcNow;

        events.Record(
            PipelineEventTypes.Dispatched,
            new
            {
                pipelineItemId = item.Id,
                orderId = item.OrderId,
                stage = KebabCase.From(item.Stage),
                quantity = item.QuantityTotal,
                kind = "generic",
            },
            orderId: item.OrderId,
            pipelineItemId: item.Id);

        await db.SaveChangesAsync(ct);
        return Results.Ok(new { pipelineItemId = item.Id, stage = KebabCase.From(item.Stage) });
    }
}

public record DispatchGenericRequest(Guid PipelineItemId);

public record DispatchSewingRequest(Guid PipelineItemId, Guid UserId, int Quantity);

public record AwaitingItemDto(
    Guid PipelineItemId,
    Guid OrderId,
    int OrderNumber,
    string Stage,
    string DispatchKind,
    int Quantity,
    string ModelName,
    string Size,
    string ColorName,
    string ColorHex,
    string FabricCode,
    bool ColorHasLining,
    DateTimeOffset AwaitingSince);
