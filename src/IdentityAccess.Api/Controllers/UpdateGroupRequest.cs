using IdentityAccess.Application.Administration;
using IdentityAccess.Application.Storage;
using IdentityAccess.Domain;
using Microsoft.AspNetCore.Mvc;

namespace IdentityAccess.Api.Controllers
{

    /// <summary>Represents the request payload for update group.</summary>
    public sealed record UpdateGroupRequest(string DisplayName, GroupStatus Status, long ExpectedVersion);
}
