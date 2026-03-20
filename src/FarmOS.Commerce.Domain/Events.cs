using FarmOS.SharedKernel;

namespace FarmOS.Commerce.Domain.Events;

// CSA Season
public record CSASeasonCreated(CSASeasonId Id, int Year, string Name, DateOnly StartDate, DateOnly EndDate, IReadOnlyList<ShareDefinition> Shares, DateTimeOffset OccurredAt) : IDomainEvent;
public record CSAPickupScheduled(CSASeasonId SeasonId, CSAPickup Pickup, DateTimeOffset OccurredAt) : IDomainEvent;
public record CSASeasonClosed(CSASeasonId Id, DateTimeOffset OccurredAt) : IDomainEvent;

// CSA Member
public record CSAMemberRegistered(CSAMemberId Id, CSASeasonId SeasonId, ContactInfo Contact, ShareSize ShareType, DeliveryMethod Method, DateTimeOffset OccurredAt) : IDomainEvent;
public record CSAPaymentRecorded(CSAMemberId Id, decimal Amount, string PaymentMethod, string? Reference, DateTimeOffset OccurredAt) : IDomainEvent;
public record CSASharePickedUp(CSAMemberId Id, DateOnly PickupDate, DateTimeOffset OccurredAt) : IDomainEvent;

// Orders (one-off farm stand, market, restaurant)
public record OrderCreated(OrderId Id, string CustomerName, IReadOnlyList<OrderItem> Items, DeliveryMethod Method, DateTimeOffset OccurredAt) : IDomainEvent;
public record OrderPacked(OrderId Id, DateTimeOffset OccurredAt) : IDomainEvent;
public record OrderFulfilled(OrderId Id, DateTimeOffset OccurredAt) : IDomainEvent;
public record OrderCancelled(OrderId Id, string Reason, DateTimeOffset OccurredAt) : IDomainEvent;

// ─── Customer CRM ───────────────────────────────────────────────────

public record CustomerCreated(CustomerId Id, CustomerProfile Profile, AccountTier Tier, DateTimeOffset OccurredAt) : IDomainEvent;
public record CustomerProfileUpdated(CustomerId Id, CustomerProfile Profile, DateTimeOffset OccurredAt) : IDomainEvent;
public record CustomerNoteAdded(CustomerId Id, CustomerNote Note, DateTimeOffset OccurredAt) : IDomainEvent;
public record DuplicateSuspected(CustomerId Id, MatchCandidate Candidate, DateTimeOffset OccurredAt) : IDomainEvent;
public record CustomersMerged(CustomerId SurvivingId, CustomerId AbsorbedId, DateTimeOffset OccurredAt) : IDomainEvent;
public record DuplicateDismissed(CustomerId Id, CustomerId DismissedMatchId, DateTimeOffset OccurredAt) : IDomainEvent;

// ─── Buying Club ────────────────────────────────────────────────────

public record BuyingClubCreated(BuyingClubId Id, string Name, string? Description, OrderCycleFrequency Frequency, DateTimeOffset OccurredAt) : IDomainEvent;
public record DropSiteAdded(BuyingClubId Id, DropSite Site, DateTimeOffset OccurredAt) : IDomainEvent;
public record DropSiteRemoved(BuyingClubId Id, string SiteName, DateTimeOffset OccurredAt) : IDomainEvent;
public record OrderCycleOpened(BuyingClubId Id, DateOnly CycleDate, DateTimeOffset OccurredAt) : IDomainEvent;
public record OrderCycleClosed(BuyingClubId Id, DateOnly CycleDate, DateTimeOffset OccurredAt) : IDomainEvent;
public record BuyingClubPaused(BuyingClubId Id, string? Reason, DateTimeOffset OccurredAt) : IDomainEvent;
public record BuyingClubClosed(BuyingClubId Id, string? Reason, DateTimeOffset OccurredAt) : IDomainEvent;

// ─── Wholesale ──────────────────────────────────────────────────────

public record WholesaleAccountOpened(WholesaleAccountId Id, string BusinessName, string ContactName, string Email, string? Phone, OrderCycleFrequency OrderFrequency, DateTimeOffset OccurredAt) : IDomainEvent;
public record StandingOrderSet(WholesaleAccountId Id, StandingOrder Order, DateTimeOffset OccurredAt) : IDomainEvent;
public record StandingOrderCancelled(WholesaleAccountId Id, string ProductName, DateTimeOffset OccurredAt) : IDomainEvent;
public record DeliveryRouteAssigned(WholesaleAccountId Id, DeliveryRoute Route, DateTimeOffset OccurredAt) : IDomainEvent;
public record WholesaleAccountClosed(WholesaleAccountId Id, string? Reason, DateTimeOffset OccurredAt) : IDomainEvent;
