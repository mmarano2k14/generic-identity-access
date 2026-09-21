namespace IdentityAccess.Domain;

/// <summary>A user group scoped to an application and tenant; not a runtime TenantGroupId.</summary>
public sealed record GroupReference
{
    public TenantReference Tenant { get; }
    public ApplicationKey Application { get; }
    public Guid GroupId { get; }

    public GroupReference(TenantReference tenant, ApplicationKey application, Guid groupId)
    {
        ArgumentNullException.ThrowIfNull(tenant);
        ArgumentNullException.ThrowIfNull(application);
        Tenant = tenant;
        Application = application;
        GroupId = ModelGuard.Identifier(groupId, nameof(groupId));
    }
}
