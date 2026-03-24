using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol.Server;

namespace FarmOS.Marketplace.API.Resources;

/// <summary>
/// MCP resources providing static farm information to AI agents.
/// Resources are read-only data that agents can access without tool calls.
/// </summary>
[McpServerResourceType]
public class FarmResources
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    [McpServerResource(UriTemplate = "farm://info", Name = "Farm Information", MimeType = "application/json")]
    [Description("General information about the farm — location, hours, what we grow, and how to visit")]
    public static string GetFarmInfo() => JsonSerializer.Serialize(new
    {
        name = "Darbee's Chasing Rainbows Farm",
        tagline = "Diversified regenerative farm near Atlanta, Georgia",
        location = new
        {
            region = "Metro Atlanta, Georgia",
            state = "GA",
            country = "US",
            directions = "Contact us for exact address and directions"
        },
        hours = new
        {
            farmStand = "Wednesday–Saturday, 9am–5pm",
            farmTours = "By appointment — book via our Campus events",
            farmersMarkets = "Saturday mornings at participating Atlanta-area markets"
        },
        whatWeGrow = new
        {
            flora = "Cut flowers — dahlias, sunflowers, ranunculus, roses, seasonal varieties",
            hearth = "Sourdough bread, kombucha, gourmet mushrooms, freeze-dried products",
            pasture = "Pastured poultry (eggs), honey from our apiaries",
            values = "Regenerative practices, Polyface-inspired management, soil health focus"
        },
        certifications = new[]
        {
            "Georgia Grown member",
            "Accepts EBT/SNAP at farm stand"
        },
        contact = new
        {
            website = "https://darbeeschasingrainbows.com",
            email = "hello@darbeeschasingrainbows.com"
        }
    }, JsonOpts);

    [McpServerResource(UriTemplate = "farm://categories", Name = "Product Categories", MimeType = "application/json")]
    [Description("All product categories available at the farm with descriptions")]
    public static string GetCategories() => JsonSerializer.Serialize(new
    {
        categories = new[]
        {
            new { id = "CutFlowers", name = "Cut Flowers", description = "Fresh-cut flowers from our flower beds — dahlias, sunflowers, ranunculus, and seasonal varieties", unit = "Stems", seasonal = true },
            new { id = "Bouquets", name = "Fresh Bouquets", description = "Hand-arranged bouquets made from our farm-grown flowers", unit = "Bunches", seasonal = true },
            new { id = "BakedGoods", name = "Sourdough Bread", description = "Artisan sourdough bread made with our house starters", unit = "Loaves", seasonal = false },
            new { id = "Kombucha", name = "Kombucha", description = "Small-batch kombucha brewed with organic tea and our SCOBYs", unit = "Bottles", seasonal = false },
            new { id = "Mushrooms", name = "Gourmet Mushrooms", description = "Fresh specialty mushrooms grown in our climate-controlled grow rooms", unit = "Pounds", seasonal = false },
            new { id = "FreezeDried", name = "Freeze-Dried Products", description = "Shelf-stable freeze-dried fruits, vegetables, and treats", unit = "Units", seasonal = false },
            new { id = "Honey", name = "Raw Honey", description = "Unfiltered raw honey from our on-farm apiaries", unit = "Units", seasonal = true },
            new { id = "Eggs", name = "Pastured Eggs", description = "Eggs from our free-range, pasture-raised hens", unit = "Dozens", seasonal = false },
            new { id = "Meat", name = "Pastured Meat", description = "Ethically raised, pasture-rotated poultry", unit = "Pounds", seasonal = true }
        }
    }, JsonOpts);

    [McpServerResource(UriTemplate = "farm://policies", Name = "Farm Policies", MimeType = "application/json")]
    [Description("Ordering, pickup, delivery, payment, and return policies")]
    public static string GetPolicies() => JsonSerializer.Serialize(new
    {
        ordering = new
        {
            minimumOrder = "No minimum for farm stand purchases",
            preOrders = "Pre-orders available for farmers market pickup (order by Thursday for Saturday)",
            csaShares = "CSA shares available seasonally — full and half shares with a la carte options"
        },
        fulfillment = new
        {
            farmPickup = "Free pickup at the farm stand during business hours",
            localDelivery = "$10 flat rate within 30 miles — Thursday and Saturday deliveries",
            farmersMarket = "Free pickup at our Saturday market booths",
            buyingClubs = "Group orders delivered to neighborhood drop sites"
        },
        payment = new
        {
            accepted = new[] { "Cash", "Credit/Debit Card", "EBT/SNAP", "Google Pay" },
            ebtNote = "EBT/SNAP accepted for eligible food items (produce, eggs, bread, honey)"
        },
        returns = new
        {
            freshProducts = "No returns on perishable items — contact us within 24 hours if there's a quality issue",
            csaShares = "CSA share purchases are non-refundable but may be transferred"
        }
    }, JsonOpts);
}
