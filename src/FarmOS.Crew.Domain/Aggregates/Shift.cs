using FarmOS.Crew.Domain.Events;
using FarmOS.SharedKernel;

namespace FarmOS.Crew.Domain.Aggregates;

public sealed class Shift : AggregateRoot<ShiftId>
{
    public ShiftEntry Entry { get; private set; } = default!;
    public ShiftStatus Status { get; private set; }

    public static Shift Schedule(ShiftEntry entry)
    {
        var shift = new Shift();
        shift.RaiseEvent(new ShiftScheduled(ShiftId.New(), entry, DateTimeOffset.UtcNow));
        return shift;
    }

    public Result<ShiftId, DomainError> Start()
    {
        if (Status != ShiftStatus.Scheduled)
            return DomainError.Conflict("Only scheduled shifts can be started.");
        RaiseEvent(new ShiftStarted(Id, DateTimeOffset.UtcNow));
        return Id;
    }

    public Result<ShiftId, DomainError> Complete(string? notes)
    {
        if (Status != ShiftStatus.InProgress)
            return DomainError.Conflict("Only in-progress shifts can be completed.");
        RaiseEvent(new ShiftCompleted(Id, notes, DateTimeOffset.UtcNow));
        return Id;
    }

    public Result<ShiftId, DomainError> Cancel(string reason)
    {
        if (Status is ShiftStatus.Completed or ShiftStatus.Cancelled)
            return DomainError.Conflict("Cannot cancel a completed or already cancelled shift.");
        RaiseEvent(new ShiftCancelled(Id, reason, DateTimeOffset.UtcNow));
        return Id;
    }

    protected override void Apply(IDomainEvent @event)
    {
        switch (@event)
        {
            case ShiftScheduled e: Id = e.Id; Entry = e.Entry; Status = ShiftStatus.Scheduled; break;
            case ShiftStarted: Status = ShiftStatus.InProgress; break;
            case ShiftCompleted: Status = ShiftStatus.Completed; break;
            case ShiftCancelled: Status = ShiftStatus.Cancelled; break;
        }
    }
}
