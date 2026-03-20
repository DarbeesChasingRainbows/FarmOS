using System.Net.Http.Headers;
using System.Text;
using FarmOS.SharedKernel.EventStore;
using Microsoft.Extensions.Logging;

namespace FarmOS.IoT.API.Workers;

/// <summary>
/// Production notification channel that sends SMS via Twilio REST API.
/// Requires TWILIO_SID, TWILIO_AUTH_TOKEN, TWILIO_FROM, and ALERT_PHONE env vars.
/// </summary>
public sealed class TwilioSmsNotifier : INotificationChannel, IDisposable
{
    private readonly HttpClient _http;
    private readonly string _from;
    private readonly string _to;
    private readonly string _accountSid;
    private readonly ILogger<TwilioSmsNotifier> _logger;

    public TwilioSmsNotifier(
        string accountSid,
        string authToken,
        string fromNumber,
        string toNumber,
        ILogger<TwilioSmsNotifier> logger)
    {
        _accountSid = accountSid;
        _from = fromNumber;
        _to = toNumber;
        _logger = logger;

        _http = new HttpClient
        {
            BaseAddress = new Uri("https://api.twilio.com")
        };
        var credentials = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{accountSid}:{authToken}"));
        _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", credentials);
    }

    public async Task SendAlertAsync(ExcursionAlertIntegrationEvent alert, CancellationToken ct)
    {
        var body = $"🚨 FarmOS ALERT [{alert.Severity}]\n" +
                   $"Sensor: {alert.SensorType}\n" +
                   $"Zone: {alert.ZoneId}\n" +
                   $"{alert.AlertMessage}\n" +
                   (alert.CorrectiveAction is not null ? $"Action: {alert.CorrectiveAction}" : "");

        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["To"] = _to,
            ["From"] = _from,
            ["Body"] = body
        });

        try
        {
            var response = await _http.PostAsync(
                $"/2010-04-01/Accounts/{_accountSid}/Messages.json",
                content,
                ct);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("SMS sent for excursion {ExcursionId} to {Phone}", alert.ExcursionId, _to);
            }
            else
            {
                var responseBody = await response.Content.ReadAsStringAsync(ct);
                _logger.LogError("Twilio SMS failed ({Status}): {Body}", response.StatusCode, responseBody);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send SMS for excursion {ExcursionId}", alert.ExcursionId);
        }
    }

    public void Dispose() => _http.Dispose();
}
