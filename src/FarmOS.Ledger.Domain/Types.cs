using FarmOS.SharedKernel;

namespace FarmOS.Ledger.Domain;

public record ExpenseId(Guid Value) { public static ExpenseId New() => new(Guid.NewGuid()); public override string ToString() => Value.ToString(); }
public record RevenueId(Guid Value) { public static RevenueId New() => new(Guid.NewGuid()); public override string ToString() => Value.ToString(); }

public enum ExpenseCategory { Feed, Seed, Ingredients, Equipment, Fuel, Maintenance, Supplies, Labor, Veterinary, Processing, Marketing, Insurance, Taxes, Utilities, Other }
public enum RevenueCategory { CSAShares, FarmStand, FarmersMarket, Restaurant, CutFlowers, Honey, Eggs, Meat, BakedGoods, Kombucha, Other }
public enum LedgerContext { Pasture, Flora, Hearth, Apiary, Commerce, Assets, General }

public record LineItem(string Description, Quantity Qty, decimal UnitPrice);
