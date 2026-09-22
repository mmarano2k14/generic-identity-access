using IdentityAccess.Application.Administration;
using IdentityAccess.Domain;
using Microsoft.AspNetCore.Mvc;

namespace IdentityAccess.Api.Controllers
{

    /// <summary>Represents the response payload for scope type.</summary>
    public sealed record ScopeTypeResponse(string Key, string DisplayName, string? ParentKey, bool CanAttachToTenant)
    {
        /// <summary>Creates the response from the supplied domain or persistence record.</summary>
        public static ScopeTypeResponse From(ApplicationScopeTypeDefinition definition) =>
            new(definition.Type.Value, definition.DisplayName, definition.ParentType?.Value, definition.CanAttachToTenant);
    }
}
