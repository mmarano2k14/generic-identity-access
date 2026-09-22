using System.ComponentModel.DataAnnotations;
using IdentityAccess.Application.Authentication;
using IdentityAccess.Application.Storage;
using IdentityAccess.Domain;
using Microsoft.AspNetCore.Mvc;

namespace IdentityAccess.Api.Controllers
{

    /// <summary>Represents the response payload for credential metadata.</summary>
    public sealed record CredentialMetadataResponse(Guid UserId, string LoginIdentifier, int FailedAccessCount,
        DateTimeOffset? LockoutUntil, long Version)
    {
        /// <summary>Creates the response from the supplied domain or persistence record.</summary>
        public static CredentialMetadataResponse From(CredentialMetadata value) =>
            new(value.UserId, value.LoginIdentifier, value.FailedAccessCount, value.LockoutUntil, value.Version);
    }
}
