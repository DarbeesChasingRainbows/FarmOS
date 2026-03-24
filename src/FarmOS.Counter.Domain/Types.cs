using FarmOS.SharedKernel;

namespace FarmOS.Counter.Domain;

// ─── Typed IDs ──────────────────────────────────────────────────────
public record RegisterId(Guid Value) { public static RegisterId New() => new(Guid.NewGuid()); public override string ToString() => Value.ToString(); }
public record SaleId(Guid Value) { public static SaleId New() => new(Guid.NewGuid()); public override string ToString() => Value.ToString(); }
public record CashDrawerId(Guid Value) { public static CashDrawerId New() => new(Guid.NewGuid()); public override string ToString() => Value.ToString(); }

// ─── Enums ──────────────────────────────────────────────────────────
public enum RegisterLocation { FarmStore, Cafe, FarmersMarket, PopUp }
public enum RegisterStatus { Open, Closed }
public enum PaymentMethod { Cash, Card, Check, EBT, Comped }
public enum TaxCategory { NonTaxable, StandardFood, PreparedFood, NonFood }
public enum SaleStatus { Completed, Voided, Refunded }

// ─── Value Objects ──────────────────────────────────────────────────
public record SaleLineItem(string ProductName, string? SKU, int Quantity, decimal UnitPrice, TaxCategory TaxCat, string? Notes);
public record PaymentRecord(PaymentMethod Method, decimal Amount, string? Reference);
public record DrawerCount(decimal Expected, decimal Actual, string? Notes);
