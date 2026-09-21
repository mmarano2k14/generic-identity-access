namespace IdentityAccess.Domain;

/// <summary>Immutable domain state. Status changes require a future authorized application operation.</summary>
public sealed class Tenant
{
    public TenantReference Reference { get; }
    public string DisplayName { get; }
    public TenantStatus Status { get; }

    public Tenant(TenantReference reference, string displayName, TenantStatus status = TenantStatus.Active)
    {
        ArgumentNullException.ThrowIfNull(reference);
        Reference = reference;
        DisplayName = ModelGuard.DisplayName(displayName, nameof(displayName));
        Status = ModelGuard.DefinedEnum(status, nameof(status));
    }
}
