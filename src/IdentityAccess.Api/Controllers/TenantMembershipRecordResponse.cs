using IdentityAccess.Application.Administration;
using IdentityAccess.Application.Storage;
using IdentityAccess.Domain;
using Microsoft.AspNetCore.Mvc;

namespace IdentityAccess.Api.Controllers
{

    /// <summary>Represents the response payload for tenant membership record.</summary>
    public sealed record TenantMembershipRecordResponse(Guid MembershipId, Guid TenantId, Guid UserId,
        MembershipStatus Status, long Version)
    {
        /// <summary>Creates the response from the supplied domain or persistence record.</summary>
        public static TenantMembershipRecordResponse From(VersionedRecord<TenantMembership> record) =>
            new(record.Value.MembershipId, record.Value.Tenant.TenantId, record.Value.Subject.UserId,
                record.Value.Status, record.Version);
    }
}
