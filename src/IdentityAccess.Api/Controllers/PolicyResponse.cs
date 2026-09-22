using IdentityAccess.Application.Administration;
using IdentityAccess.Application.Storage;
using IdentityAccess.Domain;
using Microsoft.AspNetCore.Mvc;

namespace IdentityAccess.Api.Controllers
{

    /// <summary>Represents the response payload for policy.</summary>
    public sealed record PolicyResponse(Guid PolicyId, string DisplayName, PolicyStatus Status, long Version)
    {
        /// <summary>Creates the response from the supplied domain or persistence record.</summary>
        public static PolicyResponse From(VersionedRecord<PermissionPolicy> record) =>
            new(record.Value.Reference.PolicyId, record.Value.DisplayName, record.Value.Status, record.Version);
    }
}
