using IdentityAccess.Api.Security;
using Microsoft.AspNetCore.Http;

namespace IdentityAccess.Tests.Authorization
{
    /// <summary>Returns a fixed trusted administration authentication result for authorization tests.</summary>
    internal sealed class RbacAdministrationTestContextResolver(
        AdministrationAuthenticationResult result)
        : IAdministrationRequestContextResolver
    {
        /// <inheritdoc />
        public ValueTask<AdministrationAuthenticationResult> ResolveAsync(
            HttpContext httpContext,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return ValueTask.FromResult(result);
        }
    }
}
