namespace FarmOS.Hearth.Application;

/// <summary>
/// Abstracts the Harvest Right cloud REST API for authentication and device discovery.
/// Implementation lives in Infrastructure; the Application layer depends only on this interface.
/// </summary>
public interface IHarvestRightAuthClient
{
    /// <summary>Authenticate with email/password and obtain an access token session.</summary>
    Task<HarvestRightSession> LoginAsync(CancellationToken ct);

    /// <summary>Refresh an existing session using its refresh token. Falls back to full login on failure.</summary>
    Task<HarvestRightSession> RefreshTokenAsync(HarvestRightSession current, CancellationToken ct);

    /// <summary>Fetch all freeze-dryer devices registered to the authenticated account.</summary>
    Task<HarvestRightDryer[]> GetFreezeDryersAsync(HarvestRightSession session, CancellationToken ct);
}

/// <summary>Active session with the Harvest Right cloud API.</summary>
public record HarvestRightSession(
    string AccessToken,
    string RefreshToken,
    DateTimeOffset RefreshAfter,
    string CustomerId,
    string UserId);

/// <summary>A Harvest Right freeze-dryer unit registered in the cloud.</summary>
public record HarvestRightDryer(
    int DryerId,
    string Serial,
    string Name,
    string Model,
    string FirmwareVersion);
