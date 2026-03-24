namespace FarmOS.Marketplace.API.Ucp;

/// <summary>
/// UCP (Universal Commerce Protocol) discovery profile.
/// Served at /.well-known/ucp per the UCP v2026-01-11 specification.
/// AI shopping agents discover this endpoint to learn what commerce
/// capabilities the farm supports.
/// </summary>
public static class UcpDiscovery
{
    public static object GetProfile(WebApplication app)
    {
        // Resolve the base URL from configuration or default
        var baseUrl = app.Configuration.GetValue<string>("Marketplace:BaseUrl")
            ?? "https://api.darbeeschasingrainbows.com";

        return new
        {
            ucp = new
            {
                version = "2026-01-11",
                services = new
                {
                    dev_ucp_shopping = new
                    {
                        version = "2026-01-11",
                        spec = "https://ucp.dev/specification/overview",
                        mcp = new
                        {
                            schema = "https://ucp.dev/services/shopping/mcp.openrpc.json",
                            endpoint = $"{baseUrl}/mcp"
                        }
                    }
                },
                capabilities = new object[]
                {
                    new
                    {
                        name = "dev.ucp.shopping.checkout",
                        version = "2026-01-11",
                        spec = "https://ucp.dev/specification/checkout",
                        transports = new object[]
                        {
                            new { name = "rest", endpoint = $"{baseUrl}/ucp/checkout-sessions" },
                            new { name = "mcp", endpoint = $"{baseUrl}/mcp" }
                        }
                    },
                    new
                    {
                        name = "dev.ucp.shopping.catalog",
                        version = "2026-01-11",
                        spec = "https://ucp.dev/specification/catalog",
                        transports = new object[]
                        {
                            new { name = "rest", endpoint = $"{baseUrl}/ucp/catalog/items" }
                        }
                    },
                    new
                    {
                        name = "dev.ucp.shopping.fulfillment",
                        version = "2026-01-11",
                        spec = "https://ucp.dev/specification/fulfillment",
                        transports = new object[]
                        {
                            new { name = "rest", endpoint = $"{baseUrl}/ucp/checkout-sessions" }
                        }
                    }
                }
            },
            payment = new
            {
                handlers = new[]
                {
                    new
                    {
                        id = "com.google.pay",
                        name = "gpay",
                        version = "2024-12-03",
                        spec = "https://developers.google.com/merchant/ucp/guides/gpay-payment-handler",
                        config_schema = "https://pay.google.com/gp/p/ucp/2026-01-11/schemas/gpay_config.json",
                        instrument_schemas = new[]
                        {
                            "https://pay.google.com/gp/p/ucp/2026-01-11/schemas/gpay_card_payment_instrument.json"
                        }
                    }
                }
            },
            merchant = new
            {
                name = "Darbee's Chasing Rainbows Farm",
                description = "Diversified regenerative farm near Atlanta, Georgia — cut flowers, artisan bread, kombucha, mushrooms, pastured eggs & honey",
                location = new
                {
                    region = "Metro Atlanta, GA",
                    country = "US"
                },
                fulfillment_options = new[]
                {
                    new { type = "pickup", name = "Farm Stand Pickup", cost_cents = 0 },
                    new { type = "local_delivery", name = "Local Delivery (30mi)", cost_cents = 1000 },
                    new { type = "market_pickup", name = "Farmers Market Pickup", cost_cents = 0 }
                },
                payment_methods = new[] { "google_pay", "card", "cash", "ebt_snap" }
            }
        };
    }
}
