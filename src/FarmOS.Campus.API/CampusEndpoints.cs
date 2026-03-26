using MediatR;
using FarmOS.Campus.Application.Commands;
using FarmOS.SharedKernel;

namespace FarmOS.Campus.API;

public static class CampusEndpoints
{
    public static void MapCampusEndpoints(this WebApplication app)
    {
        var events = app.MapGroup("/api/campus/events");
        var bookings = app.MapGroup("/api/campus/bookings");

        // --- FarmEvents -------------------------------------------------------

        events.MapPost("/", async (CreateFarmEventCommand cmd, IMediator m, CancellationToken ct) =>
        {
            var result = await m.Send(cmd, ct);
            return result.Match(id => Results.Created($"/api/campus/events/{id}", new { id }), err => Results.BadRequest(err));
        });

        events.MapPost("/{id:guid}/publish", async (Guid id, IMediator m, CancellationToken ct) =>
        {
            var result = await m.Send(new PublishFarmEventCommand(id), ct);
            return result.Match(_ => Results.NoContent(), err => Results.BadRequest(err));
        });

        events.MapPost("/{id:guid}/cancel", async (Guid id, CancelFarmEventCommand cmd, IMediator m, CancellationToken ct) =>
        {
            var result = await m.Send(cmd with { EventId = id }, ct);
            return result.Match(_ => Results.NoContent(), err => Results.BadRequest(err));
        });

        events.MapPost("/{id:guid}/complete", async (Guid id, CompleteFarmEventCommand cmd, IMediator m, CancellationToken ct) =>
        {
            var result = await m.Send(cmd with { EventId = id }, ct);
            return result.Match(_ => Results.NoContent(), err => Results.BadRequest(err));
        });

        // --- Bookings ---------------------------------------------------------

        bookings.MapPost("/", async (CreateBookingCommand cmd, IMediator m, CancellationToken ct) =>
        {
            var result = await m.Send(cmd, ct);
            return result.Match(id => Results.Created($"/api/campus/bookings/{id}", new { id }), err => Results.BadRequest(err));
        });

        bookings.MapPost("/{id:guid}/confirm", async (Guid id, IMediator m, CancellationToken ct) =>
        {
            var result = await m.Send(new ConfirmBookingCommand(id), ct);
            return result.Match(_ => Results.NoContent(), err => Results.BadRequest(err));
        });

        bookings.MapPost("/{id:guid}/checkin", async (Guid id, IMediator m, CancellationToken ct) =>
        {
            var result = await m.Send(new CheckInBookingCommand(id), ct);
            return result.Match(_ => Results.NoContent(), err => Results.BadRequest(err));
        });

        bookings.MapPost("/{id:guid}/cancel", async (Guid id, CancelBookingCommand cmd, IMediator m, CancellationToken ct) =>
        {
            var result = await m.Send(cmd with { BookingId = id }, ct);
            return result.Match(_ => Results.NoContent(), err => Results.BadRequest(err));
        });

        bookings.MapPost("/{id:guid}/waiver", async (Guid id, SignWaiverCommand cmd, IMediator m, CancellationToken ct) =>
        {
            var result = await m.Send(cmd with { BookingId = id }, ct);
            return result.Match(_ => Results.NoContent(), err => Results.BadRequest(err));
        });
    }
}
