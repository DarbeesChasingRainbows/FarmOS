using FarmOS.IoT.Domain;
using FarmOS.IoT.Domain.Events;

namespace FarmOS.IoT.Application;

/// <summary>
/// Abstraction for dispatching excursion alerts (SMS, email, RabbitMQ, etc.).
/// Implementations are swappable via DI — stub logging for dev, Twilio/SMTP for production.
/// </summary>
public interface IAlertNotifier
{
    Task NotifyExcursionAlertAsync(ExcursionAlertFired alert, CancellationToken ct);
}
