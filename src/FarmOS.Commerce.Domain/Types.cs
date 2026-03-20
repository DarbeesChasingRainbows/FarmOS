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
