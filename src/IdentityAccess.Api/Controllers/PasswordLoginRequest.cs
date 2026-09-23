using System.ComponentModel.DataAnnotations;
using IdentityAccess.Api;
using IdentityAccess.Application.Authentication;
using Microsoft.AspNetCore.Mvc;

namespace IdentityAccess.Api.Controllers
{

    /// <summary>Represents the request payload for password login.</summary>
    public sealed record PasswordLoginRequest(
        string LoginIdentifier,
        [param: DataType(DataType.Password)] string Password,
        string RedirectUri);
}
