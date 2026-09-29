using IdentityAccess.Application.Routing;
using IdentityAccess.Domain;

namespace IdentityAccess.Application.Storage
{
    /// <summary>Operation-scoped user-group persistence. Authorization remains outside this contract.</summary>
    public interface IUserGroupStore
    {
        /// <summary>Gets the requested user group record from the resolved database route.</summary>
        Task<VersionedRecord<UserGroup>?> GetAsync(ResolvedDatabaseRoute route, GroupReference group,
            CancellationToken cancellationToken);

        /// <summary>Lists a bounded window of groups for one tenant and application.</summary>
        Task<IReadOnlyList<VersionedRecord<UserGroup>>> ListAsync(ResolvedDatabaseRoute route, TenantReference tenant,
            ApplicationKey application, string? search, int offset, int limit, CancellationToken cancellationToken);

        /// <summary>Lists reusable real groups across the identity scope/application boundary.</summary>
        Task<IReadOnlyList<VersionedRecord<UserGroup>>> ListTemplatesAsync(ResolvedDatabaseRoute route,
            Guid identityScopeId, ApplicationKey application, string? search, int offset, int limit, bool activeOnly,
            CancellationToken cancellationToken);

        /// <summary>Creates a user group record in the resolved database route.</summary>
        Task<VersionedRecord<UserGroup>> CreateAsync(ResolvedDatabaseRoute route, UserGroup group,
            CancellationToken cancellationToken);

        /// <summary>Lists distinct resource scopes referenced by managed-policy bindings on one reusable source group.</summary>
        Task<IReadOnlyList<GroupTemplateResourceScopeRequirement>> ListTemplateScopeRequirementsAsync(
            ResolvedDatabaseRoute route, GroupReference sourceGroup, CancellationToken cancellationToken);

        /// <summary>
        /// Atomically creates a normal tenant group from a reusable source group and copies its managed-policy bindings.
        /// Scoped bindings require explicit source-to-target resource-scope mappings. Group memberships are never copied.
        /// </summary>
        Task<VersionedRecord<UserGroup>?> CreateFromTemplateAsync(ResolvedDatabaseRoute route,
            GroupReference sourceGroup, GroupReference targetGroup,
            IReadOnlyDictionary<Guid, Guid> resourceScopeMappings,
            CancellationToken cancellationToken);

        /// <summary>Updates a user group record using optimistic concurrency.</summary>
        Task<VersionedRecord<UserGroup>> UpdateAsync(ResolvedDatabaseRoute route, UserGroup group, long expectedVersion,
            CancellationToken cancellationToken);
    }
}
