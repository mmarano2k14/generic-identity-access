using System.ComponentModel.DataAnnotations;
using IdentityAccess.Api;
using IdentityAccess.Application.Authentication;
using Microsoft.AspNetCore.Mvc;

namespace IdentityAccess.Api.Controllers
{

    /// <summary>Represents the response payload for logout.</summary>
    public sealed record LogoutResponse(string? PostLogoutRedirectUri);
}
