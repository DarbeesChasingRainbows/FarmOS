using FarmOS.Campus.Domain.Events;
using FarmOS.SharedKernel;

namespace FarmOS.Campus.Domain.Aggregates;

public sealed class Booking : AggregateRoot<BookingId>
{
    public EventId EventId { get; private set; } = default!;
    public AttendeeInfo Attendee { get; private set; } = default!;
    public BookingStatus Status { get; private set; }
    public WaiverInfo? Waiver { get; private set; }

    public static Booking Create(EventId eventId, AttendeeInfo attendee)
    {
        var booking = new Booking();
        booking.RaiseEvent(new BookingCreated(BookingId.New(), eventId, attendee, DateTimeOffset.UtcNow));
        return booking;
    }

    public Result<BookingId, DomainError> Confirm()
    {
        if (Status != BookingStatus.Reserved)
            return DomainError.Conflict("Only reserved bookings can be confirmed.");
        RaiseEvent(new BookingConfirmed(Id, DateTimeOffset.UtcNow));
        return Id;
    }

    public Result<BookingId, DomainError> CheckIn()
    {
        if (Status != BookingStatus.Confirmed)
            return DomainError.Conflict("Only confirmed bookings can be checked in.");
        RaiseEvent(new BookingCheckedIn(Id, DateTimeOffset.UtcNow));
        return Id;
    }

    public Result<BookingId, DomainError> Cancel(string reason)
    {
        if (Status == BookingStatus.CheckedIn)
            return DomainError.Conflict("Cannot cancel a checked-in booking.");
        RaiseEvent(new BookingCancelled(Id, reason, DateTimeOffset.UtcNow));
        return Id;
    }

    public void SignWaiver(WaiverInfo waiver) =>
        RaiseEvent(new WaiverSigned(Id, waiver, DateTimeOffset.UtcNow));

    protected override void Apply(IDomainEvent @event)
    {
        switch (@event)
        {
            case BookingCreated e: Id = e.Id; EventId = e.EventId; Attendee = e.Attendee; Status = BookingStatus.Reserved; break;
            case BookingConfirmed: Status = BookingStatus.Confirmed; break;
            case BookingCheckedIn: Status = BookingStatus.CheckedIn; break;
            case BookingCancelled: Status = BookingStatus.Cancelled; break;
            case WaiverSigned e: Waiver = e.Waiver; break;
        }
    }
}
