using IdentityAccess.Application.Routing;
using IdentityAccess.Application.Security;
using IdentityAccess.Application.Storage;
using IdentityAccess.Domain;

namespace IdentityAccess.Application.Administration
{
    /// <summary>Provides application operations for identity-scope authority administration.</summary>
    public sealed class IdentityScopeAuthorityAdministrationService(
        IDatabaseRouteResolver routeResolver,
        IIdentityScopeAdministrationGroupStore groups,
        IIdentityScopeAdministrationMembershipStore memberships,
        IIdentityScopeAdministrationPolicyStore policies,
        IIdentityScopeAdministrationPolicyStatementStore statements,
        IIdentityScopeAdministrationBindingStore bindings,
        ISecurityAuditWriter auditWriter)
        : IIdentityScopeAuthorityAdministrationService
    {
        /// <inheritdoc />
        public async Task<VersionedRecord<IdentityScopeAdministrationGroup>?> GetGroupAsync(
            Guid identityScopeId, ApplicationKey application, Guid groupId, CancellationToken cancellationToken)
        {
            var route = await ResolveAsync(identityScopeId, application, cancellationToken);
            return await groups.GetAsync(route, Group(identityScopeId, application, groupId), cancellationToken);
        }

        /// <inheritdoc />
        public async Task<IReadOnlyList<VersionedRecord<IdentityScopeAdministrationGroup>>> ListGroupsAsync(
            Guid identityScopeId, ApplicationKey application, string? search, int offset, int limit,
            CancellationToken cancellationToken)
        {
            AdministrationPaging.EnsureValid(offset, limit);
            var normalizedSearch = AdministrationSearch.Normalize(search);
            var boundedLimit = AdministrationSearch.Limit(normalizedSearch, limit);
            var route = await ResolveAsync(identityScopeId, application, cancellationToken);
            return await groups.ListAsync(route, identityScopeId, application, normalizedSearch, offset,
                boundedLimit, cancellationToken);
        }

        /// <inheritdoc />
        public async Task<VersionedRecord<IdentityScopeAdministrationGroup>> CreateGroupAsync(
            Guid identityScopeId, ApplicationKey application, Guid groupId, string displayName,
            GroupStatus status, CancellationToken cancellationToken)
        {
            var route = await ResolveAsync(identityScopeId, application, cancellationToken);
            var value = new IdentityScopeAdministrationGroup(
                Group(identityScopeId, application, groupId), displayName, status);
            var created = await groups.CreateAsync(route, value, cancellationToken);
            await AuditAsync(route, SecurityAuditEventType.IdentityScopeAuthorityGroupCreated,
                identityScopeId, application, groupId.ToString("D"), cancellationToken);
            return created;
        }

        /// <inheritdoc />
        public async Task<VersionedRecord<IdentityScopeAdministrationGroup>> UpdateGroupAsync(
            Guid identityScopeId, ApplicationKey application, Guid groupId, string displayName,
            GroupStatus status, long expectedVersion, CancellationToken cancellationToken)
        {
            var route = await ResolveAsync(identityScopeId, application, cancellationToken);
            var value = new IdentityScopeAdministrationGroup(
                Group(identityScopeId, application, groupId), displayName, status);
            var updated = await groups.UpdateAsync(route, value, expectedVersion, cancellationToken);
            await AuditAsync(route, SecurityAuditEventType.IdentityScopeAuthorityGroupUpdated,
                identityScopeId, application, groupId.ToString("D"), cancellationToken);
            return updated;
        }

        /// <inheritdoc />
        public async Task<IReadOnlyList<IdentityScopeAdministrationGroupMembership>> ListMembersAsync(
            Guid identityScopeId, ApplicationKey application, Guid groupId, CancellationToken cancellationToken)
        {
            var route = await ResolveAsync(identityScopeId, application, cancellationToken);
            return await memberships.ListAsync(
                route, Group(identityScopeId, application, groupId), cancellationToken);
        }

        /// <inheritdoc />
        public async Task<IdentityScopeAdministrationGroupMembership?> AddMemberAsync(
            Guid identityScopeId, ApplicationKey application, Guid groupId, Guid userId,
            CancellationToken cancellationToken)
        {
            var route = await ResolveAsync(identityScopeId, application, cancellationToken);
            var value = new IdentityScopeAdministrationGroupMembership(
                Group(identityScopeId, application, groupId),
                new SubjectReference(identityScopeId, userId));

            var added = await memberships.AddIfActiveAsync(route, value, cancellationToken);
            if (added is not null)
            {
                await AuditAsync(route, SecurityAuditEventType.IdentityScopeAuthorityMemberAdded,
                    identityScopeId, application, $"{groupId:D}:{userId:D}", cancellationToken);
            }

            return added;
        }

        /// <inheritdoc />
        public async Task<bool> RemoveMemberAsync(
            Guid identityScopeId, ApplicationKey application, Guid groupId, Guid userId,
            CancellationToken cancellationToken)
        {
            var route = await ResolveAsync(identityScopeId, application, cancellationToken);
            var value = new IdentityScopeAdministrationGroupMembership(
                Group(identityScopeId, application, groupId),
                new SubjectReference(identityScopeId, userId));

            var removed = await memberships.RemoveAsync(route, value, cancellationToken);
            if (removed)
            {
                await AuditAsync(route, SecurityAuditEventType.IdentityScopeAuthorityMemberRemoved,
                    identityScopeId, application, $"{groupId:D}:{userId:D}", cancellationToken);
            }

            return removed;
        }

        /// <inheritdoc />
        public async Task<VersionedRecord<IdentityScopeAdministrationPolicy>?> GetPolicyAsync(
            Guid identityScopeId, ApplicationKey application, Guid policyId, CancellationToken cancellationToken)
        {
            var route = await ResolveAsync(identityScopeId, application, cancellationToken);
            return await policies.GetAsync(route, Policy(identityScopeId, application, policyId), cancellationToken);
        }

        /// <inheritdoc />
        public async Task<IReadOnlyList<VersionedRecord<IdentityScopeAdministrationPolicy>>> ListPoliciesAsync(
            Guid identityScopeId, ApplicationKey application, string? search, int offset, int limit,
            CancellationToken cancellationToken)
        {
            AdministrationPaging.EnsureValid(offset, limit);
            var normalizedSearch = AdministrationSearch.Normalize(search);
            var boundedLimit = AdministrationSearch.Limit(normalizedSearch, limit);
            var route = await ResolveAsync(identityScopeId, application, cancellationToken);
            return await policies.ListAsync(route, identityScopeId, application, normalizedSearch, offset,
                boundedLimit, cancellationToken);
        }

        /// <inheritdoc />
        public async Task<VersionedRecord<IdentityScopeAdministrationPolicy>> CreatePolicyAsync(
            Guid identityScopeId, ApplicationKey application, Guid policyId, string displayName,
            PolicyStatus status, CancellationToken cancellationToken)
        {
            var route = await ResolveAsync(identityScopeId, application, cancellationToken);
            var value = new IdentityScopeAdministrationPolicy(
                Policy(identityScopeId, application, policyId), displayName, status);
            var created = await policies.CreateAsync(route, value, cancellationToken);
            await AuditAsync(route, SecurityAuditEventType.IdentityScopeAuthorityPolicyCreated,
                identityScopeId, application, policyId.ToString("D"), cancellationToken);
            return created;
        }

        /// <inheritdoc />
        public async Task<VersionedRecord<IdentityScopeAdministrationPolicy>> UpdatePolicyAsync(
            Guid identityScopeId, ApplicationKey application, Guid policyId, string displayName,
            PolicyStatus status, long expectedVersion, CancellationToken cancellationToken)
        {
            var route = await ResolveAsync(identityScopeId, application, cancellationToken);
            var value = new IdentityScopeAdministrationPolicy(
                Policy(identityScopeId, application, policyId), displayName, status);
            var updated = await policies.UpdateAsync(route, value, expectedVersion, cancellationToken);
            await AuditAsync(route, SecurityAuditEventType.IdentityScopeAuthorityPolicyUpdated,
                identityScopeId, application, policyId.ToString("D"), cancellationToken);
            return updated;
        }

        /// <inheritdoc />
        public async Task<IReadOnlyList<IdentityScopeAdministrationPolicyStatement>> ListStatementsAsync(
            Guid identityScopeId, ApplicationKey application, Guid policyId, CancellationToken cancellationToken)
        {
            var route = await ResolveAsync(identityScopeId, application, cancellationToken);
            return await statements.ListAsync(
                route, Policy(identityScopeId, application, policyId), cancellationToken);
        }

        /// <inheritdoc />
        public async Task<IdentityScopeAdministrationPolicyStatement> AddStatementAsync(
            Guid identityScopeId, ApplicationKey application, Guid policyId, Guid statementId,
            int modelVersion, CapabilityPattern pattern, CancellationToken cancellationToken)
        {
            var route = await ResolveAsync(identityScopeId, application, cancellationToken);
            var statement = new IdentityScopeAdministrationPolicyStatement(
                statementId,
                Policy(identityScopeId, application, policyId),
                new ApplicationSecurityModelReference(identityScopeId, application, modelVersion),
                pattern);

            await statements.AddAsync(route, statement, cancellationToken);
            await AuditAsync(route, SecurityAuditEventType.IdentityScopeAuthorityStatementAdded,
                identityScopeId, application, statementId.ToString("D"), cancellationToken);
            return statement;
        }

        /// <inheritdoc />
        public async Task<bool> RemoveStatementAsync(
            Guid identityScopeId, ApplicationKey application, Guid policyId, Guid statementId,
            CancellationToken cancellationToken)
        {
            var route = await ResolveAsync(identityScopeId, application, cancellationToken);
            var removed = await statements.RemoveAsync(
                route, Policy(identityScopeId, application, policyId), statementId, cancellationToken);

            if (removed)
            {
                await AuditAsync(route, SecurityAuditEventType.IdentityScopeAuthorityStatementRemoved,
                    identityScopeId, application, statementId.ToString("D"), cancellationToken);
            }

            return removed;
        }

        /// <inheritdoc />
        public async Task<IReadOnlyList<IdentityScopeAdministrationGroupPolicyBinding>> ListBindingsAsync(
            Guid identityScopeId, ApplicationKey application, Guid groupId, CancellationToken cancellationToken)
        {
            var route = await ResolveAsync(identityScopeId, application, cancellationToken);
            return await bindings.ListAsync(
                route, Group(identityScopeId, application, groupId), cancellationToken);
        }

        /// <inheritdoc />
        public async Task<IdentityScopeAdministrationGroupPolicyBinding?> AddBindingAsync(
            Guid identityScopeId, ApplicationKey application, Guid groupId, Guid policyId,
            CancellationToken cancellationToken)
        {
            var route = await ResolveAsync(identityScopeId, application, cancellationToken);
            var value = new IdentityScopeAdministrationGroupPolicyBinding(
                Group(identityScopeId, application, groupId),
                Policy(identityScopeId, application, policyId));

            var added = await bindings.AddIfActiveAsync(route, value, cancellationToken);
            if (added is not null)
            {
                await AuditAsync(route, SecurityAuditEventType.IdentityScopeAuthorityBindingAdded,
                    identityScopeId, application, $"{groupId:D}:{policyId:D}", cancellationToken);
            }

            return added;
        }

        /// <inheritdoc />
        public async Task<bool> RemoveBindingAsync(
            Guid identityScopeId, ApplicationKey application, Guid groupId, Guid policyId,
            CancellationToken cancellationToken)
        {
            var route = await ResolveAsync(identityScopeId, application, cancellationToken);
            var value = new IdentityScopeAdministrationGroupPolicyBinding(
                Group(identityScopeId, application, groupId),
                Policy(identityScopeId, application, policyId));

            var removed = await bindings.RemoveAsync(route, value, cancellationToken);
            if (removed)
            {
                await AuditAsync(route, SecurityAuditEventType.IdentityScopeAuthorityBindingRemoved,
                    identityScopeId, application, $"{groupId:D}:{policyId:D}", cancellationToken);
            }

            return removed;
        }

        private ValueTask<ResolvedDatabaseRoute> ResolveAsync(
            Guid identityScopeId, ApplicationKey application, CancellationToken cancellationToken) =>
            routeResolver.ResolveAsync(
                new DatabaseRouteRequest(application, identityScopeId), cancellationToken);

        private Task<bool> AuditAsync(
            ResolvedDatabaseRoute route, SecurityAuditEventType eventType, Guid identityScopeId,
            ApplicationKey application, string targetId, CancellationToken cancellationToken) =>
            auditWriter.TryWriteAsync(
                route,
                new SecurityAuditEvent(
                    eventType,
                    SecurityAuditOutcome.Succeeded,
                    identityScopeId,
                    application: application,
                    targetId: targetId),
                cancellationToken);

        private static IdentityScopeAdministrationGroupReference Group(
            Guid identityScopeId, ApplicationKey application, Guid groupId) =>
            new(identityScopeId, application, groupId);

        private static IdentityScopeAdministrationPolicyReference Policy(
            Guid identityScopeId, ApplicationKey application, Guid policyId) =>
            new(identityScopeId, application, policyId);
    }
}
