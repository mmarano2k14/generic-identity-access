using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace IdentityAccess.Api.Security
{

    /// <summary>Defines the contract for administration request authorizer.</summary>
    public interface IAdministrationRequestAuthorizer
    {
        /// <summary>
        /// Gets a value indicating whether capability authorization is connected and can make
        /// allow/deny decisions.
        /// </summary>
        bool CapabilityAuthorizationAvailable { get; }

        /// <summary>Evaluates whether the current administration context is authorized for the requested capability.</summary>
        ValueTask<AdministrationAccessResult> AuthorizeAsync(HttpContext httpContext, string resource, string feature,
            string action, CancellationToken cancellationToken);
    }
}
