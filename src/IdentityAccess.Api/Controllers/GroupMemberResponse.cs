using IdentityAccess.Application.Administration;
using IdentityAccess.Domain;
using Microsoft.AspNetCore.Mvc;

namespace IdentityAccess.Api.Controllers
{

    /// <summary>Represents the response payload for group member.</summary>
    public sealed record GroupMemberResponse(Guid TenantMembershipId, Guid UserId)
    {
        /// <summary>Creates the response from the supplied domain or persistence record.</summary>
        public static GroupMemberResponse From(GroupMembership membership) =>
            new(membership.TenantMembershipId, membership.Subject.UserId);
    }
}
