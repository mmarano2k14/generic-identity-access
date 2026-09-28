using IdentityAccess.Application.Storage;
using IdentityAccess.Domain;

namespace IdentityAccess.Application.Administration
{

    /// <summary>Defines the contract for directory administration service.</summary>
    public interface IDirectoryAdministrationService
    {
        /// <summary>Gets the requested user.</summary>
        Task<VersionedRecord<User>?> GetUserAsync(Guid identityScopeId, ApplicationKey application,
            Guid userId, CancellationToken cancellationToken);
        /// <summary>Lists users in a bounded deterministic window.</summary>
        Task<IReadOnlyList<VersionedRecord<User>>> ListUsersAsync(Guid identityScopeId, ApplicationKey application, string? search,
            int offset, int limit, CancellationToken cancellationToken);
        /// <summary>Creates a user.</summary>
        Task<VersionedRecord<User>> CreateUserAsync(Guid identityScopeId, ApplicationKey application,
            Guid userId, string displayName, UserStatus status, CancellationToken cancellationToken);
        /// <summary>Updates a user using optimistic concurrency.</summary>
        Task<VersionedRecord<User>> UpdateUserAsync(Guid identityScopeId, ApplicationKey application,
            Guid userId, string displayName, UserStatus status, long expectedVersion, CancellationToken cancellationToken);

        /// <summary>Gets the requested tenant.</summary>
        Task<VersionedRecord<Tenant>?> GetTenantAsync(Guid identityScopeId, ApplicationKey application,
            Guid tenantId, CancellationToken cancellationToken);
        /// <summary>Lists tenants in a bounded deterministic window.</summary>
        Task<IReadOnlyList<VersionedRecord<Tenant>>> ListTenantsAsync(Guid identityScopeId, ApplicationKey application, string? search,
            int offset, int limit, CancellationToken cancellationToken);
        /// <summary>Creates a tenant.</summary>
        Task<VersionedRecord<Tenant>> CreateTenantAsync(Guid identityScopeId, ApplicationKey application,
            Guid tenantId, string displayName, TenantStatus status, CancellationToken cancellationToken);
        /// <summary>Updates a tenant using optimistic concurrency.</summary>
        Task<VersionedRecord<Tenant>> UpdateTenantAsync(Guid identityScopeId, ApplicationKey application,
            Guid tenantId, string displayName, TenantStatus status, long expectedVersion, CancellationToken cancellationToken);

        /// <summary>Gets the requested tenant membership.</summary>
        Task<VersionedRecord<TenantMembership>?> GetTenantMembershipAsync(Guid identityScopeId,
            ApplicationKey application, Guid tenantId, Guid membershipId, CancellationToken cancellationToken);
        /// <summary>Finds a tenant membership for the requested user.</summary>
        Task<VersionedRecord<TenantMembership>?> FindTenantMembershipByUserAsync(Guid identityScopeId,
            ApplicationKey application, Guid tenantId, Guid userId, CancellationToken cancellationToken);
        /// <summary>Lists tenant memberships in a bounded deterministic window.</summary>
        Task<IReadOnlyList<VersionedRecord<TenantMembership>>> ListTenantMembershipsAsync(Guid identityScopeId,
            ApplicationKey application, Guid tenantId, string? search, int offset, int limit, CancellationToken cancellationToken);
        /// <summary>Creates a tenant membership.</summary>
        Task<VersionedRecord<TenantMembership>> CreateTenantMembershipAsync(Guid identityScopeId,
            ApplicationKey application, Guid tenantId, Guid membershipId, Guid userId, MembershipStatus status,
            CancellationToken cancellationToken);
        /// <summary>Updates a tenant membership using optimistic concurrency.</summary>
        Task<VersionedRecord<TenantMembership>?> UpdateTenantMembershipAsync(Guid identityScopeId,
            ApplicationKey application, Guid tenantId, Guid membershipId, MembershipStatus status, long expectedVersion,
            CancellationToken cancellationToken);

        /// <summary>Gets the requested user group.</summary>
        Task<VersionedRecord<UserGroup>?> GetGroupAsync(Guid identityScopeId, Guid tenantId,
            ApplicationKey application, Guid groupId, CancellationToken cancellationToken);
        /// <summary>Lists user groups in a bounded deterministic window.</summary>
        Task<IReadOnlyList<VersionedRecord<UserGroup>>> ListGroupsAsync(Guid identityScopeId, Guid tenantId,
            ApplicationKey application, string? search, int offset, int limit, CancellationToken cancellationToken);
        /// <summary>Lists active real groups explicitly marked as reusable templates.</summary>
        Task<IReadOnlyList<VersionedRecord<UserGroup>>> ListGroupTemplatesAsync(Guid identityScopeId,
            ApplicationKey application, string? search, int offset, int limit, bool activeOnly,
            CancellationToken cancellationToken);
        /// <summary>Creates a normal user group.</summary>
        Task<VersionedRecord<UserGroup>> CreateGroupAsync(Guid identityScopeId, Guid tenantId,
            ApplicationKey application, Guid groupId, string displayName, GroupStatus status,
            CancellationToken cancellationToken);
        /// <summary>Creates a normal tenant group from a reusable source group and clones managed-policy bindings only.</summary>
        Task<VersionedRecord<UserGroup>?> CreateGroupFromTemplateAsync(Guid identityScopeId, Guid tenantId,
            ApplicationKey application, Guid sourceTenantId, Guid sourceGroupId, Guid groupId,
            CancellationToken cancellationToken);
        /// <summary>Updates a user group using optimistic concurrency while preserving reusable-template state.</summary>
        Task<VersionedRecord<UserGroup>> UpdateGroupAsync(Guid identityScopeId, Guid tenantId,
            ApplicationKey application, Guid groupId, string displayName, GroupStatus status, long expectedVersion,
            CancellationToken cancellationToken);
        /// <summary>Updates a real group definition and its reusable-template marker from identity-scope administration.</summary>
        Task<VersionedRecord<UserGroup>> UpdateReusableGroupAsync(Guid identityScopeId, Guid tenantId,
            ApplicationKey application, Guid groupId, string displayName, GroupStatus status, bool isTemplate,
            long expectedVersion, CancellationToken cancellationToken);

        /// <summary>Lists members of the requested user group.</summary>
        Task<IReadOnlyList<GroupMembership>> ListGroupMembersAsync(Guid identityScopeId, Guid tenantId,
            ApplicationKey application, Guid groupId, CancellationToken cancellationToken);
        /// <summary>Lists all group-membership assignments inside one tenant/application boundary.</summary>
        Task<IReadOnlyList<GroupMembership>> ListTenantGroupAssignmentsAsync(Guid identityScopeId, Guid tenantId,
            ApplicationKey application, CancellationToken cancellationToken);
        /// <summary>Adds a tenant member to the requested user group.</summary>
        Task<GroupMembership?> AddGroupMemberAsync(Guid identityScopeId, Guid tenantId, ApplicationKey application,
            Guid groupId, Guid tenantMembershipId, CancellationToken cancellationToken);
        /// <summary>Removes a tenant member from the requested user group.</summary>
        Task<bool> RemoveGroupMemberAsync(Guid identityScopeId, Guid tenantId, ApplicationKey application,
            Guid groupId, Guid tenantMembershipId, CancellationToken cancellationToken);
    }
}
