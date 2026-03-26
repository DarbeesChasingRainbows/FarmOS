using FarmOS.Commerce.Infrastructure.Projectors;

namespace FarmOS.Commerce.API;

public static class InventoryEndpoints
{
    public static void MapInventoryEndpoints(this WebApplication app)
    {
        var inventory = app.MapGroup("/api/commerce/inventory");

        inventory.MapGet("/", async (InventoryQueryService svc) =>
        {
            var products = await svc.GetAllProductsAsync();
            return Results.Ok(products);
        });

        inventory.MapGet("/available", async (InventoryQueryService svc) =>
        {
            var products = await svc.GetAvailableProductsAsync();
            return Results.Ok(products);
        });

        inventory.MapGet("/category/{category}", async (string category, InventoryQueryService svc) =>
        {
            var products = await svc.GetByCategoryAsync(category);
            return Results.Ok(products);
        });

        inventory.MapGet("/search", async (string q, InventoryQueryService svc) =>
        {
            var products = await svc.SearchProductsAsync(q);
            return Results.Ok(products);
        });

        inventory.MapGet("/{productId}", async (string productId, InventoryQueryService svc) =>
        {
            var product = await svc.GetProductAsync(productId);
            return product is not null ? Results.Ok(product) : Results.NotFound();
        });
    }
}
