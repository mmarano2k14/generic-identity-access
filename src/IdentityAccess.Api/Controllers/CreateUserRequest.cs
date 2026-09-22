using IdentityAccess.Application.Administration;
using IdentityAccess.Application.Storage;
using IdentityAccess.Domain;
using Microsoft.AspNetCore.Mvc;

namespace IdentityAccess.Api.Controllers
{

    /// <summary>Represents the request payload for create user.</summary>
    public sealed record CreateUserRequest(Guid UserId, string DisplayName, UserStatus Status = UserStatus.Active);
}
