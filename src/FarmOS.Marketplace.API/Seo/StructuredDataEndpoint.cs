using FarmOS.Commerce.Infrastructure.Projectors;

namespace FarmOS.Marketplace.API.Seo;

/// <summary>
/// Serves Schema.org JSON-LD structured data for search engine discovery.
/// This enables Google, Bing, and AI shopping assistants to understand
/// the farm's products, location, and availability.
/// </summary>
public static class StructuredDataEndpoint
{
    public static void MapStructuredDataEndpoints(this WebApplication app)
    {
        app.MapGet("/api/marketplace/structured-data", async (InventoryQueryService svc) =>
        {
            var products = await svc.GetAvailableProductsAsync();

            var jsonLd = new
            {
                @context = "https://schema.org",
                @type = "LocalBusiness",
                name = "Darbee's Chasing Rainbows Farm",
                description = "Diversified regenerative farm near Atlanta, Georgia. Cut flowers, artisan sourdough bread, kombucha, gourmet mushrooms, freeze-dried products, pastured eggs, and raw honey.",
                url = "https://darbeeschasingrainbows.com",
                address = new
                {
                    @type = "PostalAddress",
                    addressRegion = "GA",
                    addressCountry = "US"
                },
                openingHoursSpecification = new[]
                {
                    new { @type = "OpeningHoursSpecification", dayOfWeek = new[] { "Wednesday", "Thursday", "Friday", "Saturday" }, opens = "09:00", closes = "17:00" }
                },
                paymentAccepted = new[] { "Cash", "Credit Card", "Debit Card", "EBT/SNAP", "Google Pay" },
                priceRange = "$$",
                additionalType = "https://schema.org/Farm",
                makesOffer = products.Select(p => new
                {
                    @type = "Offer",
                    name = p.Name,
                    price = p.Price,
                    priceCurrency = "USD",
                    availability = p.Status.ToString() switch
                    {
                        "Available" => "https://schema.org/InStock",
                        "Low" => "https://schema.org/LimitedAvailability",
                        _ => "https://schema.org/OutOfStock"
                    },
                    itemOffered = new
                    {
                        @type = "Product",
                        name = p.Name,
                        category = p.Category.ToString(),
                        offers = new
                        {
                            @type = "Offer",
                            price = p.Price,
                            priceCurrency = "USD"
                        }
                    }
                }).ToList()
            };

            return Results.Json(jsonLd, contentType: "application/ld+json");
        });
    }
}
