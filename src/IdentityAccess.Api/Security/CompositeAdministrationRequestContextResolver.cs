namespace IdentityAccess.Api.Security
{
    /// <summary>
    /// Selects exactly one trusted administration authentication transport from the Authorization
    /// scheme without merging local-session and bearer credentials.
    /// </summary>
    internal sealed class CompositeAdministrationRequestContextResolver(
        LocalSessionAdministrationRequestContextResolver localSessionResolver,
        BearerAdministrationRequestContextResolver bearerResolver)
        : IAdministrationRequestContextResolver
    {
        /// <inheritdoc />
        public ValueTask<AdministrationAuthenticationResult> ResolveAsync(
            HttpContext httpContext,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(httpContext);

            var values = httpContext.Request.Headers["Authorization"];

            if (values.Count == 1 &&
                values[0] is { } authorization &&
                authorization.StartsWith(
                    BearerAdministrationRequestContextResolver.AuthorizationScheme + " ",
                    StringComparison.OrdinalIgnoreCase))
            {
                return bearerResolver.ResolveAsync(
                    httpContext,
                    cancellationToken);
            }

            return localSessionResolver.ResolveAsync(
                httpContext,
                cancellationToken);
        }
    }
}
