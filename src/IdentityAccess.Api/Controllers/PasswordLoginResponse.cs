using System.ComponentModel.DataAnnotations;
using IdentityAccess.Api;
using IdentityAccess.Application.Authentication;
using Microsoft.AspNetCore.Mvc;

namespace IdentityAccess.Api.Controllers
{

    /// <summary>Represents the response payload for password login.</summary>
    public sealed record PasswordLoginResponse(Guid UserId, Guid SessionId, string SessionToken,
        DateTimeOffset ExpiresAt, string RedirectUri);
}
