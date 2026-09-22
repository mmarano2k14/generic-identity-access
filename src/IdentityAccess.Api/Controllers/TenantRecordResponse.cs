using IdentityAccess.Application.Administration;
using IdentityAccess.Application.Storage;
using IdentityAccess.Domain;
using Microsoft.AspNetCore.Mvc;

namespace IdentityAccess.Api.Controllers
{

    /// <summary>Represents the response payload for tenant record.</summary>
    public sealed record TenantRecordResponse(Guid TenantId, string DisplayName, TenantStatus Status, long Version)
    {
        /// <summary>Creates the response from the supplied domain or persistence record.</summary>
        public static TenantRecordResponse From(VersionedRecord<Tenant> record) =>
            new(record.Value.Reference.TenantId, record.Value.DisplayName, record.Value.Status, record.Version);
    }
}
