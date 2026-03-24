using System.ComponentModel;
using System.Text.Json;
using FarmOS.Commerce.Domain;
using FarmOS.Commerce.Infrastructure.Projectors;
using ModelContextProtocol.Server;

namespace FarmOS.Marketplace.API.Tools;

/// <summary>
/// MCP tools for AI agents to query farm product inventory.
/// These tools are discovered automatically by agents via the MCP ListTools protocol.
/// </summary>
[McpServerToolType]
public class InventoryTools(InventoryQueryService inventory)
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    [McpServerTool, Description("List all available farm products with current inventory levels and prices. Optionally filter by category (CutFlowers, Bouquets, BakedGoods, Kombucha, Mushrooms, FreezeDried, Honey, Eggs, Meat).")]
    public async Task<string> ListProducts(
        [Description("Optional category filter")] string? category = null)
    {
        var products = category is not null
            ? await inventory.GetByCategoryAsync(category)
            : await inventory.GetAvailableProductsAsync();

        return JsonSerializer.Serialize(new
        {
            count = products.Count,
            products = products.Select(p => new
            {
                id = p.Id.Value,
                name = p.Name,
                category = p.Category.ToString(),
                price = p.Price,
                unit = p.Unit.ToString(),
                quantityAvailable = p.QuantityAvailable,
                status = p.Status.ToString(),
                lastUpdated = p.LastUpdated
            })
        }, JsonOpts);
    }

    [McpServerTool, Description("Get detailed information about a specific product including real-time availability. Use the product ID from ListProducts.")]
    public async Task<string> GetProduct(
        [Description("The product ID, e.g. 'flora:dahlia-cafe-au-lait' or 'hearth:sourdough-bread'")] string productId)
    {
        var product = await inventory.GetProductAsync(productId);
        if (product is null)
            return JsonSerializer.Serialize(new { error = "Product not found", productId }, JsonOpts);

        return JsonSerializer.Serialize(new
        {
            id = product.Id.Value,
            name = product.Name,
            category = product.Category.ToString(),
            price = product.Price,
            unit = product.Unit.ToString(),
            quantityAvailable = product.QuantityAvailable,
            status = product.Status.ToString(),
            source = product.Source,
            lastUpdated = product.LastUpdated
        }, JsonOpts);
    }

    [McpServerTool, Description("Search farm products by name. Returns matching products with availability.")]
    public async Task<string> SearchProducts(
        [Description("Search term to match against product names")] string query)
    {
        var products = await inventory.SearchProductsAsync(query);

        return JsonSerializer.Serialize(new
        {
            query,
            count = products.Count,
            products = products.Select(p => new
            {
                id = p.Id.Value,
                name = p.Name,
                category = p.Category.ToString(),
                price = p.Price,
                unit = p.Unit.ToString(),
                quantityAvailable = p.QuantityAvailable,
                status = p.Status.ToString()
            })
        }, JsonOpts);
    }

    [McpServerTool, Description("Check if specific products are available in the requested quantities. Use before placing an order.")]
    public async Task<string> CheckAvailability(
        [Description("The product ID to check")] string productId,
        [Description("The quantity needed")] int quantity)
    {
        var product = await inventory.GetProductAsync(productId);
        if (product is null)
            return JsonSerializer.Serialize(new { available = false, reason = "Product not found" }, JsonOpts);

        var available = product.QuantityAvailable >= quantity;
        return JsonSerializer.Serialize(new
        {
            productId,
            productName = product.Name,
            requested = quantity,
            available,
            quantityInStock = product.QuantityAvailable,
            status = product.Status.ToString(),
            message = available
                ? $"{quantity} {product.Unit} of {product.Name} are available."
                : $"Only {product.QuantityAvailable} {product.Unit} available (requested {quantity})."
        }, JsonOpts);
    }
}
