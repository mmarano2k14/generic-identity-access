using IdentityAccess.Application.Routing;
using IdentityAccess.Application.Security;
using IdentityAccess.Application.Storage;
using IdentityAccess.Domain;

namespace IdentityAccess.Application.Administration
{

    /// <summary>Provides application operations for directory administration.</summary>
    public sealed class DirectoryAdministrationService(
        IDatabaseRouteResolver routeResolver,
        IUserDirectoryStore users,
        ITenantDirectoryStore tenants,
        ITenantMembershipStore tenantMemberships,
        IUserGroupStore groups,
        IGroupMembershipStore groupMemberships,
        IGroupMembershipMutationStore groupMembershipMutations,
        ISecurityAuditWriter auditWriter) : IDirectoryAdministrationService
    {
        /// <summary>Gets the requested user.</summary>
        public async Task<VersionedRecord<User>?> GetUserAsync(Guid identityScopeId, ApplicationKey application,
            Guid userId, CancellationToken cancellationToken)
        {
            var route = await ResolveAsync(identityScopeId, application, cancellationToken);
            return await users.GetAsync(route, new SubjectReference(identityScopeId, userId), cancellationToken);
        }

        /// <summary>Creates a user.</summary>
        public async Task<VersionedRecord<User>> CreateUserAsync(Guid identityScopeId, ApplicationKey application,
            Guid userId, string displayName, UserStatus status, CancellationToken cancellationToken)
        {
            var route = await ResolveAsync(identityScopeId, application, cancellationToken);
            var value = new User(new SubjectReference(identityScopeId, userId), displayName, status);
            var created = await users.CreateAsync(route, value, cancellationToken);
            await AuditAsync(route, SecurityAuditEventType.UserCreated, identityScopeId, application,
                null, userId, userId.ToString("D"), cancellationToken);
            return created;
        }

        /// <summary>Updates a user using optimistic concurrency.</summary>
        public async Task<VersionedRecord<User>> UpdateUserAsync(Guid identityScopeId, ApplicationKey application,
            Guid userId, string displayName, UserStatus status, long expectedVersion, CancellationToken cancellationToken)
        {
            var route = await ResolveAsync(identityScopeId, application, cancellationToken);
            var value = new User(new SubjectReference(identityScopeId, userId), displayName, status);
            var updated = await users.UpdateAsync(route, value, expectedVersion, cancellationToken);
            await AuditAsync(route, SecurityAuditEventType.UserUpdated, identityScopeId, application,
                null, userId, userId.ToString("D"), cancellationToken);
            return updated;
        }

        /// <summary>Gets the requested tenant.</summary>
        public async Task<VersionedRecord<Tenant>?> GetTenantAsync(Guid identityScopeId, ApplicationKey application,
            Guid tenantId, CancellationToken cancellationToken)
        {
            var route = await ResolveAsync(identityScopeId, application, cancellationToken);
            return await tenants.GetAsync(route, new TenantReference(identityScopeId, tenantId), cancellationToken);
        }

        /// <summary>Creates a tenant.</summary>
        public async Task<VersionedRecord<Tenant>> CreateTenantAsync(Guid identityScopeId, ApplicationKey application,
            Guid tenantId, string displayName, TenantStatus status, CancellationToken cancellationToken)
        {
            var route = await ResolveAsync(identityScopeId, application, cancellationToken);
            var value = new Tenant(new TenantReference(identityScopeId, tenantId), displayName, status);
            var created = await tenants.CreateAsync(route, value, cancellationToken);
            await AuditAsync(route, SecurityAuditEventType.TenantCreated, identityScopeId, application,
                tenantId, null, tenantId.ToString("D"), cancellationToken);
            return created;
        }

        /// <summary>Updates a tenant using optimistic concurrency.</summary>
        public async Task<VersionedRecord<Tenant>> UpdateTenantAsync(Guid identityScopeId, ApplicationKey application,
            Guid tenantId, string displayName, TenantStatus status, long expectedVersion,
            CancellationToken cancellationToken)
        {
            var route = await ResolveAsync(identityScopeId, application, cancellationToken);
            var value = new Tenant(new TenantReference(identityScopeId, tenantId), displayName, status);
            var updated = await tenants.UpdateAsync(route, value, expectedVersion, cancellationToken);
            await AuditAsync(route, SecurityAuditEventType.TenantUpdated, identityScopeId, application,
                tenantId, null, tenantId.ToString("D"), cancellationToken);
            return updated;
        }

        /// <summary>Gets the requested tenant membership.</summary>
        public async Task<VersionedRecord<TenantMembership>?> GetTenantMembershipAsync(Guid identityScopeId,
            ApplicationKey application, Guid tenantId, Guid membershipId, CancellationToken cancellationToken)
        {
            var route = await ResolveAsync(identityScopeId, application, cancellationToken);
            var record = await tenantMemberships.GetAsync(route, identityScopeId, membershipId, cancellationToken);
            return record is not null && record.Value.Tenant.TenantId == tenantId ? record : null;
        }

        /// <summary>Finds a tenant membership for the requested user.</summary>
        public async Task<VersionedRecord<TenantMembership>?> FindTenantMembershipByUserAsync(Guid identityScopeId,
            ApplicationKey application, Guid tenantId, Guid userId, CancellationToken cancellationToken)
        {
            var route = await ResolveAsync(identityScopeId, application, cancellationToken);
            return await tenantMemberships.FindAsync(route,
                new TenantReference(identityScopeId, tenantId),
                new SubjectReference(identityScopeId, userId), cancellationToken);
        }

        /// <summary>Creates a tenant membership.</summary>
        public async Task<VersionedRecord<TenantMembership>> CreateTenantMembershipAsync(Guid identityScopeId,
            ApplicationKey application, Guid tenantId, Guid membershipId, Guid userId, MembershipStatus status,
            CancellationToken cancellationToken)
        {
            var route = await ResolveAsync(identityScopeId, application, cancellationToken);
            var value = new TenantMembership(membershipId,
                new TenantReference(identityScopeId, tenantId),
                new SubjectReference(identityScopeId, userId), status);
            var created = await tenantMemberships.CreateAsync(route, value, cancellationToken);
            await AuditAsync(route, SecurityAuditEventType.TenantMembershipCreated, identityScopeId, application,
                tenantId, userId, membershipId.ToString("D"), cancellationToken);
            return created;
        }

        /// <summary>Updates a tenant membership using optimistic concurrency.</summary>
        public async Task<VersionedRecord<TenantMembership>?> UpdateTenantMembershipAsync(Guid identityScopeId,
            ApplicationKey application, Guid tenantId, Guid membershipId, MembershipStatus status, long expectedVersion,
            CancellationToken cancellationToken)
        {
            var route = await ResolveAsync(identityScopeId, application, cancellationToken);
            var existing = await tenantMemberships.GetAsync(route, identityScopeId, membershipId, cancellationToken);
            if (existing is null || existing.Value.Tenant.TenantId != tenantId) return null;
            var value = new TenantMembership(membershipId, existing.Value.Tenant, existing.Value.Subject, status);
            var updated = await tenantMemberships.UpdateAsync(route, value, expectedVersion, cancellationToken);
            await AuditAsync(route, SecurityAuditEventType.TenantMembershipUpdated, identityScopeId, application,
                tenantId, existing.Value.Subject.UserId, membershipId.ToString("D"), cancellationToken);
            return updated;
        }

        /// <summary>Gets the requested user group.</summary>
        public async Task<VersionedRecord<UserGroup>?> GetGroupAsync(Guid identityScopeId, Guid tenantId,
            ApplicationKey application, Guid groupId, CancellationToken cancellationToken)
        {
            var route = await ResolveAsync(identityScopeId, application, cancellationToken);
            return await groups.GetAsync(route, Group(identityScopeId, tenantId, application, groupId), cancellationToken);
        }

        /// <summary>Creates a user group.</summary>
        public async Task<VersionedRecord<UserGroup>> CreateGroupAsync(Guid identityScopeId, Guid tenantId,
            ApplicationKey application, Guid groupId, string displayName, GroupStatus status,
            CancellationToken cancellationToken)
        {
            var route = await ResolveAsync(identityScopeId, application, cancellationToken);
            var value = new UserGroup(Group(identityScopeId, tenantId, application, groupId), displayName, status);
            var created = await groups.CreateAsync(route, value, cancellationToken);
            await AuditAsync(route, SecurityAuditEventType.GroupCreated, identityScopeId, application,
                tenantId, null, groupId.ToString("D"), cancellationToken);
            return created;
        }

        /// <summary>Updates a user group using optimistic concurrency.</summary>
        public async Task<VersionedRecord<UserGroup>> UpdateGroupAsync(Guid identityScopeId, Guid tenantId,
            ApplicationKey application, Guid groupId, string displayName, GroupStatus status, long expectedVersion,
            CancellationToken cancellationToken)
        {
            var route = await ResolveAsync(identityScopeId, application, cancellationToken);
            var value = new UserGroup(Group(identityScopeId, tenantId, application, groupId), displayName, status);
            var updated = await groups.UpdateAsync(route, value, expectedVersion, cancellationToken);
            await AuditAsync(route, SecurityAuditEventType.GroupUpdated, identityScopeId, application,
                tenantId, null, groupId.ToString("D"), cancellationToken);
            return updated;
        }

        /// <summary>Lists members of the requested user group.</summary>
        public async Task<IReadOnlyList<GroupMembership>> ListGroupMembersAsync(Guid identityScopeId, Guid tenantId,
            ApplicationKey application, Guid groupId, CancellationToken cancellationToken)
        {
            var route = await ResolveAsync(identityScopeId, application, cancellationToken);
            return await groupMemberships.ListAsync(route, Group(identityScopeId, tenantId, application, groupId),
                cancellationToken);
        }

        /// <summary>Adds a tenant member to the requested user group.</summary>
        public async Task<GroupMembership?> AddGroupMemberAsync(Guid identityScopeId, Guid tenantId,
            ApplicationKey application, Guid groupId, Guid tenantMembershipId, CancellationToken cancellationToken)
        {
            var route = await ResolveAsync(identityScopeId, application, cancellationToken);
            var added = await groupMembershipMutations.AddIfActiveAsync(
                route,
                Group(identityScopeId, tenantId, application, groupId),
                tenantMembershipId,
                cancellationToken);

            if (added is not null)
            {
                await AuditAsync(route, SecurityAuditEventType.GroupMemberAdded, identityScopeId, application,
                    tenantId, added.Subject.UserId, tenantMembershipId.ToString("D"), cancellationToken);
            }

            return added;
        }

        /// <summary>Removes a tenant member from the requested user group.</summary>
        public async Task<bool> RemoveGroupMemberAsync(Guid identityScopeId, Guid tenantId, ApplicationKey application,
            Guid groupId, Guid tenantMembershipId, CancellationToken cancellationToken)
        {
            var route = await ResolveAsync(identityScopeId, application, cancellationToken);
            var removed = await groupMemberships.RemoveAsync(
                route,
                Group(identityScopeId, tenantId, application, groupId),
                tenantMembershipId,
                cancellationToken);

            if (removed)
            {
                await AuditAsync(route, SecurityAuditEventType.GroupMemberRemoved, identityScopeId, application,
                    tenantId, null, tenantMembershipId.ToString("D"), cancellationToken);
            }

            return removed;
        }

        private Task<bool> AuditAsync(
            ResolvedDatabaseRoute route,
            SecurityAuditEventType eventType,
            Guid identityScopeId,
            ApplicationKey application,
            Guid? tenantId,
            Guid? userId,
            string targetId,
            CancellationToken cancellationToken) =>
            auditWriter.TryWriteAsync(
                route,
                new SecurityAuditEvent(
                    eventType,
                    SecurityAuditOutcome.Succeeded,
                    identityScopeId,
                    tenantId,
                    userId,
                    application,
                    targetId: targetId),
                cancellationToken);

        private ValueTask<ResolvedDatabaseRoute> ResolveAsync(Guid identityScopeId, ApplicationKey application,
            CancellationToken cancellationToken) =>
            routeResolver.ResolveAsync(new DatabaseRouteRequest(application, identityScopeId), cancellationToken);

        private static GroupReference Group(Guid identityScopeId, Guid tenantId, ApplicationKey application,
            Guid groupId) => new(new TenantReference(identityScopeId, tenantId), application, groupId);
    }
}
