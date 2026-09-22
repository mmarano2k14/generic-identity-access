using IdentityAccess.Application.Administration;
using IdentityAccess.Application.Storage;
using IdentityAccess.Domain;
using Microsoft.AspNetCore.Mvc;

namespace IdentityAccess.Api.Controllers
{

    /// <summary>Represents the request payload for update tenant.</summary>
    public sealed record UpdateTenantRequest(string DisplayName, TenantStatus Status, long ExpectedVersion);
}
