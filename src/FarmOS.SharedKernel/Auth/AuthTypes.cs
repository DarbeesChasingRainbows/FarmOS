namespace FarmOS.SharedKernel.Auth;

/// <summary>
/// A farm family member authenticated by PIN.
/// </summary>
public record FarmUser(string Id, string Name, string Role, string PinHash);

/// <summary>
/// Available roles in the sovereign family system.
/// </summary>
public static class FarmRoles
{
    public const string Steward = "steward";         // Full access, all contexts
    public const string Partner = "partner";         // Full access, all contexts
    public const string Apprentice = "apprentice";   // FieldOps + HearthOS, simplified views
    public const string Helper = "helper";           // HearthOS read-only (recipes, timers)
}

/// <summary>
/// PIN-based authentication service for family economy.
/// No external identity provider — fully sovereign.
/// </summary>
public interface IAuthService
{
    Task<FarmUser?> AuthenticateByPinAsync(string pin, CancellationToken ct);
    string HashPin(string pin);
    bool VerifyPin(string pin, string hash);
}
