using ArangoDBNetStandard;
using ArangoDBNetStandard.CursorApi.Models;
using FarmOS.Commerce.Domain;

namespace FarmOS.Commerce.Infrastructure.Projectors;

/// <summary>
/// Read-only query service over the inventory_products ArangoDB collection.
/// Used by REST endpoints, MCP tools, and UCP catalog to serve real-time inventory.
/// </summary>
public sealed class InventoryQueryService
{
    private readonly IArangoDBClient _arango;

    public InventoryQueryService(IArangoDBClient arango)
    {
        _arango = arango;
    }

    public async Task<IReadOnlyList<ProductListing>> GetAllProductsAsync(CancellationToken ct = default)
    {
        var cursor = await _arango.Cursor.PostCursorAsync<InventoryDoc>(
            new PostCursorBody
            {
                Query = "FOR p IN inventory_products SORT p.category, p.name RETURN p"
            });

        return cursor.Result.Select(MapToListing).ToList();
    }

    public async Task<ProductListing?> GetProductAsync(string productId, CancellationToken ct = default)
    {
        var cursor = await _arango.Cursor.PostCursorAsync<InventoryDoc>(
            new PostCursorBody
            {
                Query = "FOR p IN inventory_products FILTER p._key == @key RETURN p",
                BindVars = new Dictionary<string, object> { ["key"] = productId }
            });

        var doc = cursor.Result.FirstOrDefault();
        return doc is null ? null : MapToListing(doc);
    }

    public async Task<IReadOnlyList<ProductListing>> GetByCategoryAsync(string category, CancellationToken ct = default)
    {
        var cursor = await _arango.Cursor.PostCursorAsync<InventoryDoc>(
            new PostCursorBody
            {
                Query = "FOR p IN inventory_products FILTER p.category == @cat SORT p.name RETURN p",
                BindVars = new Dictionary<string, object> { ["cat"] = category }
            });

        return cursor.Result.Select(MapToListing).ToList();
    }

    public async Task<IReadOnlyList<ProductListing>> GetAvailableProductsAsync(CancellationToken ct = default)
    {
        var cursor = await _arango.Cursor.PostCursorAsync<InventoryDoc>(
            new PostCursorBody
            {
                Query = "FOR p IN inventory_products FILTER p.status != 'OutOfStock' SORT p.category, p.name RETURN p"
            });

        return cursor.Result.Select(MapToListing).ToList();
    }

    public async Task<IReadOnlyList<ProductListing>> SearchProductsAsync(string query, CancellationToken ct = default)
    {
        var cursor = await _arango.Cursor.PostCursorAsync<InventoryDoc>(
            new PostCursorBody
            {
                Query = "FOR p IN inventory_products FILTER CONTAINS(LOWER(p.name), LOWER(@q)) SORT p.name RETURN p",
                BindVars = new Dictionary<string, object> { ["q"] = query }
            });

        return cursor.Result.Select(MapToListing).ToList();
    }

    private static ProductListing MapToListing(InventoryDoc doc) => new(
        Id: new ProductId(doc._key),
        Name: doc.name,
        Category: Enum.TryParse<InventoryCategory>(doc.category, out var cat) ? cat : InventoryCategory.CutFlowers,
        Price: doc.price,
        Unit: Enum.TryParse<ProductUnit>(doc.unit, out var unit) ? unit : ProductUnit.Units,
        QuantityAvailable: doc.quantityAvailable,
        Status: Enum.TryParse<InventoryStatus>(doc.status, out var status) ? status : InventoryStatus.OutOfStock,
        Source: doc.source,
        LastUpdated: doc.lastUpdated
    );

    // ArangoDB document shape — matches what InventoryProjectorWorker writes
    private record InventoryDoc
    {
        public string _key { get; init; } = "";
        public string name { get; init; } = "";
        public string category { get; init; } = "";
        public decimal price { get; init; }
        public string unit { get; init; } = "";
        public int quantityAvailable { get; init; }
        public string status { get; init; } = "";
        public string source { get; init; } = "";
        public DateTimeOffset lastUpdated { get; init; }
    }
}
