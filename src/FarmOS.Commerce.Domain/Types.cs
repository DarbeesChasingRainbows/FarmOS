using FarmOS.SharedKernel;

namespace FarmOS.Commerce.Domain;

public record CSASeasonId(Guid Value) { public static CSASeasonId New() => new(Guid.NewGuid()); public override string ToString() => Value.ToString(); }
public record CSAMemberId(Guid Value) { public static CSAMemberId New() => new(Guid.NewGuid()); public override string ToString() => Value.ToString(); }
public record OrderId(Guid Value) { public static OrderId New() => new(Guid.NewGuid()); public override string ToString() => Value.ToString(); }

public enum ShareSize { Full, Half, Weekly, BiWeekly }
public enum PaymentStatus { Pending, Paid, PartiallyPaid, Refunded }
public enum DeliveryMethod { Pickup, Delivery }
public enum OrderStatus { Draft, Confirmed, Packed, PickedUp, Delivered, Cancelled }

public record ShareDefinition(ShareSize Size, decimal Price, int TotalWeeks, IReadOnlyList<string> IncludedCategories);
public record CSAPickup(DateOnly Date, string TimeWindow, string Location, int? MaxSlots);
public record OrderItem(string ProductName, string Category, Quantity Qty, decimal UnitPrice, string? Notes);
public record ContactInfo(string Name, string Email, string? Phone, string? PreferredContact);

// ─── Customer CRM ───────────────────────────────────────────────────

public record CustomerId(Guid Value) { public static CustomerId New() => new(Guid.NewGuid()); public override string ToString() => Value.ToString(); }

public enum CustomerChannel { CSA, BuyingClub, FarmStore, FarmersMarket, Wholesale, Online, Tour }
public enum AccountTier { Standard, Premium, Wholesale }

public record CustomerProfile(string Name, string Email, string? Phone, string? Address, IReadOnlyList<CustomerChannel> Channels, string? Notes, string? DietaryPrefs);
public record CustomerNote(string Content, DateTimeOffset CreatedAt);
public record MatchCandidate(CustomerId ExistingId, string ExistingName, string? ExistingEmail, decimal ConfidenceScore, string MatchBasis);

// ─── Buying Clubs ───────────────────────────────────────────────────

public record BuyingClubId(Guid Value) { public static BuyingClubId New() => new(Guid.NewGuid()); public override string ToString() => Value.ToString(); }
public record WholesaleAccountId(Guid Value) { public static WholesaleAccountId New() => new(Guid.NewGuid()); public override string ToString() => Value.ToString(); }

public enum ClubStatus { Active, Paused, Closed }
public enum OrderCycleStatus { Open, Closed }
public enum OrderCycleFrequency { Weekly, BiWeekly, Monthly }

public record DropSite(string Name, string Address, string ContactPerson, string ContactPhone, DayOfWeek DeliveryDay, TimeOnly DeliveryWindow);
public record StandingOrder(string ProductName, int Quantity, decimal UnitPrice, string? Notes);
public record DeliveryRoute(string Name, IReadOnlyList<string> DropSiteIds, decimal EstimatedMiles);

// ─── A La Carte CSA ────────────────────────────────────────────────

public enum CSASelectionMode { FixedBox, ALaCarte, Hybrid }
public enum SelectionWindowStatus { Closed, Open }

/// <summary>
/// A single item selection within an a la carte CSA pickup.
/// ProductId matches the inventory_products collection keys.
/// </summary>
public record CSAItemSelection(string ProductId, string ProductName, int Quantity, decimal UnitPrice)
{
    public decimal Subtotal => Quantity * UnitPrice;
}
