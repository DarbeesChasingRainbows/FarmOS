using FarmOS.Campus.Domain;
using FarmOS.Campus.Domain.Aggregates;
using FarmOS.SharedKernel;
using FarmOS.SharedKernel.CQRS;

namespace FarmOS.Campus.Application.Commands.Handlers;

public sealed class FarmEventCommandHandlers(ICampusEventStore store) :
    ICommandHandler<CreateFarmEventCommand, Guid>,
    ICommandHandler<PublishFarmEventCommand, Guid>,
    ICommandHandler<CancelFarmEventCommand, Guid>,
    ICommandHandler<CompleteFarmEventCommand, Guid>
{
    public async Task<Result<Guid, DomainError>> Handle(CreateFarmEventCommand cmd, CancellationToken ct)
    {
        var farmEvent = FarmEvent.Create(cmd.Type, cmd.Title, cmd.Description, cmd.Schedule);
        await store.SaveFarmEventAsync(farmEvent, "steward", ct);
        return farmEvent.Id.Value;
    }

    public async Task<Result<Guid, DomainError>> Handle(PublishFarmEventCommand cmd, CancellationToken ct)
    {
        var farmEvent = await store.LoadFarmEventAsync(cmd.EventId.ToString(), ct);
        var result = farmEvent.Publish();
        if (result.IsFailure) return result.Error;
        await store.SaveFarmEventAsync(farmEvent, "steward", ct);
        return farmEvent.Id.Value;
    }

    public async Task<Result<Guid, DomainError>> Handle(CancelFarmEventCommand cmd, CancellationToken ct)
    {
        var farmEvent = await store.LoadFarmEventAsync(cmd.EventId.ToString(), ct);
        var result = farmEvent.Cancel(cmd.Reason);
        if (result.IsFailure) return result.Error;
        await store.SaveFarmEventAsync(farmEvent, "steward", ct);
        return farmEvent.Id.Value;
    }

    public async Task<Result<Guid, DomainError>> Handle(CompleteFarmEventCommand cmd, CancellationToken ct)
    {
        var farmEvent = await store.LoadFarmEventAsync(cmd.EventId.ToString(), ct);
        var result = farmEvent.Complete(cmd.TotalAttendees, cmd.TotalRevenue);
        if (result.IsFailure) return result.Error;
        await store.SaveFarmEventAsync(farmEvent, "steward", ct);
        return farmEvent.Id.Value;
    }
}

public sealed class BookingCommandHandlers(ICampusEventStore store) :
    ICommandHandler<CreateBookingCommand, Guid>,
    ICommandHandler<ConfirmBookingCommand, Guid>,
    ICommandHandler<CheckInBookingCommand, Guid>,
    ICommandHandler<CancelBookingCommand, Guid>,
    ICommandHandler<SignWaiverCommand, Guid>
{
    public async Task<Result<Guid, DomainError>> Handle(CreateBookingCommand cmd, CancellationToken ct)
    {
        var booking = Booking.Create(new EventId(cmd.EventId), cmd.Attendee);
        await store.SaveBookingAsync(booking, "steward", ct);
        return booking.Id.Value;
    }

    public async Task<Result<Guid, DomainError>> Handle(ConfirmBookingCommand cmd, CancellationToken ct)
    {
        var booking = await store.LoadBookingAsync(cmd.BookingId.ToString(), ct);
        var result = booking.Confirm();
        if (result.IsFailure) return result.Error;
        await store.SaveBookingAsync(booking, "steward", ct);
        return booking.Id.Value;
    }

    public async Task<Result<Guid, DomainError>> Handle(CheckInBookingCommand cmd, CancellationToken ct)
    {
        var booking = await store.LoadBookingAsync(cmd.BookingId.ToString(), ct);
        var result = booking.CheckIn();
        if (result.IsFailure) return result.Error;
        await store.SaveBookingAsync(booking, "steward", ct);
        return booking.Id.Value;
    }

    public async Task<Result<Guid, DomainError>> Handle(CancelBookingCommand cmd, CancellationToken ct)
    {
        var booking = await store.LoadBookingAsync(cmd.BookingId.ToString(), ct);
        var result = booking.Cancel(cmd.Reason);
        if (result.IsFailure) return result.Error;
        await store.SaveBookingAsync(booking, "steward", ct);
        return booking.Id.Value;
    }

    public async Task<Result<Guid, DomainError>> Handle(SignWaiverCommand cmd, CancellationToken ct)
    {
        var booking = await store.LoadBookingAsync(cmd.BookingId.ToString(), ct);
        booking.SignWaiver(cmd.Waiver);
        await store.SaveBookingAsync(booking, "steward", ct);
        return booking.Id.Value;
    }
}
