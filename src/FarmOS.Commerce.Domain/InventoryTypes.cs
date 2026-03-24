namespace FarmOS.Commerce.Domain;

// ─── Inventory Read Model Types ─────────────────────────────────────
// These are NOT aggregates — they represent the projected read model
// for real-time inventory queried by MCP/UCP/REST consumers.

public record ProductId(string Value)
{
    public override string ToString() => Value;
}

public enum InventoryCategory
{
    CutFlowers,
    Bouquets,
    BakedGoods,
    Kombucha,
    Mushrooms,
    FreezeDried,
    Honey,
    Eggs,
    Meat
}

public enum ProductUnit
{
    Stems,
    Bunches,
    Loaves,
    Bottles,
    Pounds,
    Dozens,
    Units
}

public enum InventoryStatus
{
    Available,
    Low,
    OutOfStock
}

/// <summary>
/// Projected read model for a single product's current inventory state.
/// Built by InventoryProjectorWorker from cross-context domain events.
/// </summary>
public record ProductListing(
    ProductId Id,
    string Name,
    InventoryCategory Category,
    decimal Price,
    ProductUnit Unit,
    int QuantityAvailable,
    InventoryStatus Status,
    string Source,
    DateTimeOffset LastUpdated)
{
    /// <summary>
    /// Derives status from quantity thresholds.
    /// </summary>
    public static InventoryStatus DeriveStatus(int quantity) => quantity switch
    {
        0 => InventoryStatus.OutOfStock,
        <= 10 => InventoryStatus.Low,
        _ => InventoryStatus.Available
    };
}
