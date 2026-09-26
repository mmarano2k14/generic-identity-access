using IdentityAccess.Application.Routing;
using IdentityAccess.Application.Security;
using IdentityAccess.Application.Storage;
using IdentityAccess.Domain;

namespace IdentityAccess.Application.Administration
{

    /// <summary>Provides application operations for policy administration.</summary>
    public sealed class PolicyAdministrationService(
        IDatabaseRouteResolver routeResolver,
        IPermissionPolicyStore policies,
        IPolicyStatementStore statements,
        IGroupPolicyBindingStore bindings,
        IGroupPolicyBindingMutationStore bindingMutations,
        ISecurityAuditWriter auditWriter) : IPolicyAdministrationService
    {
        /// <summary>Gets the requested permission policy.</summary>
        public async Task<VersionedRecord<PermissionPolicy>?> GetPolicyAsync(Guid identityScopeId, Guid tenantId,
            ApplicationKey application, Guid policyId, CancellationToken cancellationToken)
        {
            var route = await ResolveAsync(identityScopeId, application, cancellationToken);
            return await policies.GetAsync(route, Policy(identityScopeId, tenantId, application, policyId),
                cancellationToken);
        }

        /// <summary>Lists permission policies in a bounded deterministic window.</summary>
        public async Task<IReadOnlyList<VersionedRecord<PermissionPolicy>>> ListPoliciesAsync(Guid identityScopeId,
            Guid tenantId, ApplicationKey application, string? search, int offset, int limit, CancellationToken cancellationToken)
        {
            AdministrationPaging.EnsureValid(offset, limit);
            var normalizedSearch = AdministrationSearch.Normalize(search);
            var boundedLimit = AdministrationSearch.Limit(normalizedSearch, limit);
            var route = await ResolveAsync(identityScopeId, application, cancellationToken);
            return await policies.ListAsync(route, new TenantReference(identityScopeId, tenantId), application,
                normalizedSearch, offset, boundedLimit, cancellationToken);
        }

        /// <summary>Creates a permission policy.</summary>
        public async Task<VersionedRecord<PermissionPolicy>> CreatePolicyAsync(Guid identityScopeId, Guid tenantId,
            ApplicationKey application, Guid policyId, string displayName, PolicyStatus status,
            CancellationToken cancellationToken)
        {
            var route = await ResolveAsync(identityScopeId, application, cancellationToken);
            var value = new PermissionPolicy(Policy(identityScopeId, tenantId, application, policyId), displayName, status);
            var created = await policies.CreateAsync(route, value, cancellationToken);
            await AuditAsync(route, SecurityAuditEventType.PolicyCreated, identityScopeId, tenantId,
                application, policyId.ToString("D"), cancellationToken);
            return created;
        }

        /// <summary>Updates a permission policy using optimistic concurrency.</summary>
        public async Task<VersionedRecord<PermissionPolicy>> UpdatePolicyAsync(Guid identityScopeId, Guid tenantId,
            ApplicationKey application, Guid policyId, string displayName, PolicyStatus status, long expectedVersion,
            CancellationToken cancellationToken)
        {
            var route = await ResolveAsync(identityScopeId, application, cancellationToken);
            var value = new PermissionPolicy(Policy(identityScopeId, tenantId, application, policyId), displayName, status);
            var updated = await policies.UpdateAsync(route, value, expectedVersion, cancellationToken);
            await AuditAsync(route, SecurityAuditEventType.PolicyUpdated, identityScopeId, tenantId,
                application, policyId.ToString("D"), cancellationToken);
            return updated;
        }

        /// <summary>Lists statements belonging to the requested permission policy.</summary>
        public async Task<IReadOnlyList<PolicyStatement>> ListStatementsAsync(Guid identityScopeId, Guid tenantId,
            ApplicationKey application, Guid policyId, CancellationToken cancellationToken)
        {
            var route = await ResolveAsync(identityScopeId, application, cancellationToken);
            return await statements.ListAsync(route, Policy(identityScopeId, tenantId, application, policyId),
                cancellationToken);
        }

        /// <summary>Adds a capability statement to the requested permission policy.</summary>
        public async Task<PolicyStatement> AddStatementAsync(Guid identityScopeId, Guid tenantId,
            ApplicationKey application, Guid policyId, Guid statementId, int modelVersion, CapabilityPattern pattern,
            CancellationToken cancellationToken)
        {
            var route = await ResolveAsync(identityScopeId, application, cancellationToken);
            var policy = Policy(identityScopeId, tenantId, application, policyId);
            var statement = new PolicyStatement(statementId, policy,
                new ApplicationSecurityModelReference(identityScopeId, application, modelVersion), pattern);
            await statements.AddAsync(route, statement, cancellationToken);
            await AuditAsync(route, SecurityAuditEventType.PolicyStatementAdded, identityScopeId, tenantId,
                application, statementId.ToString("D"), cancellationToken);
            return statement;
        }

        /// <summary>Removes a capability statement from the requested permission policy.</summary>
        public async Task<bool> RemoveStatementAsync(Guid identityScopeId, Guid tenantId, ApplicationKey application,
            Guid policyId, Guid statementId, CancellationToken cancellationToken)
        {
            var route = await ResolveAsync(identityScopeId, application, cancellationToken);
            var removed = await statements.RemoveAsync(
                route,
                Policy(identityScopeId, tenantId, application, policyId),
                statementId,
                cancellationToken);

            if (removed)
            {
                await AuditAsync(route, SecurityAuditEventType.PolicyStatementRemoved, identityScopeId, tenantId,
                    application, statementId.ToString("D"), cancellationToken);
            }

            return removed;
        }

        /// <summary>Lists policy bindings for the requested user group.</summary>
        public async Task<IReadOnlyList<GroupPolicyBinding>> ListBindingsAsync(Guid identityScopeId, Guid tenantId,
            ApplicationKey application, Guid groupId, CancellationToken cancellationToken)
        {
            var route = await ResolveAsync(identityScopeId, application, cancellationToken);
            return await bindings.ListAsync(route, Group(identityScopeId, tenantId, application, groupId),
                cancellationToken);
        }

        /// <summary>Adds a policy binding to the requested user group.</summary>
        public Task<GroupPolicyBinding?> AddBindingAsync(Guid identityScopeId, Guid tenantId,
            ApplicationKey application, Guid groupId, Guid policyId, CancellationToken cancellationToken) =>
            AddBindingAsync(identityScopeId, tenantId, application, groupId, policyId, null, false, cancellationToken);

        /// <summary>Adds a policy binding to the requested user group.</summary>
        public async Task<GroupPolicyBinding?> AddBindingAsync(Guid identityScopeId, Guid tenantId,
            ApplicationKey application, Guid groupId, Guid policyId, Guid? resourceScopeId, bool includeDescendants,
            CancellationToken cancellationToken)
        {
            var route = await ResolveAsync(identityScopeId, application, cancellationToken);
            var group = Group(identityScopeId, tenantId, application, groupId);
            var policy = Policy(identityScopeId, tenantId, application, policyId);
            var scope = resourceScopeId is null
                ? null
                : new ResourceScopeReference(group.Tenant, application, resourceScopeId.Value);

            var binding = GroupPolicyBinding.Restore(group, policy, scope, includeDescendants);
            var added = await bindingMutations.AddIfActiveAsync(route, binding, cancellationToken);

            if (added)
            {
                await AuditAsync(route, SecurityAuditEventType.PolicyBindingAdded, identityScopeId, tenantId,
                    application, BindingTarget(groupId, policyId, resourceScopeId), cancellationToken);
                return binding;
            }

            return null;
        }

        /// <summary>Removes a policy binding from the requested user group.</summary>
        public Task<bool> RemoveBindingAsync(Guid identityScopeId, Guid tenantId, ApplicationKey application,
            Guid groupId, Guid policyId, CancellationToken cancellationToken) =>
            RemoveBindingAsync(identityScopeId, tenantId, application, groupId, policyId, null, cancellationToken);

        /// <summary>Removes a policy binding from the requested user group.</summary>
        public async Task<bool> RemoveBindingAsync(Guid identityScopeId, Guid tenantId, ApplicationKey application,
            Guid groupId, Guid policyId, Guid? resourceScopeId, CancellationToken cancellationToken)
        {
            var route = await ResolveAsync(identityScopeId, application, cancellationToken);
            var group = Group(identityScopeId, tenantId, application, groupId);
            ResourceScopeReference? scope = resourceScopeId is null
                ? null
                : new ResourceScopeReference(group.Tenant, application, resourceScopeId.Value);
            var binding = GroupPolicyBinding.Restore(group,
                Policy(identityScopeId, tenantId, application, policyId), scope, false);
            var removed = await bindings.RemoveAsync(route, binding, cancellationToken);

            if (removed)
            {
                await AuditAsync(route, SecurityAuditEventType.PolicyBindingRemoved, identityScopeId, tenantId,
                    application, BindingTarget(groupId, policyId, resourceScopeId), cancellationToken);
            }

            return removed;
        }

        private Task<bool> AuditAsync(
            ResolvedDatabaseRoute route,
            SecurityAuditEventType eventType,
            Guid identityScopeId,
            Guid tenantId,
            ApplicationKey application,
            string targetId,
            CancellationToken cancellationToken) =>
            auditWriter.TryWriteAsync(
                route,
                new SecurityAuditEvent(
                    eventType,
                    SecurityAuditOutcome.Succeeded,
                    identityScopeId,
                    tenantId,
                    application: application,
                    targetId: targetId),
                cancellationToken);

        private static string BindingTarget(Guid groupId, Guid policyId, Guid? resourceScopeId) =>
            resourceScopeId is null
                ? $"{groupId:D}:{policyId:D}:tenant"
                : $"{groupId:D}:{policyId:D}:{resourceScopeId.Value:D}";

        private ValueTask<ResolvedDatabaseRoute> ResolveAsync(Guid identityScopeId, ApplicationKey application,
            CancellationToken cancellationToken) =>
            routeResolver.ResolveAsync(new DatabaseRouteRequest(application, identityScopeId), cancellationToken);

        private static PermissionPolicyReference Policy(Guid identityScopeId, Guid tenantId,
            ApplicationKey application, Guid policyId) =>
            new(new TenantReference(identityScopeId, tenantId), application, policyId);

        private static GroupReference Group(Guid identityScopeId, Guid tenantId, ApplicationKey application,
            Guid groupId) => new(new TenantReference(identityScopeId, tenantId), application, groupId);
    }
}
