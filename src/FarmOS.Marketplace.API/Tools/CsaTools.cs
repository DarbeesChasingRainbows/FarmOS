using System.ComponentModel;
using System.Text.Json;
using FarmOS.Commerce.Application.Commands;
using FarmOS.Commerce.Domain;
using FarmOS.Commerce.Infrastructure.Projectors;
using MediatR;
using ModelContextProtocol.Server;

namespace FarmOS.Marketplace.API.Tools;

/// <summary>
/// MCP tools for AI agents to help CSA members browse and select
/// their weekly share items in a la carte / hybrid seasons.
/// </summary>
[McpServerToolType]
public class CsaTools(InventoryQueryService inventory, IMediator mediator)
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    [McpServerTool, Description(
        "Browse available items for a la carte CSA share selection. " +
        "Shows products with prices so a member can build their box within their share allowance. " +
        "Full share = $40, Half share = $25.")]
    public async Task<string> BrowseCsaItems(
        [Description("Optional category filter (CutFlowers, Bouquets, BakedGoods, Kombucha, Mushrooms, FreezeDried, Honey, Eggs)")] string? category = null)
    {
        var products = category is not null
            ? await inventory.GetByCategoryAsync(category)
            : await inventory.GetAvailableProductsAsync();

        return JsonSerializer.Serialize(new
        {
            info = "Select items up to your share allowance. Full share = $40.00, Half share = $25.00.",
            count = products.Count,
            items = products.Select(p => new
            {
                productId = p.Id.Value,
                name = p.Name,
                category = p.Category.ToString(),
                pricePerUnit = p.Price,
                unit = p.Unit.ToString(),
                available = p.QuantityAvailable,
                status = p.Status.ToString()
            })
        }, JsonOpts);
    }

    [McpServerTool, Description(
        "Select items for a CSA member's a la carte share pickup. " +
        "Validates that the total value doesn't exceed the share allowance. " +
        "Items is a JSON array of {productId, productName, quantity, unitPrice}.")]
    public async Task<string> SelectCsaItems(
        [Description("The CSA member ID (GUID)")] string memberId,
        [Description("The pickup date in YYYY-MM-DD format")] string pickupDate,
        [Description("JSON array of items: [{productId, productName, quantity, unitPrice}]")] string items,
        [Description("Share allowance in dollars (40.00 for full, 25.00 for half)")] decimal shareAllowance)
    {
        if (!Guid.TryParse(memberId, out var memberGuid))
            return JsonSerializer.Serialize(new { error = "Invalid member ID format." }, JsonOpts);

        if (!DateOnly.TryParse(pickupDate, out var date))
            return JsonSerializer.Serialize(new { error = "Invalid date format. Use YYYY-MM-DD." }, JsonOpts);

        List<ItemRequest>? parsed;
        try
        {
            parsed = JsonSerializer.Deserialize<List<ItemRequest>>(items, JsonOpts);
        }
        catch
        {
            return JsonSerializer.Serialize(new { error = "Invalid items JSON. Expected [{productId, productName, quantity, unitPrice}]." }, JsonOpts);
        }

        if (parsed is null || parsed.Count == 0)
            return JsonSerializer.Serialize(new { error = "No items provided." }, JsonOpts);

        // Validate availability for each item
        foreach (var item in parsed)
        {
            var product = await inventory.GetProductAsync(item.ProductId);
            if (product is null)
                return JsonSerializer.Serialize(new { error = $"Product '{item.ProductId}' not found." }, JsonOpts);
            if (product.QuantityAvailable < item.Quantity)
                return JsonSerializer.Serialize(new
                {
                    error = $"Insufficient stock for {product.Name}. Available: {product.QuantityAvailable}, requested: {item.Quantity}."
                }, JsonOpts);
        }

        var selections = parsed.Select(i =>
            new CSAItemSelection(i.ProductId, i.ProductName, i.Quantity, i.UnitPrice)).ToList();

        var totalValue = selections.Sum(s => s.Subtotal);

        var cmd = new SelectItemsCommand(memberGuid, date, selections, shareAllowance);
        var result = await mediator.Send(cmd);

        return result.Match(
            id => JsonSerializer.Serialize(new
            {
                success = true,
                memberId = id,
                pickupDate = date.ToString("yyyy-MM-dd"),
                itemCount = selections.Count,
                totalValue,
                remainingAllowance = shareAllowance - totalValue,
                message = $"Selected {selections.Count} items totaling ${totalValue:F2}. " +
                          $"${shareAllowance - totalValue:F2} remaining of ${shareAllowance:F2} allowance."
            }, JsonOpts),
            err => JsonSerializer.Serialize(new { error = err.Message }, JsonOpts)
        );
    }

    private record ItemRequest(string ProductId, string ProductName, int Quantity, decimal UnitPrice);
}
