using FarmOS.SharedKernel;

namespace FarmOS.Ledger.Domain;

public record ExpenseId(Guid Value) { public static ExpenseId New() => new(Guid.NewGuid()); public override string ToString() => Value.ToString(); }
public record RevenueId(Guid Value) { public static RevenueId New() => new(Guid.NewGuid()); public override string ToString() => Value.ToString(); }

public enum ExpenseCategory { Feed, Seed, Ingredients, Equipment, Fuel, Maintenance, Supplies, Labor, Veterinary, Processing, Marketing, Insurance, Taxes, Utilities, Other, Permits, Certification, GrantMatch, Tour, Wages, Stipend }
public enum RevenueCategory { CSAShares, FarmStand, FarmersMarket, Restaurant, CutFlowers, Honey, Eggs, Meat, BakedGoods, Kombucha, Other, Tours, Workshops, BuyingClub, Wholesale, CafeFood, CafeBeverage, Retail }
public enum LedgerContext { Pasture, Flora, Hearth, Apiary, Commerce, Assets, General, Crew, Campus, Counter, Compliance }

public record LineItem(string Description, Quantity Qty, decimal UnitPrice);

public record EnterpriseCode(LedgerContext Context, string? SubEnterprise);
public record CostAllocationRule(EnterpriseCode From, EnterpriseCode To, decimal Percentage, string Basis);
