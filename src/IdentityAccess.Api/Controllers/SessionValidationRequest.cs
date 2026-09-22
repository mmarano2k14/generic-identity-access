using System.ComponentModel.DataAnnotations;
using IdentityAccess.Api;
using IdentityAccess.Application.Authentication;
using Microsoft.AspNetCore.Mvc;

namespace IdentityAccess.Api.Controllers
{

    /// <summary>Represents the request payload for session validation.</summary>
    public sealed record SessionValidationRequest(Guid SessionId, string SessionToken);
}
