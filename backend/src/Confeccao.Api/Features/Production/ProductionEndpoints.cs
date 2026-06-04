using Confeccao.Api.Infrastructure;
using Confeccao.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Confeccao.Api.Features.Production;

public static class ProductionEndpoints
{
    public static void MapProductionEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/production");
        group.MapGet("/dashboard", GetDashboard);
    }

    private static async Task<IResult> GetDashboard(ConfeccaoDbContext db, CancellationToken ct)
    {
        var stages = Enum.GetValues<StageCode>();

        // Per-stage counts: sum of remaining (qty_total - qty_done) for non-Done items at that stage,
        // and sum of qty_done for Done items at that stage.
        var pipelineCounts = await db.PipelineItems
            .GroupBy(p => new { p.Stage, p.Status })
            .Select(g => new
            {
                g.Key.Stage,
                g.Key.Status,
                Remaining = g.Sum(p => p.QuantityTotal - p.QuantityDone),
                Done = g.Sum(p => p.QuantityDone),
            })
            .ToListAsync(ct);

        var byStage = stages.ToDictionary(
            s => s,
            s => new StageCounts(0, 0));

        foreach (var row in pipelineCounts)
        {
            var current = byStage[row.Stage];
            byStage[row.Stage] = row.Status == PipelineItemStatus.Done
                ? current with { Completed = current.Completed + row.Done }
                : current with { InProcess = current.InProcess + row.Remaining };
        }

        var ordersActive = await db.Orders.CountAsync(o => o.Status != OrderStatus.Completed, ct);
        var ordersCompleted = await db.Orders.CountAsync(o => o.Status == OrderStatus.Completed, ct);

        var stageDtos = stages
            .Select(s => new StageCountDto(
                s.ToString().ToLowerInvariant(),
                byStage[s].InProcess,
                byStage[s].Completed))
            .ToList();

        return Results.Ok(new DashboardDto(ordersActive, ordersCompleted, stageDtos));
    }

    private record StageCounts(int InProcess, int Completed);
}

public record DashboardDto(int OrdersActive, int OrdersCompleted, IReadOnlyList<StageCountDto> Stages);
public record StageCountDto(string Stage, int InProcess, int Completed);
