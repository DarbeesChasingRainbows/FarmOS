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
