namespace IdentityAccess.Api.Security
{
    /// <summary>
    /// Resolves a server-trusted administration identity from the current HTTP request without
    /// treating route values or caller-provided identity claims as trusted authentication state.
    /// </summary>
    public interface IAdministrationRequestContextResolver
    {
        /// <summary>Resolves the trusted administration context for the request.</summary>
        ValueTask<AdministrationAuthenticationResult> ResolveAsync(
            HttpContext httpContext,
            CancellationToken cancellationToken);
    }
}
