using IdentityAccess.Domain;

namespace IdentityAccess.Application.Storage
{
    /// <summary>Tenant-constrained user projection backed by an explicit tenant membership.</summary>
    public sealed record TenantUserReadRecord(
        Guid MembershipId,
        TenantReference Tenant,
        SubjectReference Subject,
        string DisplayName,
        UserStatus UserStatus,
        MembershipStatus MembershipStatus,
        long UserVersion,
        long MembershipVersion);
}
