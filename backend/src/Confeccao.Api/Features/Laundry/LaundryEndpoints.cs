using Confeccao.Api.Common.CurrentUser;
using Confeccao.Api.Common.Naming;
using Confeccao.Api.Common.Pipeline;
using Confeccao.Api.Infrastructure;
using Confeccao.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Confeccao.Api.Features.Laundry;

public static class LaundryEndpoints
{
    public static void MapLaundryEndpoints(this IEndpointRouteBuilder app)
    {
        var dispatch = app.MapGroup("/api/v1/dispatch");
        dispatch.MapPost("/laundry-package", BundlePackage);

        var laundry = app.MapGroup("/api/v1/laundry");
        laundry.MapGet("/queue", GetQueue);
        laundry.MapPost("/packages/{packageId:guid}/complete", CompletePackage);
    }

    private static async Task<IResult> BundlePackage(
        BundlePackageRequest request,
        ConfeccaoDbContext db,
        LaundryPackageService service,
        CancellationToken ct)
    {
        if (request.PipelineItemIds is null || request.PipelineItemIds.Count == 0)
            return Results.BadRequest(new { error = "Select at least one item." });

        var items = await db.PipelineItems
            .Where(p => request.PipelineItemIds.Contains(p.Id))
            .ToListAsync(ct);

        if (items.Count != request.PipelineItemIds.Count)
            return Results.BadRequest(new { error = "One or more pipelineItemIds are unknown." });

        try
        {
            var result = await service.BundleAsync(items, ct);
            await db.SaveChangesAsync(ct);
            return Results.Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }
    }

    private static async Task<IResult> GetQueue(ConfeccaoDbContext db, CancellationToken ct)
    {
        var packages = await db.LaundryPackages
            .Where(p => p.Status == LaundryPackageStatus.Awaiting)
            .OrderBy(p => p.Number)
            .ToListAsync(ct);

        var pkgIds = packages.Select(p => p.Id).ToList();
        var itemRows = await db.PipelineItems
            .Where(p => p.LaundryPackageId != null && pkgIds.Contains(p.LaundryPackageId!.Value))
            .Select(p => new
            {
                p.LaundryPackageId,
                p.Id,
                p.OrderId,
                OrderNumber = p.Order!.Number,
                ModelName = p.Model!.Name,
                p.Size,
                p.ColorNameSnapshot,
                ColorHex = p.Order!.Color!.HexCode,
                p.QuantityTotal,
                p.QuantityDone,
            })
            .ToListAsync(ct);

        var byPackage = itemRows
            .GroupBy(r => r.LaundryPackageId!.Value)
            .ToDictionary(g => g.Key, g => g.ToList());

        var dtos = packages.Select(p =>
        {
            var items = byPackage.TryGetValue(p.Id, out var rows) ? rows : [];
            return new LaundryPackageDto(
                p.Id,
                p.Number,
                p.SentAt,
                items.Sum(r => r.QuantityTotal - r.QuantityDone),
                items.Select(r => new LaundryPackageItemDto(
                    r.Id,
                    r.OrderId,
                    r.OrderNumber,
                    r.ModelName,
                    r.Size.ToString(),
                    r.ColorNameSnapshot,
                    r.ColorHex,
                    r.QuantityTotal - r.QuantityDone)).ToList());
        }).ToList();

        return Results.Ok(dtos);
    }

    private static async Task<IResult> CompletePackage(
        Guid packageId,
        ConfeccaoDbContext db,
        ICurrentUserContext currentUser,
        LaundryPackageService service,
        CancellationToken ct)
    {
        var userId = currentUser.UserId;
        if (userId is null) return Results.BadRequest(new { error = "X-User-Id header is required." });

        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId, ct);
        if (user is null || user.Role != UserRole.Washing)
            return Results.BadRequest(new { error = "Acting user must have role 'washing'." });

        var pkg = await db.LaundryPackages.FirstOrDefaultAsync(p => p.Id == packageId, ct);
        if (pkg is null) return Results.NotFound();

        try
        {
            var result = await service.CompleteAsync(pkg, userId.Value, ct);
            await db.SaveChangesAsync(ct);
            return Results.Ok(new CompletePackageResponse(
                result.PackageId,
                result.ItemCount,
                result.TotalQuantity,
                result.CompletedOrderIds));
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }
    }
}

public record BundlePackageRequest(List<Guid> PipelineItemIds);

public record LaundryPackageDto(
    Guid PackageId,
    int Number,
    DateTimeOffset SentAt,
    int TotalQuantity,
    IReadOnlyList<LaundryPackageItemDto> Items);

public record LaundryPackageItemDto(
    Guid PipelineItemId,
    Guid OrderId,
    int OrderNumber,
    string ModelName,
    string Size,
    string ColorName,
    string ColorHex,
    int Quantity);

public record CompletePackageResponse(
    Guid PackageId,
    int ItemCount,
    int TotalQuantity,
    IReadOnlyList<Guid> CompletedOrderIds);
