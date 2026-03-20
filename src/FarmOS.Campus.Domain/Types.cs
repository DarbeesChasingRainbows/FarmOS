using FarmOS.SharedKernel;

namespace FarmOS.Campus.Domain;

// ─── Typed IDs ──────────────────────────────────────────────────────
public record EventId(Guid Value) { public static EventId New() => new(Guid.NewGuid()); public override string ToString() => Value.ToString(); }
public record BookingId(Guid Value) { public static BookingId New() => new(Guid.NewGuid()); public override string ToString() => Value.ToString(); }

// ─── Enums ──────────────────────────────────────────────────────────
public enum EventType { FarmTour, Workshop, FieldDay, ClassroomSession, PrivateTour, FarmDinner }
public enum EventStatus { Draft, Published, Full, InProgress, Completed, Cancelled }
public enum BookingStatus { Reserved, Confirmed, CheckedIn, NoShow, Cancelled }

// ─── Value Objects ──────────────────────────────────────────────────
public record EventSchedule(DateOnly Date, TimeOnly Start, TimeOnly End, string Location, int Capacity, decimal PricePerPerson, decimal? GroupRate);
public record AttendeeInfo(string Name, string Email, string? Phone, int PartySize, string? DietaryNotes);
public record WaiverInfo(string SignedBy, DateTimeOffset SignedAt, string? DocumentPath);
