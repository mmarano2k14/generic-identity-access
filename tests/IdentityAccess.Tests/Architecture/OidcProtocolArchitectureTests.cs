using IdentityAccess.Application.Authentication;

namespace IdentityAccess.Tests.Architecture
{
    /// <summary>
    /// Protects the public-client OIDC surface as strict Authorization Code + PKCE S256 with
    /// rotating refresh tokens and no client-secret or password-grant fallback.
    /// </summary>
    public sealed class OidcProtocolArchitectureTests
    {
        /// <summary>Verifies client registration exposes no client secret surface.</summary>
        [Fact]
        public void Authentication_client_registration_has_no_client_secret()
        {
            var properties = typeof(AuthenticationClientRegistration)
                .GetProperties()
                .Select(property => property.Name)
                .ToArray();

            Assert.DoesNotContain(
                "ClientSecret",
                properties,
                StringComparer.OrdinalIgnoreCase);

            Assert.DoesNotContain(
                "ClientSecretHash",
                properties,
                StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>Verifies the token endpoint supports only authorization-code/PKCE and refresh-token inputs.</summary>
        [Fact]
        public void Token_endpoint_has_no_password_or_client_secret_grant_inputs()
        {
            var source = Read(
                "src",
                "IdentityAccess.Api",
                "Controllers",
                "OidcTokenController.cs");

            Assert.Contains(
                "grant_type",
                source,
                StringComparison.Ordinal);

            Assert.Contains(
                "code_verifier",
                source,
                StringComparison.Ordinal);

            Assert.Contains(
                "refresh_token",
                source,
                StringComparison.Ordinal);

            var service = Read(
                "src",
                "IdentityAccess.Application",
                "Authentication",
                "OidcAuthorizationService.cs");

            Assert.Contains(
                "\"authorization_code\"",
                service,
                StringComparison.Ordinal);

            Assert.Contains(
                "RefreshAsync",
                service,
                StringComparison.Ordinal);

            Assert.DoesNotContain(
                "client_secret",
                source,
                StringComparison.OrdinalIgnoreCase);

            Assert.DoesNotContain(
                "password",
                source,
                StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>Verifies the authorization service requires S256 and the openid scope.</summary>
        [Fact]
        public void Authorization_service_requires_s256_and_openid()
        {
            var source = Read(
                "src",
                "IdentityAccess.Application",
                "Authentication",
                "OidcAuthorizationService.cs");

            Assert.Contains(
                "\"S256\"",
                source,
                StringComparison.Ordinal);

            Assert.Contains(
                "\"openid\"",
                source,
                StringComparison.Ordinal);

            Assert.DoesNotContain(
                "\"plain\"",
                source,
                StringComparison.Ordinal);
        }

        /// <summary>Verifies discovery publishes the complete configured signing-key set.</summary>
        [Fact]
        public void Jwks_discovery_projects_all_published_signing_keys()
        {
            var controller = Read(
                "src",
                "IdentityAccess.Api",
                "Controllers",
                "OidcDiscoveryController.cs");

            Assert.Contains(
                "service.SigningKeys",
                controller,
                StringComparison.Ordinal);

            var issuer = Read(
                "src",
                "IdentityAccess.Infrastructure.Authentication",
                "RsaOidcTokenIssuer.cs");

            Assert.Contains(
                "activeSigningKeyId",
                issuer,
                StringComparison.Ordinal);

            Assert.Contains(
                "activeRsa.SignData",
                issuer,
                StringComparison.Ordinal);
        }

        /// <summary>Verifies administration Bearer authentication is composed with the existing local-session transport.</summary>
        [Fact]
        public void Administration_api_supports_strict_bearer_validation_without_replacing_local_sessions()
        {
            var registration = Read(
                "src",
                "IdentityAccess.Api",
                "Security",
                "AdministrationSecurityServiceRegistration.cs");

            Assert.Contains(
                "IOidcAccessTokenValidator",
                registration,
                StringComparison.Ordinal);

            Assert.Contains(
                "CompositeAdministrationRequestContextResolver",
                registration,
                StringComparison.Ordinal);

            var resolver = Read(
                "src",
                "IdentityAccess.Api",
                "Security",
                "BearerAdministrationRequestContextResolver.cs");

            Assert.Contains(
                "IOidcAccessTokenSessionValidator",
                resolver,
                StringComparison.Ordinal);

            Assert.Contains(
                "BearerTokenInvalid",
                resolver,
                StringComparison.Ordinal);
        }

        /// <summary>Verifies access-token validation owns strict signature, key, issuer, audience, and claim checks.</summary>
        [Fact]
        public void Access_token_validator_is_strict_and_process_pinned()
        {
            var validator = Read(
                "src",
                "IdentityAccess.Infrastructure.Authentication",
                "RsaOidcAccessTokenValidator.cs");

            Assert.Contains("RS256", validator, StringComparison.Ordinal);
            Assert.Contains("UnknownSigningKey", validator, StringComparison.Ordinal);
            Assert.Contains("SignatureInvalid", validator, StringComparison.Ordinal);
            Assert.Contains("IssuerMismatch", validator, StringComparison.Ordinal);
            Assert.Contains("AudienceMismatch", validator, StringComparison.Ordinal);
            Assert.Contains("identity_scope_id", validator, StringComparison.Ordinal);
            Assert.Contains("application_key", validator, StringComparison.Ordinal);
            Assert.Contains("client_id", validator, StringComparison.Ordinal);
            Assert.Contains("sid", validator, StringComparison.Ordinal);
        }

        private static string Read(params string[] segments)
        {
            var current = new DirectoryInfo(AppContext.BaseDirectory);

            while (current is not null)
            {
                if (File.Exists(
                        Path.Combine(
                            current.FullName,
                            "IdentityAccess.sln")))
                {
                    return File.ReadAllText(
                        Path.Combine(
                            new[] { current.FullName }
                                .Concat(segments)
                                .ToArray()));
                }

                current = current.Parent;
            }

            throw new InvalidOperationException(
                "Repository root could not be located.");
        }
    }
}
