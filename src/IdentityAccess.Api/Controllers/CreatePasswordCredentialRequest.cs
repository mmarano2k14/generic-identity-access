using System.ComponentModel.DataAnnotations;
using IdentityAccess.Application.Authentication;
using IdentityAccess.Application.Storage;
using IdentityAccess.Domain;
using Microsoft.AspNetCore.Mvc;

namespace IdentityAccess.Api.Controllers
{

    /// <summary>Represents the request payload for create password credential.</summary>
    public sealed record CreatePasswordCredentialRequest(string LoginIdentifier,
        [param: DataType(DataType.Password)] string Password);
}
