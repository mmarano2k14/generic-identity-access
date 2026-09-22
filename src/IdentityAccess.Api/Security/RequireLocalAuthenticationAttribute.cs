using IdentityAccess.Application.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using IdentityAccess.Api.Http;

namespace IdentityAccess.Api.Security
{

    /// <summary>
    /// Fails closed before model binding when local authentication is not configured.
    /// This keeps disabled authentication endpoints distinguishable from malformed requests
    /// and prevents request-body processing from becoming the availability gate.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
    public sealed class RequireLocalAuthenticationAttribute : Attribute, IAsyncAuthorizationFilter
    {
        /// <summary>Fails the request closed when local authentication services are unavailable.</summary>
        public Task OnAuthorizationAsync(AuthorizationFilterContext context)
        {
            if (context.HttpContext.RequestServices.GetService<ILocalAuthenticationService>() is not null)
                return Task.CompletedTask;

            context.Result = ApiProblems.AuthenticationUnavailable();

            return Task.CompletedTask;
        }
    }
}
