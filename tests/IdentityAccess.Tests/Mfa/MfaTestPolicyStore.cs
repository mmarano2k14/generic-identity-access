using IdentityAccess.Application.Authentication.Mfa;
using IdentityAccess.Application.Routing;
using IdentityAccess.Application.Storage;
using IdentityAccess.Domain;

namespace IdentityAccess.Tests.Mfa
{
    internal sealed class MfaTestPolicyStore : IMfaPolicyStore
    {
        public VersionedRecord<MfaPolicy>? Policy { get; set; }

        public Task<VersionedRecord<MfaPolicy>?> GetAsync(
            ResolvedDatabaseRoute route,
            Guid identityScopeId,
            ApplicationKey application,
            CancellationToken cancellationToken) =>
            Task.FromResult(Policy);

        public Task<VersionedRecord<MfaPolicy>> CreateAsync(
            ResolvedDatabaseRoute route,
            MfaPolicy policy,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<VersionedRecord<MfaPolicy>> UpdateAsync(
            ResolvedDatabaseRoute route,
            MfaPolicy policy,
            long expectedVersion,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }
}
