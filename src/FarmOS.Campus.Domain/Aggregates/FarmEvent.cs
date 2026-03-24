using FarmOS.Campus.Domain.Events;
using FarmOS.SharedKernel;

namespace FarmOS.Campus.Domain.Aggregates;

public sealed class FarmEvent : AggregateRoot<EventId>
{
    public EventType Type { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public EventSchedule Schedule { get; private set; } = default!;
    public EventStatus Status { get; private set; }
    public int BookedCount { get; private set; }

    public static FarmEvent Create(EventType type, string title, string? description, EventSchedule schedule)
    {
        var farmEvent = new FarmEvent();
        farmEvent.RaiseEvent(new FarmEventCreated(EventId.New(), type, title, description, schedule, DateTimeOffset.UtcNow));
        return farmEvent;
    }

    public Result<EventId, DomainError> Publish()
    {
        if (Status == EventStatus.Cancelled)
            return DomainError.Conflict("Cannot publish a cancelled event.");
        RaiseEvent(new FarmEventPublished(Id, DateTimeOffset.UtcNow));
        return Id;
    }

    public Result<EventId, DomainError> Cancel(string reason)
    {
        if (Status == EventStatus.Completed)
            return DomainError.Conflict("Cannot cancel a completed event.");
        RaiseEvent(new FarmEventCancelled(Id, reason, DateTimeOffset.UtcNow));
        return Id;
    }

    public Result<EventId, DomainError> Complete(int totalAttendees, decimal totalRevenue)
    {
        if (Status is not (EventStatus.Published or EventStatus.Full or EventStatus.InProgress))
            return DomainError.Conflict("Only published, full, or in-progress events can be completed.");
        RaiseEvent(new FarmEventCompleted(Id, totalAttendees, totalRevenue, DateTimeOffset.UtcNow));
        return Id;
    }

    public Result<EventId, DomainError> ReserveSpot(int partySize)
    {
        var remaining = Schedule.Capacity - BookedCount;
        if (remaining < partySize)
            return DomainError.BusinessRule($"Not enough capacity. Remaining: {remaining}, requested: {partySize}.");
        var newBookedCount = BookedCount + partySize;
        RaiseEvent(new SpotReserved(Id, partySize, newBookedCount, DateTimeOffset.UtcNow));
        return Id;
    }

    protected override void Apply(IDomainEvent @event)
    {
        switch (@event)
        {
            case FarmEventCreated e:
                Id = e.Id; Type = e.Type; Title = e.Title; Description = e.Description;
                Schedule = e.Schedule; Status = EventStatus.Draft; BookedCount = 0;
                break;
            case FarmEventPublished: Status = EventStatus.Published; break;
            case FarmEventCancelled: Status = EventStatus.Cancelled; break;
            case FarmEventCompleted: Status = EventStatus.Completed; break;
            case SpotReserved e:
                BookedCount = e.NewBookedCount;
                if (BookedCount == Schedule.Capacity) Status = EventStatus.Full;
                break;
        }
    }
}
