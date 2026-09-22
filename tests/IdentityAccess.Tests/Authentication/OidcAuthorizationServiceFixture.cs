using IdentityAccess.Application.Authentication;

namespace IdentityAccess.Tests.Authentication
{
    /// <summary>Groups the protocol service and deterministic collaborators used by OIDC tests.</summary>
    internal sealed record OidcAuthorizationServiceFixture(
        OidcAuthorizationService Service,
        OidcTestAuthorizationCodeStore Store,
        OidcTestCodeService CodeService,
        OidcTestRefreshTokenStore RefreshTokenStore,
        OidcTestRefreshTokenService RefreshTokenService);
}
