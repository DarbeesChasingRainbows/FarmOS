namespace FarmOS.SharedKernel;

/// <summary>
/// Tenant identifier. In sovereign (single-farm) mode, this is always the hardcoded Sovereign value.
/// When the system evolves to multi-tenant SaaS, this is resolved from the authenticated user's JWT claims.
/// Baking this in from day one means zero refactoring for multi-tenancy.
/// </summary>
public record TenantId(Guid Value)
{
    /// <summary>
    /// The single tenant ID used in sovereign (self-hosted) mode.
    /// </summary>
    public static readonly TenantId Sovereign = new(Guid.Parse("00000000-0000-0000-0000-000000000001"));
}
