using System.ComponentModel;
using System.Text.Json;
using FarmOS.Commerce.Application.Commands;
using FarmOS.Commerce.Domain;
using FarmOS.Commerce.Infrastructure.Projectors;
using MediatR;
using ModelContextProtocol.Server;

namespace FarmOS.Marketplace.API.Tools;

/// <summary>
/// MCP tools that implement UCP checkout operations.
/// These map UCP capabilities to MCP tools so AI agents using MCP
/// can also complete UCP-standard checkout flows.
/// </summary>
[McpServerToolType]
public class UcpCheckoutTools(InventoryQueryService inventory, IMediator mediator)
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    [McpServerTool, Description("Create a UCP checkout session for purchasing farm products. Returns a checkout session ID that can be used to update and complete the purchase.")]
    public async Task<string> CreateCheckout(
        [Description("JSON array of items: [{\"id\": \"hearth:sourdough-bread\", \"quantity\": 2}]")] string lineItems)
    {
        List<ItemRequest>? items;
        try
        {
            items = JsonSerializer.Deserialize<List<ItemRequest>>(lineItems,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch
        {
            return JsonSerializer.Serialize(new { error = "Invalid lineItems format" }, JsonOpts);
        }

        if (items is null || items.Count == 0)
            return JsonSerializer.Serialize(new { error = "No items provided" }, JsonOpts);

        var resolvedItems = new List<ResolvedCheckoutItem>();
        foreach (var item in items)
        {
            var product = await inventory.GetProductAsync(item.Id);
            if (product is null)
                return JsonSerializer.Serialize(new { error = $"Product '{item.Id}' not found" }, JsonOpts);
            if (product.QuantityAvailable < item.Quantity)
                return JsonSerializer.Serialize(new
                {
                    error = $"Insufficient stock for '{product.Name}': requested {item.Quantity}, available {product.QuantityAvailable}"
                }, JsonOpts);

            resolvedItems.Add(new(product, item.Quantity));
        }

        var subtotal = resolvedItems.Sum(r => r.Product.Price * r.Quantity);

        return JsonSerializer.Serialize(new
        {
            checkoutId = $"mcp_chk_{Guid.NewGuid():N}",
            status = "incomplete",
            lineItems = resolvedItems.Select(r => new
            {
                id = r.Product.Id.Value,
                title = r.Product.Name,
                priceCents = (int)(r.Product.Price * 100),
                quantity = r.Quantity,
                subtotalCents = (int)(r.Product.Price * r.Quantity * 100)
            }),
            subtotalCents = (int)(subtotal * 100),
            currency = "USD",
            nextStep = "Call UpdateCheckout to add buyer info and fulfillment choice, then CompleteCheckout to finalize."
        }, JsonOpts);
    }

    [McpServerTool, Description("Complete a checkout and place the order. Provide buyer info and items to purchase.")]
    public async Task<string> CompleteCheckout(
        [Description("Customer's full name")] string customerName,
        [Description("Customer's email")] string email,
        [Description("JSON array of items: [{\"id\": \"hearth:sourdough-bread\", \"quantity\": 2}]")] string lineItems,
        [Description("Fulfillment: 'farm_pickup' (free) or 'local_delivery' ($10)")] string fulfillment = "farm_pickup")
    {
        List<ItemRequest>? items;
        try
        {
            items = JsonSerializer.Deserialize<List<ItemRequest>>(lineItems,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new { error = $"Invalid lineItems: {ex.Message}" }, JsonOpts);
        }

        if (items is null || items.Count == 0)
            return JsonSerializer.Serialize(new { error = "No items" }, JsonOpts);

        // Validate + resolve
        var resolvedItems = new List<ResolvedCheckoutItem>();
        foreach (var item in items)
        {
            var product = await inventory.GetProductAsync(item.Id);
            if (product is null)
                return JsonSerializer.Serialize(new { error = $"Product '{item.Id}' not found" }, JsonOpts);
            if (product.QuantityAvailable < item.Quantity)
                return JsonSerializer.Serialize(new
                {
                    error = $"Insufficient stock: '{product.Name}' has {product.QuantityAvailable}, need {item.Quantity}"
                }, JsonOpts);

            resolvedItems.Add(new(product, item.Quantity));
        }

        // Create Commerce Order
        var orderItems = resolvedItems.Select(r => new OrderItem(
            r.Product.Name,
            r.Product.Category.ToString(),
            new FarmOS.SharedKernel.Quantity(r.Quantity, r.Product.Unit.ToString(), r.Product.Unit.ToString()),
            r.Product.Price,
            null
        )).ToList();

        var method = fulfillment.Contains("delivery", StringComparison.OrdinalIgnoreCase)
            ? DeliveryMethod.Delivery
            : DeliveryMethod.Pickup;

        var command = new CreateOrderCommand(customerName, orderItems, method);
        var result = await mediator.Send(command);

        return result.Match(
            orderId =>
            {
                var subtotal = resolvedItems.Sum(r => r.Product.Price * r.Quantity);
                var deliveryCost = method == DeliveryMethod.Delivery ? 10m : 0m;
                var total = subtotal + deliveryCost;

                return JsonSerializer.Serialize(new
                {
                    success = true,
                    orderId = orderId.ToString(),
                    status = "completed",
                    buyer = new { name = customerName, email },
                    lineItems = resolvedItems.Select(r => new
                    {
                        product = r.Product.Name,
                        quantity = r.Quantity,
                        unitPrice = r.Product.Price,
                        subtotal = r.Product.Price * r.Quantity
                    }),
                    fulfillment = new
                    {
                        method = fulfillment,
                        cost = deliveryCost
                    },
                    total,
                    currency = "USD",
                    message = method == DeliveryMethod.Pickup
                        ? "Order confirmed! Pick up at the farm stand Wednesday–Saturday, 9am–5pm."
                        : "Order confirmed! We'll contact you to arrange delivery within 30 miles."
                }, JsonOpts);
            },
            error => JsonSerializer.Serialize(new { success = false, error = error.Message }, JsonOpts)
        );
    }

    private record ItemRequest(string Id, int Quantity);
    private record ResolvedCheckoutItem(ProductListing Product, int Quantity);
}
