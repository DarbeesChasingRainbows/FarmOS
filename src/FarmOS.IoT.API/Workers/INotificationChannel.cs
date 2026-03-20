using FarmOS.SharedKernel.EventStore;

namespace FarmOS.IoT.API.Workers;

/// <summary>
/// Abstraction for notification delivery channels (SMS, email, push, etc.).
/// Implementations are swappable via DI — ConsoleNotifier for dev, TwilioSmsNotifier for prod.
/// </summary>
public interface INotificationChannel
{
    Task SendAlertAsync(ExcursionAlertIntegrationEvent alert, CancellationToken ct);
}
