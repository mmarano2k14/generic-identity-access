using IdentityAccess.Application.Storage;
using IdentityAccess.Domain;

namespace IdentityAccess.Api.Controllers
{
    /// <summary>Generic authenticator metadata without provider-owned secret material.</summary>
    public sealed record UserAuthenticatorResponse(
        Guid AuthenticatorId,
        Guid UserId,
        string ProviderKey,
        string DisplayName,
        UserAuthenticatorStatus Status,
        DateTimeOffset CreatedAt,
        DateTimeOffset? ConfirmedAt,
        DateTimeOffset? LastUsedAt,
        DateTimeOffset? RevokedAt,
        long Version)
    {
        /// <summary>Creates an HTTP response from a versioned authenticator.</summary>
        public static UserAuthenticatorResponse From(VersionedRecord<UserAuthenticator> record)
        {
            var value = record.Value;
            return new UserAuthenticatorResponse(
                value.AuthenticatorId,
                value.UserId,
                value.Provider.Value,
                value.DisplayName,
                value.Status,
                value.CreatedAt,
                value.ConfirmedAt,
                value.LastUsedAt,
                value.RevokedAt,
                record.Version);
        }
    }
}
