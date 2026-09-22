using IdentityAccess.Application.Administration;
using IdentityAccess.Application.Storage;
using IdentityAccess.Domain;
using Microsoft.AspNetCore.Mvc;

namespace IdentityAccess.Api.Controllers
{

    /// <summary>Represents the request payload for create policy.</summary>
    public sealed record CreatePolicyRequest(Guid PolicyId, string DisplayName, PolicyStatus Status = PolicyStatus.Active);
}
