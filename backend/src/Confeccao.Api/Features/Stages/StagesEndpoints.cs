using Confeccao.Api.Common.CurrentUser;
using Confeccao.Api.Common.Naming;
using Confeccao.Api.Common.Pipeline;
using Confeccao.Api.Infrastructure;
using Confeccao.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Confeccao.Api.Features.Stages;

public static class StagesEndpoints
{
    public static void MapStagesEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/stages");
        group.MapGet("/{stage}/queue", GetQueue);
        group.MapPost("/{stage}/items/{pipelineItemId:guid}/complete", CompleteItem);
    }

    /// <summary>Returns InProgress pipeline items at the given generic stage.</summary>
    private static async Task<IResult> GetQueue(string stage, ConfeccaoDbContext db, CancellationToken ct)
    {
        if (!TryResolveStage(stage, out var stageCode, out var problem)) return problem;
        if (!StageClassification.IsGenericQueueStage(stageCode))
            return Results.BadRequest(new { error = $"Stage '{stage}' has a specialized queue (use the cutter or sewing endpoints)." });

        var rows = await db.PipelineItems
            .Where(p => p.Stage == stageCode && p.Status == PipelineItemStatus.InProgress)
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

        var dtos = rows.Select(r => new StageQueueItemDto(
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

    private static async Task<IResult> CompleteItem(
        string stage,
        Guid pipelineItemId,
        CompleteItemRequest request,
        ConfeccaoDbContext db,
        ICurrentUserContext currentUser,
        StageCompletionService completion,
        CancellationToken ct)
    {
        if (!TryResolveStage(stage, out var stageCode, out var problem)) return problem;
        if (!StageClassification.AllowsItemCompletion(stageCode))
            return Results.BadRequest(new { error = $"Stage '{stage}' does not use item-level completion." });

        var userId = currentUser.UserId;
        if (userId is null) return Results.BadRequest(new { error = "X-User-Id header is required." });

        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId, ct);
        var expectedRole = stageCode switch
        {
            StageCode.Interfacing => UserRole.Interfacing,
            StageCode.Sewing => UserRole.Sewing,
            StageCode.Buttoning => UserRole.Buttoning,
            StageCode.Labeling => UserRole.Labeling,
            StageCode.Pressing => UserRole.Pressing,
            _ => (UserRole?)null,
        };
        if (user is null || expectedRole is null || user.Role != expectedRole)
            return Results.BadRequest(new
            {
                error = $"Acting user must have role '{KebabCase.From(expectedRole ?? UserRole.Manager)}'.",
            });

        var item = await db.PipelineItems
            .FirstOrDefaultAsync(p => p.Id == pipelineItemId, ct);
        if (item is null) return Results.NotFound();
        if (item.Stage != stageCode)
            return Results.BadRequest(new
            {
                error = $"Pipeline item belongs to stage '{KebabCase.From(item.Stage)}', not '{stage}'.",
            });

        try
        {
            var result = await completion.RecordAsync(item, userId.Value, request.Quantity, ct);
            await db.SaveChangesAsync(ct);

            return Results.Ok(new CompleteItemResponse(
                result.Item.Id,
                result.QuantityCompleted,
                result.Item.QuantityDone,
                result.Item.QuantityTotal,
                result.StageDone,
                result.SpawnedNext is null ? null : KebabCase.From(result.SpawnedNext.Stage),
                result.OrderCompleted));
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }
    }

    private static bool TryResolveStage(string stage, out StageCode stageCode, out IResult problem)
    {
        problem = null!;
        // Accept kebab-case ("cutting") and PascalCase ("Cutting").
        var normalized = stage.Replace("-", string.Empty);
        if (!Enum.TryParse(normalized, ignoreCase: true, out stageCode))
        {
            problem = Results.NotFound(new { error = $"Unknown stage '{stage}'." });
            return false;
        }
        return true;
    }
}

public record CompleteItemRequest(int Quantity);

public record CompleteItemResponse(
    Guid PipelineItemId,
    int QuantityCompleted,
    int QuantityDone,
    int QuantityTotal,
    bool StageDone,
    string? NextStage,
    bool OrderCompleted);

public record StageQueueItemDto(
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
