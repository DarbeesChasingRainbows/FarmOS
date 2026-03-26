using System.Collections.Concurrent;
using FarmOS.Commerce.Application.Commands;
using FarmOS.Commerce.Domain;
using FarmOS.Commerce.Infrastructure.Projectors;
using MediatR;

namespace FarmOS.Marketplace.API.Ucp;

/// <summary>
/// UCP Checkout capability — session-based checkout flow per UCP v2026-01-11.
/// Checkout sessions are ephemeral (in-memory) until completed,
/// at which point they create a Commerce Order aggregate in the event store.
/// </summary>
public static class UcpCheckoutEndpoints
{
    // In-memory checkout sessions — no persistence cost for abandoned carts
    private static readonly ConcurrentDictionary<string, CheckoutSession> Sessions = new();

    public static void MapUcpCheckoutEndpoints(this WebApplication app)
    {
        var checkout = app.MapGroup("/ucp/checkout-sessions");

        // Create checkout session
        checkout.MapPost("/", async (CreateCheckoutRequest request, InventoryQueryService inventory) =>
        {
            var sessionId = $"chk_{Guid.NewGuid():N}";

            var session = new CheckoutSession
            {
                Id = sessionId,
                Status = "incomplete",
                CreatedAt = DateTimeOffset.UtcNow
            };

            // If line items are provided, resolve them
            if (request.LineItems is { Count: > 0 })
            {
                foreach (var item in request.LineItems)
                {
                    var product = await inventory.GetProductAsync(item.Item.Id);
                    if (product is not null)
                    {
                        session.LineItems.Add(new CheckoutLineItem
                        {
                            Id = item.Item.Id,
                            Title = product.Name,
                            PriceCents = (int)(product.Price * 100),
                            Quantity = item.Quantity,
                            Category = product.Category.ToString()
                        });
                    }
                }
            }

            Sessions[sessionId] = session;

            return Results.Created($"/ucp/checkout-sessions/{sessionId}", FormatSession(session));
        });

        // Get checkout session
        checkout.MapGet("/{id}", (string id) =>
        {
            return Sessions.TryGetValue(id, out var session)
                ? Results.Ok(FormatSession(session))
                : Results.NotFound(new { error = "Checkout session not found" });
        });

        // Update checkout session
        checkout.MapPut("/{id}", async (string id, UpdateCheckoutRequest request, InventoryQueryService inventory) =>
        {
            if (!Sessions.TryGetValue(id, out var session))
                return Results.NotFound(new { error = "Checkout session not found" });

            if (session.Status is "completed" or "cancelled")
                return Results.BadRequest(new { error = $"Cannot update {session.Status} session" });

            // Update buyer
            if (request.Buyer is not null)
            {
                session.BuyerEmail = request.Buyer.Email;
                session.BuyerFirstName = request.Buyer.FirstName;
                session.BuyerLastName = request.Buyer.LastName;
            }

            // Update line items
            if (request.LineItems is { Count: > 0 })
            {
                session.LineItems.Clear();
                foreach (var item in request.LineItems)
                {
                    var product = await inventory.GetProductAsync(item.Item.Id);
                    if (product is not null)
                    {
                        session.LineItems.Add(new CheckoutLineItem
                        {
                            Id = item.Item.Id,
                            Title = product.Name,
                            PriceCents = (int)(product.Price * 100),
                            Quantity = item.Quantity,
                            Category = product.Category.ToString()
                        });
                    }
                }
            }

            // Update fulfillment
            if (request.Fulfillment is not null)
                session.FulfillmentMethod = request.Fulfillment;

            return Results.Ok(FormatSession(session));
        });

        // Complete checkout → creates Commerce Order
        checkout.MapPost("/{id}/complete", async (string id, IMediator mediator) =>
        {
            if (!Sessions.TryGetValue(id, out var session))
                return Results.NotFound(new { error = "Checkout session not found" });

            if (session.Status == "completed")
                return Results.BadRequest(new { error = "Session already completed" });

            if (session.LineItems.Count == 0)
                return Results.BadRequest(new { error = "Cannot complete checkout with no items" });

            // Create Commerce Order
            var customerName = $"{session.BuyerFirstName} {session.BuyerLastName}".Trim();
            if (string.IsNullOrEmpty(customerName)) customerName = "UCP Customer";

            var orderItems = session.LineItems.Select(li => new OrderItem(
                li.Title,
                li.Category,
                new FarmOS.SharedKernel.Quantity(li.Quantity, "units", "units"),
                li.PriceCents / 100m,
                null
            )).ToList();

            var method = session.FulfillmentMethod?.Contains("delivery", StringComparison.OrdinalIgnoreCase) == true
                ? DeliveryMethod.Delivery
                : DeliveryMethod.Pickup;

            var command = new CreateOrderCommand(customerName, orderItems, method);
            var result = await mediator.Send(command);

            return result.Match(
                orderId =>
                {
                    session.Status = "completed";
                    session.OrderId = orderId.ToString();

                    return Results.Ok(FormatSession(session));
                },
                error => Results.BadRequest(new { error = error.Message })
            );
        });

        // Cancel checkout
        checkout.MapPost("/{id}/cancel", (string id) =>
        {
            if (!Sessions.TryGetValue(id, out var session))
                return Results.NotFound(new { error = "Checkout session not found" });

            if (session.Status == "completed")
                return Results.BadRequest(new { error = "Cannot cancel completed session" });

            session.Status = "cancelled";
            return Results.Ok(FormatSession(session));
        });
    }

    private static object FormatSession(CheckoutSession session)
    {
        var subtotal = session.LineItems.Sum(li => li.PriceCents * li.Quantity);
        var fulfillmentCost = session.FulfillmentMethod?.Contains("delivery", StringComparison.OrdinalIgnoreCase) == true ? 1000 : 0;
        var total = subtotal + fulfillmentCost;

        return new
        {
            ucp = new
            {
                version = "2026-01-11",
                capabilities = new[]
                {
                    new { name = "dev.ucp.shopping.checkout", version = "2026-01-11" },
                    new { name = "dev.ucp.shopping.fulfillment", version = "2026-01-11" }
                }
            },
            id = session.Id,
            status = session.Status,
            buyer = new
            {
                email = session.BuyerEmail,
                first_name = session.BuyerFirstName,
                last_name = session.BuyerLastName
            },
            line_items = session.LineItems.Select(li => new
            {
                id = li.Id,
                item = new { id = li.Id, title = li.Title, price = li.PriceCents },
                quantity = li.Quantity,
                totals = new[]
                {
                    new { type = "subtotal", amount = li.PriceCents * li.Quantity },
                    new { type = "total", amount = li.PriceCents * li.Quantity }
                }
            }),
            currency = "USD",
            totals = new[]
            {
                new { type = "subtotal", amount = subtotal, display_text = "" },
                new { type = "fulfillment", amount = fulfillmentCost, display_text = fulfillmentCost > 0 ? "Local Delivery" : "Farm Pickup (Free)" },
                new { type = "total", amount = total, display_text = "" }
            },
            order = session.OrderId is not null ? new { id = session.OrderId } : null,
            payment = new
            {
                handlers = new[]
                {
                    new
                    {
                        id = "com.google.pay",
                        name = "gpay",
                        version = "2024-12-03",
                        spec = "https://developers.google.com/merchant/ucp/guides/gpay-payment-handler"
                    }
                }
            }
        };
    }

    // ─── Session State ─────────────────────────────────────────────────

    private class CheckoutSession
    {
        public string Id { get; init; } = "";
        public string Status { get; set; } = "incomplete";
        public string? BuyerEmail { get; set; }
        public string? BuyerFirstName { get; set; }
        public string? BuyerLastName { get; set; }
        public List<CheckoutLineItem> LineItems { get; } = [];
        public string? FulfillmentMethod { get; set; }
        public string? OrderId { get; set; }
        public DateTimeOffset CreatedAt { get; init; }
    }

    private class CheckoutLineItem
    {
        public string Id { get; init; } = "";
        public string Title { get; init; } = "";
        public int PriceCents { get; init; }
        public int Quantity { get; init; }
        public string Category { get; init; } = "";
    }

    // ─── Request DTOs ──────────────────────────────────────────────────

    public record CreateCheckoutRequest(List<LineItemRequest>? LineItems);
    public record UpdateCheckoutRequest(BuyerRequest? Buyer, List<LineItemRequest>? LineItems, string? Fulfillment);
    public record LineItemRequest(ItemRef Item, int Quantity);
    public record ItemRef(string Id);
    public record BuyerRequest(string? Email, string? FirstName, string? LastName);
}
