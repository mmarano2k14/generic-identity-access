using IdentityAccess.Application.Administration;
using IdentityAccess.Application.Storage;
using IdentityAccess.Domain;
using Microsoft.AspNetCore.Mvc;

namespace IdentityAccess.Api.Controllers
{

    /// <summary>Represents the request payload for create group.</summary>
    public sealed record CreateGroupRequest(Guid GroupId, string DisplayName, GroupStatus Status = GroupStatus.Active);
}
