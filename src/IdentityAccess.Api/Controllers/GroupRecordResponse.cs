using IdentityAccess.Application.Storage;
using IdentityAccess.Domain;

namespace IdentityAccess.Api.Controllers
{
    /// <summary>Represents one real tenant-scoped group, including whether it is reusable.</summary>
    public sealed record GroupRecordResponse(
        Guid TenantId,
        Guid GroupId,
        string DisplayName,
        GroupStatus Status,
        bool IsTemplate,
        long Version)
    {
        /// <summary>Creates the response from the supplied domain or persistence record.</summary>
        public static GroupRecordResponse From(VersionedRecord<UserGroup> record) =>
            new(
                record.Value.Reference.Tenant.TenantId,
                record.Value.Reference.GroupId,
                record.Value.DisplayName,
                record.Value.Status,
                record.Value.IsTemplate,
                record.Version);
    }
}
