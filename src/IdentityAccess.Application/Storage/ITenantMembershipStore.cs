using IdentityAccess.Application.Routing;
using IdentityAccess.Domain;

namespace IdentityAccess.Application.Storage
{

    /// <summary>Operation-scoped tenant-membership persistence. Authorization remains outside this contract.</summary>
    public interface ITenantMembershipStore
    {
        /// <summary>Gets the requested tenant membership record from the resolved database route.</summary>
        Task<VersionedRecord<TenantMembership>?> GetAsync(ResolvedDatabaseRoute route, Guid identityScopeId,
            Guid membershipId, CancellationToken cancellationToken);

        /// <summary>Finds the tenant membership record matching the supplied logical keys.</summary>
        Task<VersionedRecord<TenantMembership>?> FindAsync(ResolvedDatabaseRoute route, TenantReference tenant,
            SubjectReference subject, CancellationToken cancellationToken);

        /// <summary>Lists tenant membership records for the requested tenant in a bounded deterministic window.</summary>
        Task<IReadOnlyList<VersionedRecord<TenantMembership>>> ListAsync(ResolvedDatabaseRoute route,
            TenantReference tenant, string? search, int offset, int limit, CancellationToken cancellationToken);

        /// <summary>Lists tenant membership records for one subject across the resolved identity scope.</summary>
        Task<IReadOnlyList<VersionedRecord<TenantMembership>>> ListForSubjectAsync(ResolvedDatabaseRoute route,
            SubjectReference subject, CancellationToken cancellationToken);

        /// <summary>Creates a tenant membership record in the resolved database route.</summary>
        Task<VersionedRecord<TenantMembership>> CreateAsync(ResolvedDatabaseRoute route, TenantMembership membership,
            CancellationToken cancellationToken);

        /// <summary>Updates a tenant membership record using optimistic concurrency.</summary>
        Task<VersionedRecord<TenantMembership>> UpdateAsync(ResolvedDatabaseRoute route, TenantMembership membership,
            long expectedVersion, CancellationToken cancellationToken);
    }
}
