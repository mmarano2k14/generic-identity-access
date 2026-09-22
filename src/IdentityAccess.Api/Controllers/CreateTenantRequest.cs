using IdentityAccess.Application.Administration;
using IdentityAccess.Application.Storage;
using IdentityAccess.Domain;
using Microsoft.AspNetCore.Mvc;

namespace IdentityAccess.Api.Controllers
{

    /// <summary>Represents the request payload for create tenant.</summary>
    public sealed record CreateTenantRequest(Guid TenantId, string DisplayName, TenantStatus Status = TenantStatus.Active);
}
