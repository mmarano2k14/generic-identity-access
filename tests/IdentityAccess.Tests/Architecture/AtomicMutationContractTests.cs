using IdentityAccess.Application.Administration;
using IdentityAccess.Application.Authentication;
using IdentityAccess.Application.Storage;

namespace IdentityAccess.Tests.Architecture
{
    /// <summary>
    /// Protects multi-row administration mutations from regressing to read-then-write
    /// orchestration across independent store connections.
    /// </summary>
    public sealed class AtomicMutationContractTests
    {
        /// <summary>
        /// Verifies that directory administration depends on the atomic group-membership
        /// mutation contract.
        /// </summary>
        [Fact]
        public void Directory_administration_uses_atomic_group_membership_mutation_store()
        {
            AssertConstructorDependency<DirectoryAdministrationService, IGroupMembershipMutationStore>();
        }

        /// <summary>
        /// Verifies that policy administration depends on the atomic policy-binding mutation
        /// contract and no longer depends on read stores used only for pre-validation.
        /// </summary>
        [Fact]
        public void Policy_administration_uses_atomic_binding_mutation_store()
        {
            var parameters = ConstructorParameters<PolicyAdministrationService>();

            Assert.Contains(typeof(IGroupPolicyBindingMutationStore), parameters);
            Assert.DoesNotContain(typeof(IUserGroupStore), parameters);
            Assert.DoesNotContain(typeof(IResourceScopeStore), parameters);
        }

        /// <summary>
        /// Verifies that credential administration uses the atomic credential mutation
        /// contract instead of a separate user existence read.
        /// </summary>
        [Fact]
        public void Credential_administration_uses_atomic_credential_mutation_store()
        {
            var parameters = ConstructorParameters<CredentialAdministrationService>();

            Assert.Contains(typeof(ICredentialMutationStore), parameters);
            Assert.DoesNotContain(typeof(IUserDirectoryStore), parameters);
        }

        private static void AssertConstructorDependency<TService, TDependency>()
        {
            Assert.Contains(typeof(TDependency), ConstructorParameters<TService>());
        }

        private static Type[] ConstructorParameters<TService>() =>
            Assert.Single(typeof(TService).GetConstructors())
                .GetParameters()
                .Select(parameter => parameter.ParameterType)
                .ToArray();
    }
}
