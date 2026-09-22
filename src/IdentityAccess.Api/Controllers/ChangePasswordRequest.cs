using System.ComponentModel.DataAnnotations;
using IdentityAccess.Application.Authentication;
using IdentityAccess.Application.Storage;
using IdentityAccess.Domain;
using Microsoft.AspNetCore.Mvc;

namespace IdentityAccess.Api.Controllers
{

    /// <summary>Represents the request payload for change password.</summary>
    public sealed record ChangePasswordRequest(string LoginIdentifier,
        [property: DataType(DataType.Password)] string Password, long ExpectedVersion);
}
