using System.ComponentModel;
using System.Text.Json;
using FarmOS.Commerce.Application.Commands;
using FarmOS.Commerce.Domain;
using FarmOS.Commerce.Infrastructure.Projectors;
using MediatR;
using ModelContextProtocol.Server;

namespace FarmOS.Marketplace.API.Tools;

/// <summary>
/// MCP tools for AI agents to place orders on behalf of customers.
/// Validates inventory availability before creating orders.
/// </summary>
[McpServerToolType]
public class OrderTools(IMediator mediator, InventoryQueryService inventory)
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    [McpServerTool, Description("Place an order for farm products. Validates inventory before creating the order. Returns order ID and confirmation details.")]
    public async Task<string> PlaceOrder(
        [Description("Customer's full name")] string customerName,
        [Description("Customer's email address")] string email,
        [Description("JSON array of items: [{\"productId\": \"hearth:sourdough-bread\", \"quantity\": 2}]")] string items,
        [Description("Fulfillment method: 'Pickup' or 'Delivery'")] string fulfillment = "Pickup")
    {
        // Parse items
        List<OrderItemRequest>? orderItems;
        try
        {
            orderItems = JsonSerializer.Deserialize<List<OrderItemRequest>>(items,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new
            {
                success = false,
                error = $"Invalid items format: {ex.Message}",
                expectedFormat = new[] { new { productId = "hearth:sourdough-bread", quantity = 2 } }
            }, JsonOpts);
        }

        if (orderItems is null || orderItems.Count == 0)
            return JsonSerializer.Serialize(new { success = false, error = "No items provided" }, JsonOpts);

        // Validate availability for each item
        var validationErrors = new List<string>();
        var resolvedItems = new List<ResolvedItem>();

        foreach (var item in orderItems)
        {
            var product = await inventory.GetProductAsync(item.ProductId);
            if (product is null)
            {
                validationErrors.Add($"Product '{item.ProductId}' not found.");
                continue;
            }

            if (product.QuantityAvailable < item.Quantity)
            {
                validationErrors.Add(
                    $"Insufficient stock for '{product.Name}': requested {item.Quantity}, available {product.QuantityAvailable}.");
                continue;
            }

            resolvedItems.Add(new(product, item.Quantity));
        }

        if (validationErrors.Count > 0)
        {
            return JsonSerializer.Serialize(new
            {
                success = false,
                errors = validationErrors
            }, JsonOpts);
        }

        // Build Commerce command items
        var commerceItems = resolvedItems.Select(r => new OrderItem(
            r.Product.Name,
            r.Product.Category.ToString(),
            new FarmOS.SharedKernel.Quantity(r.Quantity, r.Product.Unit.ToString(), r.Product.Unit.ToString()),
            r.Product.Price,
            null
        )).ToList();

        var method = fulfillment.Equals("Delivery", StringComparison.OrdinalIgnoreCase)
            ? DeliveryMethod.Delivery
            : DeliveryMethod.Pickup;

        // Place the order via MediatR
        var command = new CreateOrderCommand(customerName, commerceItems, method);
        var result = await mediator.Send(command);

        return result.Match(
            orderId => JsonSerializer.Serialize(new
            {
                success = true,
                orderId = orderId.ToString(),
                customerName,
                email,
                fulfillment = method.ToString(),
                items = resolvedItems.Select(r => new
                {
                    product = r.Product.Name,
                    quantity = r.Quantity,
                    unitPrice = r.Product.Price,
                    subtotal = r.Product.Price * r.Quantity
                }),
                total = resolvedItems.Sum(r => r.Product.Price * r.Quantity),
                message = $"Order placed successfully! {(method == DeliveryMethod.Pickup ? "Please pick up at the farm stand during business hours." : "We'll contact you to arrange delivery.")}"
            }, JsonOpts),
            error => JsonSerializer.Serialize(new
            {
                success = false,
                error = error.Message
            }, JsonOpts)
        );
    }

    [McpServerTool, Description("Get available fulfillment options for ordering from the farm.")]
    public static string GetFulfillmentOptions()
    {
        return JsonSerializer.Serialize(new
        {
            options = new[]
            {
                new
                {
                    id = "farm_pickup",
                    name = "Farm Stand Pickup",
                    description = "Pick up your order at the farm stand. Free.",
                    cost = 0m,
                    schedule = "Wednesday–Saturday, 9am–5pm"
                },
                new
                {
                    id = "local_delivery",
                    name = "Local Delivery",
                    description = "Delivery within 30 miles of the farm. $10 flat rate.",
                    cost = 10m,
                    schedule = "Thursday and Saturday deliveries"
                },
                new
                {
                    id = "farmers_market",
                    name = "Farmers Market Pickup",
                    description = "Pick up at our booth at participating farmers markets.",
                    cost = 0m,
                    schedule = "Saturday markets only — pre-order by Thursday"
                }
            }
        }, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase, WriteIndented = true });
    }

    private record OrderItemRequest(string ProductId, int Quantity);
    private record ResolvedItem(ProductListing Product, int Quantity);
}
