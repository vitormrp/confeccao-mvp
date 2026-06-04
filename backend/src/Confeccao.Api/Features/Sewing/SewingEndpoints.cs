using Confeccao.Api.Common.CurrentUser;
using Confeccao.Api.Infrastructure;
using Confeccao.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Confeccao.Api.Features.Sewing;

public static class SewingEndpoints
{
    public static void MapSewingEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/sewing");
        group.MapGet("/queue", GetQueue);
    }

    /// <summary>
    /// Returns sewing items assigned to the calling user (X-User-Id header).
    /// Unlike the generic stage queue, sewing items are partitioned per
    /// seamstress because they're dispatched by the manager to a specific user.
    /// </summary>
    private static async Task<IResult> GetQueue(
        ConfeccaoDbContext db,
        ICurrentUserContext currentUser,
        CancellationToken ct)
    {
        var userId = currentUser.UserId;
        if (userId is null)
            return Results.BadRequest(new { error = "X-User-Id header is required." });

        var rows = await db.PipelineItems
            .Where(p => p.Stage == StageCode.Sewing
                     && p.Status == PipelineItemStatus.InProgress
                     && p.AssignedUserId == userId)
            .OrderBy(p => p.DispatchedAt)
            .Select(p => new
            {
                p.Id,
                p.OrderId,
                OrderNumber = p.Order!.Number,
                ModelName = p.Model!.Name,
                Size = p.Size,
                p.ColorNameSnapshot,
                ColorHex = p.Order!.Color!.HexCode,
                p.FabricCodeSnapshot,
                ColorHasLining = p.Order!.Color!.HasLining,
                p.QuantityTotal,
                p.QuantityDone,
                p.DispatchedAt,
            })
            .ToListAsync(ct);

        var dtos = rows.Select(r => new SewingQueueItemDto(
            r.Id,
            r.OrderId,
            r.OrderNumber,
            r.ModelName,
            r.Size.ToString(),
            r.ColorNameSnapshot,
            r.ColorHex,
            r.ColorHasLining,
            r.FabricCodeSnapshot,
            r.QuantityTotal,
            r.QuantityDone,
            r.DispatchedAt)).ToList();

        return Results.Ok(dtos);
    }
}

public record SewingQueueItemDto(
    Guid PipelineItemId,
    Guid OrderId,
    int OrderNumber,
    string ModelName,
    string Size,
    string ColorName,
    string ColorHex,
    bool ColorHasLining,
    string FabricCode,
    int QuantityTotal,
    int QuantityDone,
    DateTimeOffset? DispatchedAt);
