namespace FarmOS.Hearth.Domain.Services;

public static class LotNumberGenerator
{
    public static string GenerateLotNumber(ProductCategory category, DateOnly date, int batchNumberOfDay)
    {
        var prefix = category switch
        {
            ProductCategory.Mushroom => "MUSH",
            ProductCategory.Jun => "JUN",
            ProductCategory.Kombucha => "KOMB",
            ProductCategory.Sourdough => "SOUR",
            ProductCategory.Beef => "BEEF",
            ProductCategory.Wheat => "WHEAT",
            ProductCategory.Ingredients => "INGR",
            _ => "MISC"
        };
        
        return $"{prefix}-{date:yyyyMMdd}-{batchNumberOfDay:D2}";
    }
}
