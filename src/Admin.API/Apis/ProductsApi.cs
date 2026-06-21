using System.Security.Claims;
using eShop.Admin.API.Services;
using Microsoft.AspNetCore.Http.HttpResults;

namespace eShop.Admin.API.Apis;

public static class ProductsApi
{
    public static IEndpointRouteBuilder MapProductsApi(this IEndpointRouteBuilder app)
    {
        // All product management requires an authenticated administrator.
        var group = app.MapGroup("/api/admin/products").RequireAuthorization(AdminApi.AdminPolicy);

        group.MapGet(string.Empty, ListProducts);
        group.MapGet("/{id:int}", GetProduct);
        group.MapPut("/{id:int}", UpdateProduct);

        return app;
    }

    public static async Task<Ok<AdminProductsResult>> ListProducts(
        IProductCatalogService products,
        int? page,
        int? pageSize,
        string? search,
        int? category,
        int? brand,
        CancellationToken cancellationToken)
    {
        var query = new ProductQuery(
            PageIndex: Math.Max(page ?? 0, 0),
            PageSize: Math.Clamp(pageSize ?? 10, 1, 100),
            Name: string.IsNullOrWhiteSpace(search) ? null : search,
            CategoryId: category,
            BrandId: brand);

        var result = await products.ListAsync(query, cancellationToken);
        return TypedResults.Ok(result);
    }

    public static async Task<Results<Ok<AdminProduct>, NotFound>> GetProduct(
        IProductCatalogService products,
        int id,
        CancellationToken cancellationToken)
    {
        var product = await products.GetAsync(id, cancellationToken);
        return product is null ? TypedResults.NotFound() : TypedResults.Ok(product);
    }

    public static async Task<Results<Ok<AdminProduct>, NotFound, ValidationProblem>> UpdateProduct(
        IProductCatalogService products,
        ClaimsPrincipal user,
        int id,
        ProductUpdateRequest request,
        CancellationToken cancellationToken)
    {
        if (Validate(request) is { Count: > 0 } errors)
        {
            return TypedResults.ValidationProblem(errors);
        }

        var editor = user.FindFirstValue("name") ?? user.FindFirstValue("sub") ?? "unknown";
        var updated = await products.UpdateAsync(id, request, editor, cancellationToken);
        return updated is null ? TypedResults.NotFound() : TypedResults.Ok(updated);
    }

    private static Dictionary<string, string[]> Validate(ProductUpdateRequest request)
    {
        var errors = new Dictionary<string, string[]>();
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            errors[nameof(request.Name)] = ["Product name is required."];
        }
        if (request.Price < 0)
        {
            errors[nameof(request.Price)] = ["Price cannot be negative."];
        }
        if (request.Stock < 0)
        {
            errors[nameof(request.Stock)] = ["Stock cannot be negative."];
        }
        return errors;
    }
}
