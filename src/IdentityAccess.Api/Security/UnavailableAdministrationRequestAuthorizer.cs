using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace IdentityAccess.Api.Security
{

    internal sealed class UnavailableAdministrationRequestAuthorizer : IAdministrationRequestAuthorizer
    {
        /// <inheritdoc />
        public bool CapabilityAuthorizationAvailable => false;

        public ValueTask<AdministrationAccessResult> AuthorizeAsync(HttpContext httpContext, string resource,
            string feature, string action, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return ValueTask.FromResult(AdministrationAccessResult.Unavailable(
                AdministrationAccessFailureCode.AuthorizationUnavailable));
        }
    }
}
