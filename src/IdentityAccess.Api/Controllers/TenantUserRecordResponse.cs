using IdentityAccess.Application.Storage;
using IdentityAccess.Domain;

namespace IdentityAccess.Api.Controllers
{
    /// <summary>Represents a user projection constrained by an explicit tenant membership.</summary>
    public sealed record TenantUserRecordResponse(
        Guid MembershipId,
        Guid TenantId,
        Guid UserId,
        string DisplayName,
        UserStatus UserStatus,
        MembershipStatus MembershipStatus,
        long UserVersion,
        long MembershipVersion)
    {
        /// <summary>Creates the response from the tenant-constrained read projection.</summary>
        public static TenantUserRecordResponse From(TenantUserReadRecord record) =>
            new(
                record.MembershipId,
                record.Tenant.TenantId,
                record.Subject.UserId,
                record.DisplayName,
                record.UserStatus,
                record.MembershipStatus,
                record.UserVersion,
                record.MembershipVersion);
    }
}
