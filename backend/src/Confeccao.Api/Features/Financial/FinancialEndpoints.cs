using Confeccao.Api.Common.Naming;
using Confeccao.Api.Infrastructure;
using Confeccao.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Confeccao.Api.Features.Financial;

public static class FinancialEndpoints
{
    public static void MapFinancialEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/financial");

        group.MapGet("/operators", GetOperators);
        group.MapGet("/operators/{userId:guid}/credits", GetCreditsForUser);
        group.MapPost("/payments", CreatePayment);
        group.MapGet("/payments", GetPayments);
        group.MapGet("/misc-costs", GetMiscCosts);
        group.MapPost("/misc-costs", CreateMiscCost);
        group.MapGet("/summary", GetSummary);
    }

    private static async Task<IResult> GetOperators(ConfeccaoDbContext db, CancellationToken ct)
    {
        // Per-user totals: unpaid balance, total paid, credit count.
        var rows = await db.Users
            .Where(u => u.Active)
            .Select(u => new
            {
                u.Id,
                u.Name,
                u.Role,
                Unpaid = db.Credits.Where(c => c.UserId == u.Id && c.PaymentId == null).Sum(c => (decimal?)c.Amount) ?? 0m,
                Paid = db.Credits.Where(c => c.UserId == u.Id && c.PaymentId != null).Sum(c => (decimal?)c.Amount) ?? 0m,
                UnpaidCount = db.Credits.Count(c => c.UserId == u.Id && c.PaymentId == null),
                LastCreditAt = (DateTimeOffset?)db.Credits
                    .Where(c => c.UserId == u.Id)
                    .Max(c => (DateTimeOffset?)c.OccurredAt),
            })
            .ToListAsync(ct);

        var dtos = rows
            .Where(r => r.Unpaid > 0 || r.Paid > 0)
            .OrderByDescending(r => r.Unpaid)
            .Select(r => new OperatorBalanceDto(
                r.Id,
                r.Name,
                KebabCase.From(r.Role),
                r.Unpaid,
                r.Paid,
                r.UnpaidCount,
                r.LastCreditAt))
            .ToList();

        return Results.Ok(dtos);
    }

    private static async Task<IResult> GetCreditsForUser(
        Guid userId,
        ConfeccaoDbContext db,
        string? status,
        CancellationToken ct)
    {
        var query = db.Credits.Where(c => c.UserId == userId);
        if (string.Equals(status, "unpaid", StringComparison.OrdinalIgnoreCase))
            query = query.Where(c => c.PaymentId == null);
        else if (string.Equals(status, "paid", StringComparison.OrdinalIgnoreCase))
            query = query.Where(c => c.PaymentId != null);

        var rows = await query
            .OrderByDescending(c => c.OccurredAt)
            .Select(c => new
            {
                c.Id,
                c.OrderId,
                OrderNumber = c.Order!.Number,
                c.Stage,
                ModelName = c.Model!.Name,
                c.Size,
                c.Quantity,
                c.Amount,
                c.OccurredAt,
                c.PaymentId,
            })
            .ToListAsync(ct);

        var dtos = rows.Select(r => new CreditDto(
            r.Id,
            r.OrderId,
            r.OrderNumber,
            KebabCase.From(r.Stage),
            r.ModelName,
            r.Size.ToString(),
            r.Quantity,
            r.Amount,
            r.OccurredAt,
            r.PaymentId)).ToList();

        return Results.Ok(dtos);
    }

    private static async Task<IResult> CreatePayment(
        CreatePaymentRequest request,
        ConfeccaoDbContext db,
        CancellationToken ct)
    {
        if (request.CreditIds is null || request.CreditIds.Count == 0)
            return Results.BadRequest(new { error = "Select at least one credit." });

        var credits = await db.Credits
            .Where(c => request.CreditIds.Contains(c.Id))
            .ToListAsync(ct);

        if (credits.Count != request.CreditIds.Count)
            return Results.BadRequest(new { error = "One or more creditIds are unknown." });

        if (credits.Any(c => c.UserId != request.UserId))
            return Results.BadRequest(new { error = "All credits must belong to the same userId." });

        if (credits.Any(c => c.PaymentId is not null))
            return Results.BadRequest(new { error = "One or more credits are already paid." });

        var payment = new Payment
        {
            Id = Guid.NewGuid(),
            UserId = request.UserId,
            Amount = credits.Sum(c => c.Amount),
            PaidAt = DateTimeOffset.UtcNow,
            Note = string.IsNullOrWhiteSpace(request.Note) ? null : request.Note.Trim(),
        };
        db.Payments.Add(payment);
        foreach (var credit in credits) credit.PaymentId = payment.Id;

        await db.SaveChangesAsync(ct);
        return Results.Ok(new PaymentDto(
            payment.Id,
            payment.Number,
            payment.UserId,
            payment.Amount,
            payment.PaidAt,
            payment.Note,
            credits.Count));
    }

    private static async Task<IResult> GetPayments(
        ConfeccaoDbContext db,
        Guid? userId,
        CancellationToken ct)
    {
        var query = db.Payments.AsQueryable();
        if (userId is not null) query = query.Where(p => p.UserId == userId);

        var payments = await query
            .OrderByDescending(p => p.Number)
            .Select(p => new PaymentDto(
                p.Id,
                p.Number,
                p.UserId,
                p.Amount,
                p.PaidAt,
                p.Note,
                p.Credits.Count))
            .ToListAsync(ct);
        return Results.Ok(payments);
    }

    private static async Task<IResult> GetMiscCosts(ConfeccaoDbContext db, CancellationToken ct)
    {
        var rows = await db.MiscCosts
            .OrderByDescending(m => m.Date)
            .Select(m => new MiscCostDto(m.Id, m.Description, m.Amount, m.Date, m.Category))
            .ToListAsync(ct);
        return Results.Ok(rows);
    }

    private static async Task<IResult> CreateMiscCost(
        CreateMiscCostRequest request,
        ConfeccaoDbContext db,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Description))
            return Results.BadRequest(new { error = "Description is required." });
        if (request.Amount <= 0)
            return Results.BadRequest(new { error = "Amount must be positive." });

        var cost = new MiscCost
        {
            Id = Guid.NewGuid(),
            Description = request.Description.Trim(),
            Amount = request.Amount,
            Date = request.Date ?? DateTimeOffset.UtcNow,
            Category = string.IsNullOrWhiteSpace(request.Category) ? null : request.Category.Trim(),
        };
        db.MiscCosts.Add(cost);
        await db.SaveChangesAsync(ct);
        return Results.Created($"/api/v1/financial/misc-costs/{cost.Id}",
            new MiscCostDto(cost.Id, cost.Description, cost.Amount, cost.Date, cost.Category));
    }

    private static async Task<IResult> GetSummary(ConfeccaoDbContext db, CancellationToken ct)
    {
        var totalUnpaid = await db.Credits.Where(c => c.PaymentId == null).SumAsync(c => (decimal?)c.Amount, ct) ?? 0m;
        var totalPaid = await db.Payments.SumAsync(p => (decimal?)p.Amount, ct) ?? 0m;
        var totalMiscCosts = await db.MiscCosts.SumAsync(m => (decimal?)m.Amount, ct) ?? 0m;
        var operatorsWithBalance = await db.Credits
            .Where(c => c.PaymentId == null)
            .Select(c => c.UserId)
            .Distinct()
            .CountAsync(ct);

        return Results.Ok(new SummaryDto(totalUnpaid, totalPaid, totalMiscCosts, operatorsWithBalance));
    }
}

public record OperatorBalanceDto(
    Guid UserId,
    string Name,
    string Role,
    decimal UnpaidBalance,
    decimal LifetimePaid,
    int UnpaidCount,
    DateTimeOffset? LastCreditAt);

public record CreditDto(
    Guid Id,
    Guid OrderId,
    int OrderNumber,
    string Stage,
    string ModelName,
    string Size,
    int Quantity,
    decimal Amount,
    DateTimeOffset OccurredAt,
    Guid? PaymentId);

public record CreatePaymentRequest(Guid UserId, List<Guid> CreditIds, string? Note);

public record PaymentDto(
    Guid Id,
    int Number,
    Guid UserId,
    decimal Amount,
    DateTimeOffset PaidAt,
    string? Note,
    int CreditCount);

public record CreateMiscCostRequest(string Description, decimal Amount, DateTimeOffset? Date, string? Category);

public record MiscCostDto(Guid Id, string Description, decimal Amount, DateTimeOffset Date, string? Category);

public record SummaryDto(
    decimal TotalUnpaid,
    decimal TotalPaid,
    decimal TotalMiscCosts,
    int OperatorsWithBalance);
