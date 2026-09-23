using IdentityAccess.Application.Storage;
using IdentityAccess.Domain;

namespace IdentityAccess.Api.Controllers
{
    /// <summary>Represents one generic MFA policy.</summary>
    public sealed record MfaPolicyResponse(
        MfaPolicyMode Mode,
        IReadOnlyList<string> AllowedProviders,
        long Version)
    {
        /// <summary>Creates a response from a versioned policy.</summary>
        public static MfaPolicyResponse From(VersionedRecord<MfaPolicy> record) =>
            new(
                record.Value.Mode,
                record.Value.AllowedProviders.Select(provider => provider.Value).ToArray(),
                record.Version);
    }
}
