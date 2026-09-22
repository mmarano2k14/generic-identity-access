using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using IdentityAccess.Api.Http;

namespace IdentityAccess.Api.Security
{

    /// <summary>Declares require administration capability metadata for an API operation.</summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
    public sealed class RequireAdministrationCapabilityAttribute(string resource, string feature, string action)
        : Attribute, IAsyncAuthorizationFilter
    {
        /// <summary>Evaluates the administration capability requirement before the controller action executes.</summary>
        public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
        {
            var authorizer = context.HttpContext.RequestServices.GetRequiredService<IAdministrationRequestAuthorizer>();
            var result = await authorizer.AuthorizeAsync(context.HttpContext, resource, feature, action,
                context.HttpContext.RequestAborted);
            context.Result = result.Decision switch
            {
                AdministrationAccessDecision.Allowed => null,
                AdministrationAccessDecision.Unauthenticated => ApiProblems.Unauthorized(
                    "Authentication required",
                    "A valid administration session is required for this operation."),
                AdministrationAccessDecision.Denied => ApiProblems.Forbidden(
                    "Forbidden",
                    "The authenticated administration context is not authorized for this operation."),
                _ => ApiProblems.AdministrationAuthorizationUnavailable()
            };
        }
    }
}
