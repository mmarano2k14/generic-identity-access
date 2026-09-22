using System.ComponentModel.DataAnnotations;
using IdentityAccess.Api;
using IdentityAccess.Application.Authentication;
using Microsoft.AspNetCore.Mvc;

namespace IdentityAccess.Api.Controllers
{

    /// <summary>Represents the request payload for logout.</summary>
    public sealed record LogoutRequest(Guid SessionId, string SessionToken, string? PostLogoutRedirectUri = null);
}
