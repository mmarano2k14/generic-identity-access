using IdentityAccess.Application.Administration;
using IdentityAccess.Application.Storage;
using IdentityAccess.Domain;
using Microsoft.AspNetCore.Mvc;

namespace IdentityAccess.Api.Controllers
{

    /// <summary>Represents the response payload for group record.</summary>
    public sealed record GroupRecordResponse(Guid GroupId, string DisplayName, GroupStatus Status, long Version)
    {
        /// <summary>Creates the response from the supplied domain or persistence record.</summary>
        public static GroupRecordResponse From(VersionedRecord<UserGroup> record) =>
            new(record.Value.Reference.GroupId, record.Value.DisplayName, record.Value.Status, record.Version);
    }
}
