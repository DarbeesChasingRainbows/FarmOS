using FarmOS.SharedKernel;

namespace FarmOS.Campus.Domain.Events;

// ─── FarmEvent ──────────────────────────────────────────────────────
public record FarmEventCreated(EventId Id, EventType Type, string Title, string? Description, EventSchedule Schedule, DateTimeOffset OccurredAt) : IDomainEvent;
public record FarmEventPublished(EventId Id, DateTimeOffset OccurredAt) : IDomainEvent;
public record FarmEventCancelled(EventId Id, string Reason, DateTimeOffset OccurredAt) : IDomainEvent;
public record FarmEventCompleted(EventId Id, int TotalAttendees, decimal TotalRevenue, DateTimeOffset OccurredAt) : IDomainEvent;
public record SpotReserved(EventId Id, int PartySize, int NewBookedCount, DateTimeOffset OccurredAt) : IDomainEvent;

// ─── Booking ────────────────────────────────────────────────────────
public record BookingCreated(BookingId Id, EventId EventId, AttendeeInfo Attendee, DateTimeOffset OccurredAt) : IDomainEvent;
public record BookingConfirmed(BookingId Id, DateTimeOffset OccurredAt) : IDomainEvent;
public record BookingCheckedIn(BookingId Id, DateTimeOffset OccurredAt) : IDomainEvent;
public record BookingCancelled(BookingId Id, string Reason, DateTimeOffset OccurredAt) : IDomainEvent;
public record WaiverSigned(BookingId Id, WaiverInfo Waiver, DateTimeOffset OccurredAt) : IDomainEvent;
