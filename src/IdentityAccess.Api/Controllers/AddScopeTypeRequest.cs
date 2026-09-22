using IdentityAccess.Application.Administration;
using IdentityAccess.Domain;
using Microsoft.AspNetCore.Mvc;

namespace IdentityAccess.Api.Controllers
{

    /// <summary>Represents the request payload for add scope type.</summary>
    public sealed record AddScopeTypeRequest(string Key, string DisplayName, string? ParentKey, bool CanAttachToTenant);
}
