using FarmOS.Commerce.Infrastructure.Projectors;

namespace FarmOS.Marketplace.API.Ucp;

/// <summary>
/// UCP Catalog capability endpoints.
/// Maps inventory products to UCP catalog format where prices are in
/// smallest currency unit (cents for USD) per the UCP specification.
/// </summary>
public static class UcpCatalogEndpoints
{
    public static void MapUcpCatalogEndpoints(this WebApplication app)
    {
        var catalog = app.MapGroup("/ucp/catalog");

        catalog.MapGet("/items", async (InventoryQueryService svc) =>
        {
            var products = await svc.GetAvailableProductsAsync();

            return Results.Ok(new
            {
                ucp = new { version = "2026-01-11" },
                items = products.Select(p => new
                {
                    id = p.Id.Value,
                    title = p.Name,
                    description = $"{p.Category} — {p.Name}",
                    price = (int)(p.Price * 100), // UCP uses cents
                    currency = "USD",
                    available = p.QuantityAvailable > 0,
                    inventory = new
                    {
                        quantity = p.QuantityAvailable,
                        unit = p.Unit.ToString()
                    },
                    metadata = new
                    {
                        category = p.Category.ToString(),
                        source = p.Source,
                        lastUpdated = p.LastUpdated
                    }
                })
            });
        });

        catalog.MapGet("/items/{id}", async (string id, InventoryQueryService svc) =>
        {
            var product = await svc.GetProductAsync(id);
            if (product is null)
                return Results.NotFound(new { error = "Item not found", id });

            return Results.Ok(new
            {
                ucp = new { version = "2026-01-11" },
                id = product.Id.Value,
                title = product.Name,
                description = $"{product.Category} — {product.Name}",
                price = (int)(product.Price * 100),
                currency = "USD",
                available = product.QuantityAvailable > 0,
                inventory = new
                {
                    quantity = product.QuantityAvailable,
                    unit = product.Unit.ToString()
                },
                metadata = new
                {
                    category = product.Category.ToString(),
                    source = product.Source,
                    lastUpdated = product.LastUpdated
                }
            });
        });
    }
}
