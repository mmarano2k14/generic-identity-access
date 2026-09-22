using IdentityAccess.Application.Storage;
using IdentityAccess.Domain;

namespace IdentityAccess.Api.Controllers
{
    /// <summary>Represents an identity-scope administration group.</summary>
    public sealed record IdentityScopeAdministrationGroupResponse(
        Guid GroupId,
        string DisplayName,
        GroupStatus Status,
        long Version)
    {
        /// <summary>Maps a persisted group record.</summary>
        public static IdentityScopeAdministrationGroupResponse From(
            VersionedRecord<IdentityScopeAdministrationGroup> record) =>
            new(record.Value.Reference.GroupId, record.Value.DisplayName, record.Value.Status, record.Version);
    }
}
