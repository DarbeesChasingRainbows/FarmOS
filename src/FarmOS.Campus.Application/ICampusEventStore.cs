using FarmOS.Campus.Domain.Aggregates;

namespace FarmOS.Campus.Application;

public interface ICampusEventStore
{
    Task<FarmEvent> LoadFarmEventAsync(string eventId, CancellationToken ct);
    Task SaveFarmEventAsync(FarmEvent farmEvent, string userId, CancellationToken ct);

    Task<Booking> LoadBookingAsync(string bookingId, CancellationToken ct);
    Task SaveBookingAsync(Booking booking, string userId, CancellationToken ct);
}
