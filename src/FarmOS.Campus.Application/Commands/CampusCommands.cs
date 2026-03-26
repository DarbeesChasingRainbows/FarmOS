using FarmOS.Campus.Domain;
using FarmOS.SharedKernel;
using FarmOS.SharedKernel.CQRS;

namespace FarmOS.Campus.Application.Commands;

// --- FarmEvent ----------------------------------------------------------------

public record CreateFarmEventCommand(EventType Type, string Title, string? Description, EventSchedule Schedule) : ICommand<Guid>;
public record PublishFarmEventCommand(Guid EventId) : ICommand<Guid>;
public record CancelFarmEventCommand(Guid EventId, string Reason) : ICommand<Guid>;
public record CompleteFarmEventCommand(Guid EventId, int TotalAttendees, decimal TotalRevenue) : ICommand<Guid>;

// --- Booking ------------------------------------------------------------------

public record CreateBookingCommand(Guid EventId, AttendeeInfo Attendee) : ICommand<Guid>;
public record ConfirmBookingCommand(Guid BookingId) : ICommand<Guid>;
public record CheckInBookingCommand(Guid BookingId) : ICommand<Guid>;
public record CancelBookingCommand(Guid BookingId, string Reason) : ICommand<Guid>;
public record SignWaiverCommand(Guid BookingId, WaiverInfo Waiver) : ICommand<Guid>;
