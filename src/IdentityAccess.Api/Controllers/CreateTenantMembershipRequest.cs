using IdentityAccess.Application.Administration;
using IdentityAccess.Application.Storage;
using IdentityAccess.Domain;
using Microsoft.AspNetCore.Mvc;

namespace IdentityAccess.Api.Controllers
{

    /// <summary>Represents the request payload for create tenant membership.</summary>
    public sealed record CreateTenantMembershipRequest(Guid MembershipId, Guid UserId,
        MembershipStatus Status = MembershipStatus.Active);
}
