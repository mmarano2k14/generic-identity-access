using IdentityAccess.Application.Administration;
using IdentityAccess.Application.Storage;
using IdentityAccess.Domain;
using Microsoft.AspNetCore.Mvc;

namespace IdentityAccess.Api.Controllers
{

    /// <summary>Represents the request payload for update policy.</summary>
    public sealed record UpdatePolicyRequest(string DisplayName, PolicyStatus Status, long ExpectedVersion);
}
