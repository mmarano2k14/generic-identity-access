using IdentityAccess.Application.Storage;
using IdentityAccess.Domain;

namespace IdentityAccess.Api.Controllers
{
    /// <summary>Managed policy metadata exposed through the administration API.</summary>
    public sealed record ManagedPolicyResponse(
        Guid PolicyId,
        string PolicyKey,
        string DisplayName,
        PolicyStatus Status,
        int? DefaultVersion,
        long Version)
    {
        public static ManagedPolicyResponse From(VersionedRecord<ManagedPolicy> record) =>
            new(
                record.Value.Reference.PolicyId,
                record.Value.Key.Value,
                record.Value.DisplayName,
                record.Value.Status,
                record.Value.DefaultVersion,
                record.Version);
    }
}
