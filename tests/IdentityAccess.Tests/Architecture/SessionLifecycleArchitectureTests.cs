using IdentityAccess.Application.Authentication;

namespace IdentityAccess.Tests.Architecture
{
    /// <summary>
    /// Protects session lifecycle guarantees that bind session validity to current account state.
    /// </summary>
    public sealed class SessionLifecycleArchitectureTests
    {
        /// <summary>
        /// Verifies local authentication no longer performs a separate user-state read before
        /// session issuance.
        /// </summary>
        [Fact]
        public void Local_authentication_does_not_depend_on_user_directory_store()
        {
            var parameters = Assert.Single(
                    typeof(LocalAuthenticationService).GetConstructors())
                .GetParameters()
                .Select(parameter => parameter.ParameterType)
                .ToArray();

            Assert.DoesNotContain(typeof(IdentityAccess.Application.Storage.IUserDirectoryStore), parameters);
            Assert.Contains(typeof(IAuthenticationSessionStore), parameters);
        }

        /// <summary>
        /// Verifies the session store exposes active-user issuance and bulk revocation contracts.
        /// </summary>
        [Fact]
        public void Session_store_exposes_hardened_lifecycle_operations()
        {
            var methods = typeof(IAuthenticationSessionStore)
                .GetMethods()
                .Select(method => method.Name)
                .ToArray();

            Assert.Contains(nameof(IAuthenticationSessionStore.CreateForActiveUserAsync), methods);
            Assert.Contains(nameof(IAuthenticationSessionStore.ValidateReferenceAsync), methods);
            Assert.Contains(nameof(IAuthenticationSessionStore.RevokeAllForSubjectAsync), methods);
            Assert.Contains(nameof(IAuthenticationSessionStore.RevokeAllForClientAsync), methods);
            Assert.DoesNotContain("CreateAsync", methods);
        }

        /// <summary>
        /// Verifies password changes use the mutation contract that revokes existing sessions.
        /// </summary>
        [Fact]
        public void Credential_mutation_contract_revokes_sessions_on_password_change()
        {
            var methods = typeof(ICredentialMutationStore)
                .GetMethods()
                .Select(method => method.Name)
                .ToArray();

            Assert.Contains(nameof(ICredentialMutationStore.UpdatePasswordAndRevokeSessionsAsync), methods);
            Assert.DoesNotContain("UpdatePasswordForExistingUserAsync", methods);
        }
    }
}
