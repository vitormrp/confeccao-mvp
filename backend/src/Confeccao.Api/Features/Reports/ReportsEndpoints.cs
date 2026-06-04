using Confeccao.Api.Common.Naming;
using Confeccao.Api.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Confeccao.Api.Features.Reports;

public static class ReportsEndpoints
{
    public static void MapReportsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/reports");
        group.MapGet("/operators", GetOperatorProductivity);
        group.MapGet("/orders", GetOrderThroughput);
    }

    /// <summary>
    /// Per-user productivity over the last N days. Aggregates pieces completed +
    /// amount earned from <c>credits</c>, then groups by stage so the manager can
    /// see who did what.
    /// </summary>
    private static async Task<IResult> GetOperatorProductivity(
        ConfeccaoDbContext db,
        int? days,
        CancellationToken ct)
    {
        var window = days is > 0 ? days.Value : 30;
        var since = DateTimeOffset.UtcNow.AddDays(-window);

        var rows = await db.Credits
            .Where(c => c.OccurredAt >= since)
            .GroupBy(c => new { c.UserId, c.Stage })
            .Select(g => new
            {
                g.Key.UserId,
                g.Key.Stage,
                Pieces = g.Sum(c => c.Quantity),
                Amount = g.Sum(c => c.Amount),
                Count = g.Count(),
            })
            .ToListAsync(ct);

        var userIds = rows.Select(r => r.UserId).Distinct().ToList();
        var users = await db.Users
            .Where(u => userIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, ct);

        var byUser = rows
            .GroupBy(r => r.UserId)
            .Select(g =>
            {
                var u = users[g.Key];
                return new OperatorReportDto(
                    g.Key,
                    u.Name,
                    KebabCase.From(u.Role),
                    g.Sum(r => r.Pieces),
                    g.Sum(r => r.Amount),
                    g.Sum(r => r.Count),
                    g.Select(r => new OperatorReportStageDto(
                            KebabCase.From(r.Stage),
                            r.Pieces,
                            r.Amount))
                        .OrderBy(s => s.Stage)
                        .ToList());
            })
            .OrderByDescending(r => r.TotalAmount)
            .ToList();

        return Results.Ok(new OperatorReportResponse(window, since, byUser));
    }

    /// <summary>
    /// Headline order throughput stats over the same window.
    /// </summary>
    private static async Task<IResult> GetOrderThroughput(
        ConfeccaoDbContext db,
        int? days,
        CancellationToken ct)
    {
        var window = days is > 0 ? days.Value : 30;
        var since = DateTimeOffset.UtcNow.AddDays(-window);

        var ordersCreated = await db.Orders.CountAsync(o => o.CreatedAt >= since, ct);
        var ordersCompleted = await db.Orders.CountAsync(
            o => o.CompletedAt != null && o.CompletedAt >= since, ct);

        // Average days from order creation to completion for completed orders in window.
        decimal? avgLeadDays = null;
        var completedInWindow = await db.Orders
            .Where(o => o.CompletedAt != null && o.CompletedAt >= since)
            .Select(o => new { o.CreatedAt, o.CompletedAt })
            .ToListAsync(ct);
        if (completedInWindow.Count > 0)
        {
            var totalDays = completedInWindow.Sum(o => (o.CompletedAt!.Value - o.CreatedAt).TotalDays);
            avgLeadDays = (decimal)Math.Round(totalDays / completedInWindow.Count, 2);
        }

        // Each garment passes through cutting exactly once — counting cutting credits
        // gives a clean "pieces started production" measure for the window.
        var piecesStarted = await db.Credits
            .Where(c => c.OccurredAt >= since && c.Stage == Confeccao.Domain.Enums.StageCode.Cutting)
            .SumAsync(c => (int?)c.Quantity, ct) ?? 0;

        return Results.Ok(new OrderThroughputDto(
            window,
            since,
            ordersCreated,
            ordersCompleted,
            avgLeadDays,
            piecesStarted));
    }
}

public record OperatorReportResponse(
    int WindowDays,
    DateTimeOffset Since,
    IReadOnlyList<OperatorReportDto> Operators);

public record OperatorReportDto(
    Guid UserId,
    string Name,
    string Role,
    int TotalPieces,
    decimal TotalAmount,
    int CreditCount,
    IReadOnlyList<OperatorReportStageDto> ByStage);

public record OperatorReportStageDto(string Stage, int Pieces, decimal Amount);

public record OrderThroughputDto(
    int WindowDays,
    DateTimeOffset Since,
    int OrdersCreated,
    int OrdersCompleted,
    decimal? AverageLeadTimeDays,
    int PiecesStarted);
