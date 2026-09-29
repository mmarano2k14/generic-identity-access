using OrganizationDirectory.Domain;

namespace OrganizationDirectory.Application.Storage
{
    /// <summary>Minimal external tenant-membership state used for organization membership validation.</summary>
    public sealed record TenantMembershipReferenceState(
        TenantMembershipReference Reference,
        bool IsActive);
}
