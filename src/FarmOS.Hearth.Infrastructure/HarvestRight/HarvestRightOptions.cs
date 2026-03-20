namespace FarmOS.Hearth.Infrastructure.HarvestRight;

/// <summary>
/// Configuration options for connecting to the Harvest Right cloud API.
/// Bind to the "HarvestRight" section of appsettings.json.
/// </summary>
public sealed class HarvestRightOptions
{
    public const string SectionName = "HarvestRight";

    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string ApiBase { get; set; } = "https://prod.harvestrightapp.com";
    public string MqttBroker { get; set; } = "mqtt.harvestrightapp.com";
    public int MqttPort { get; set; } = 8883;
    public int WatchdogIntervalSeconds { get; set; } = 30;
    public int SilenceThresholdSeconds { get; set; } = 900;
}
