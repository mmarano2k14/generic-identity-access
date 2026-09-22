using IdentityAccess.Application.Storage;
using IdentityAccess.Domain;

namespace IdentityAccess.Api.Controllers
{
    /// <summary>Represents an identity-scope administration policy.</summary>
    public sealed record IdentityScopeAdministrationPolicyResponse(
        Guid PolicyId,
        string DisplayName,
        PolicyStatus Status,
        long Version)
    {
        /// <summary>Maps a persisted policy record.</summary>
        public static IdentityScopeAdministrationPolicyResponse From(
            VersionedRecord<IdentityScopeAdministrationPolicy> record) =>
            new(record.Value.Reference.PolicyId, record.Value.DisplayName, record.Value.Status, record.Version);
    }
}
