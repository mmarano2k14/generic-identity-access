using IdentityAccess.Application.Routing;
using IdentityAccess.Application.Storage;
using IdentityAccess.Domain;

namespace IdentityAccess.Tests.Administration
{
    internal sealed class EffectiveAdministrationContextFakeTenantMembershipStore(
        List<VersionedRecord<TenantMembership>> records) : ITenantMembershipStore
    {
        public Task<IReadOnlyList<VersionedRecord<TenantMembership>>> ListForSubjectAsync(
            ResolvedDatabaseRoute route,
            SubjectReference subject,
            CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<VersionedRecord<TenantMembership>>>(records);

        public Task<VersionedRecord<TenantMembership>?> GetAsync(
            ResolvedDatabaseRoute route,
            Guid identityScopeId,
            Guid membershipId,
            CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<VersionedRecord<TenantMembership>?> FindAsync(
            ResolvedDatabaseRoute route,
            TenantReference tenant,
            SubjectReference subject,
            CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<IReadOnlyList<VersionedRecord<TenantMembership>>> ListAsync(
            ResolvedDatabaseRoute route,
            TenantReference tenant,
            string? search,
            int offset,
            int limit,
            CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<VersionedRecord<TenantMembership>> CreateAsync(
            ResolvedDatabaseRoute route,
            TenantMembership membership,
            CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<VersionedRecord<TenantMembership>> UpdateAsync(
            ResolvedDatabaseRoute route,
            TenantMembership membership,
            long expectedVersion,
            CancellationToken cancellationToken) => throw new NotSupportedException();
    }
}
