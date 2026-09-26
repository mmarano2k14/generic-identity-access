using IdentityAccess.Application.Routing;
using IdentityAccess.Domain;

namespace IdentityAccess.Application.Storage
{
    /// <summary>Persistence for reusable application-managed policy metadata.</summary>
    public interface IManagedPolicyStore
    {
        /// <summary>Gets one managed policy.</summary>
        Task<VersionedRecord<ManagedPolicy>?> GetAsync(
            ResolvedDatabaseRoute route,
            ManagedPolicyReference policy,
            CancellationToken cancellationToken);

        /// <summary>Lists a bounded window of managed policies for one identity scope and application.</summary>
        Task<IReadOnlyList<VersionedRecord<ManagedPolicy>>> ListAsync(
            ResolvedDatabaseRoute route,
            Guid identityScopeId,
            ApplicationKey application,
            string? search,
            int offset,
            int limit,
            CancellationToken cancellationToken);

        /// <summary>Lists active managed policies whose default version is published and attachable.</summary>
        Task<IReadOnlyList<VersionedRecord<ManagedPolicy>>> ListAttachableAsync(
            ResolvedDatabaseRoute route,
            Guid identityScopeId,
            ApplicationKey application,
            string? search,
            int offset,
            int limit,
            CancellationToken cancellationToken);

        /// <summary>Creates a managed policy.</summary>
        Task<VersionedRecord<ManagedPolicy>> CreateAsync(
            ResolvedDatabaseRoute route,
            ManagedPolicy policy,
            CancellationToken cancellationToken);

        /// <summary>Updates managed policy metadata using optimistic concurrency.</summary>
        Task<VersionedRecord<ManagedPolicy>> UpdateAsync(
            ResolvedDatabaseRoute route,
            ManagedPolicy policy,
            long expectedVersion,
            CancellationToken cancellationToken);
    }
}
