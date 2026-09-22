namespace IdentityAccess.Api.Security
{
    /// <summary>
    /// Establishes and verifies a trusted administration identity from a local session. Capability
    /// authorization remains fail-closed until the identity authorization service is connected.
    /// </summary>
    internal sealed class SessionBackedAdministrationRequestAuthorizer(
        IAdministrationRequestContextResolver contextResolver)
        : IAdministrationRequestAuthorizer
    {
        /// <inheritdoc />
        public bool CapabilityAuthorizationAvailable => false;

        /// <inheritdoc />
        public async ValueTask<AdministrationAccessResult> AuthorizeAsync(
            HttpContext httpContext,
            string resource,
            string feature,
            string action,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(httpContext);
            ArgumentException.ThrowIfNullOrWhiteSpace(resource);
            ArgumentException.ThrowIfNullOrWhiteSpace(feature);
            ArgumentException.ThrowIfNullOrWhiteSpace(action);

            var authentication = await contextResolver
                .ResolveAsync(
                    httpContext,
                    cancellationToken)
                .ConfigureAwait(false);

            if (authentication.Decision ==
                AdministrationAuthenticationDecision.Unavailable)
            {
                return AdministrationAccessResult.Unavailable(
                    AdministrationAccessFailureCode.AuthenticationUnavailable);
            }

            if (authentication.Decision !=
                    AdministrationAuthenticationDecision.Authenticated ||
                authentication.Context is null)
            {
                return AdministrationAccessResult.Unauthenticated(
                    AdministrationAccessFailureCode.AuthenticationRequired);
            }

            var context = authentication.Context;

            if (!AdministrationRequestBoundary.Matches(
                    httpContext,
                    context))
            {
                return AdministrationAccessResult.Deny(
                    AdministrationAccessFailureCode.AuthenticationContextMismatch);
            }

            AdministrationAuditActivityContext.Apply(
                context);

            httpContext.Features.Set(
                new AdministrationRequestContextFeature(
                    context));

            return AdministrationAccessResult.Unavailable(
                AdministrationAccessFailureCode.AuthorizationUnavailable);
        }
    }
}
