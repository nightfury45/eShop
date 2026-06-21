using System.Security.Claims;
using eShop.Admin.API.Services;
using Microsoft.AspNetCore.Http.HttpResults;

namespace eShop.Admin.API.Apis;

public static class InventoryApi
{
    public static IEndpointRouteBuilder MapInventoryApi(this IEndpointRouteBuilder app)
    {
        // All inventory management requires an authenticated administrator.
        var group = app.MapGroup("/api/admin/inventory").RequireAuthorization(AdminApi.AdminPolicy);

        group.MapGet(string.Empty, GetInventory);
        group.MapPost("/{id:int}/adjust", AdjustStock);

        return app;
    }

    public static async Task<Ok<InventoryResult>> GetInventory(
        IInventoryService inventory,
        int? page,
        int? pageSize,
        string? search,
        bool? lowStockOnly,
        CancellationToken cancellationToken)
    {
        var query = new InventoryQuery(
            PageIndex: Math.Max(page ?? 0, 0),
            PageSize: Math.Clamp(pageSize ?? 10, 1, 100),
            Search: string.IsNullOrWhiteSpace(search) ? null : search,
            LowStockOnly: lowStockOnly ?? false);

        var result = await inventory.GetInventoryAsync(query, cancellationToken);
        return TypedResults.Ok(result);
    }

    public static async Task<Results<Ok<InventoryItem>, NotFound, ValidationProblem>> AdjustStock(
        IInventoryService inventory,
        ClaimsPrincipal user,
        int id,
        StockAdjustment request,
        CancellationToken cancellationToken)
    {
        if (Validate(request) is { Count: > 0 } errors)
        {
            return TypedResults.ValidationProblem(errors);
        }

        var editor = user.FindFirstValue("name") ?? user.FindFirstValue("sub") ?? "unknown";
        var updated = await inventory.AdjustAsync(id, request, editor, cancellationToken);
        return updated is null ? TypedResults.NotFound() : TypedResults.Ok(updated);
    }

    private static Dictionary<string, string[]> Validate(StockAdjustment request)
    {
        var errors = new Dictionary<string, string[]>();
        if (request.NewOnHand < 0)
        {
            errors[nameof(request.NewOnHand)] = ["On-hand quantity cannot be negative."];
        }
        if (string.IsNullOrWhiteSpace(request.Reason))
        {
            errors[nameof(request.Reason)] = ["A reason for the adjustment is required."];
        }
        return errors;
    }
}
