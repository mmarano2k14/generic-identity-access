using IdentityAccess.Application.Routing;
using IdentityAccess.Application.Security;
using IdentityAccess.Application.Storage;
using IdentityAccess.Domain;

namespace IdentityAccess.Application.Administration
{
    /// <summary>Coordinates tenant-scoped bindings to reusable managed-policy versions.</summary>
    public sealed class ManagedPolicyBindingAdministrationService(
        IDatabaseRouteResolver routeResolver,
        IManagedPolicyStore policies,
        IManagedGroupPolicyBindingStore bindings,
        IManagedGroupPolicyBindingMutationStore bindingMutations,
        ISecurityAuditWriter auditWriter) : IManagedPolicyBindingAdministrationService
    {
        public async Task<IReadOnlyList<VersionedRecord<ManagedPolicy>>> ListAvailablePoliciesAsync(
            Guid identityScopeId,
            Guid tenantId,
            ApplicationKey application,
            string? search,
            int offset,
            int limit,
            CancellationToken cancellationToken)
        {
            _ = Tenant(identityScopeId, tenantId);
            AdministrationPaging.EnsureValid(offset, limit);
            var normalizedSearch = AdministrationSearch.Normalize(search);
            var boundedLimit = AdministrationSearch.Limit(normalizedSearch, limit);
            var route = await ResolveAsync(identityScopeId, application, cancellationToken);
            return await policies.ListAttachableAsync(
                route,
                identityScopeId,
                application,
                normalizedSearch,
                offset,
                boundedLimit,
                cancellationToken);
        }

        public async Task<IReadOnlyList<ManagedGroupPolicyBinding>> ListBindingsAsync(
            Guid identityScopeId,
            Guid tenantId,
            ApplicationKey application,
            Guid groupId,
            CancellationToken cancellationToken)
        {
            var route = await ResolveAsync(identityScopeId, application, cancellationToken);
            return await bindings.ListAsync(route, Group(identityScopeId, tenantId, application, groupId), cancellationToken);
        }

        public async Task<ManagedGroupPolicyBinding?> AddBindingAsync(
            Guid identityScopeId,
            Guid tenantId,
            ApplicationKey application,
            Guid groupId,
            Guid policyId,
            int? policyVersion,
            Guid? resourceScopeId,
            bool includeDescendants,
            CancellationToken cancellationToken)
        {
            var route = await ResolveAsync(identityScopeId, application, cancellationToken);
            var policyReference = new ManagedPolicyReference(identityScopeId, application, policyId);
            var policy = await policies.GetAsync(route, policyReference, cancellationToken);
            if (policy is null || policy.Value.Status != PolicyStatus.Active) return null;

            var selectedVersion = policyVersion ?? policy.Value.DefaultVersion;
            if (selectedVersion is null) return null;

            var group = Group(identityScopeId, tenantId, application, groupId);
            var scope = resourceScopeId is null
                ? null
                : new ResourceScopeReference(group.Tenant, application, resourceScopeId.Value);
            var binding = ManagedGroupPolicyBinding.Restore(
                group,
                new ManagedPolicyVersionReference(policyReference, selectedVersion.Value),
                scope,
                includeDescendants);

            var added = await bindingMutations.AddIfActiveAsync(route, binding, cancellationToken);
            if (!added) return null;

            await AuditAsync(
                route,
                SecurityAuditEventType.ManagedPolicyBindingAdded,
                identityScopeId,
                tenantId,
                application,
                BindingTarget(groupId, policyId, selectedVersion.Value, resourceScopeId),
                cancellationToken);
            return binding;
        }

        public async Task<bool> RemoveBindingAsync(
            Guid identityScopeId,
            Guid tenantId,
            ApplicationKey application,
            Guid groupId,
            Guid policyId,
            int policyVersion,
            Guid? resourceScopeId,
            CancellationToken cancellationToken)
        {
            if (policyVersion <= 0) throw new ArgumentOutOfRangeException(nameof(policyVersion));
            var route = await ResolveAsync(identityScopeId, application, cancellationToken);
            var group = Group(identityScopeId, tenantId, application, groupId);
            var scope = resourceScopeId is null
                ? null
                : new ResourceScopeReference(group.Tenant, application, resourceScopeId.Value);
            var binding = ManagedGroupPolicyBinding.Restore(
                group,
                new ManagedPolicyVersionReference(
                    new ManagedPolicyReference(identityScopeId, application, policyId),
                    policyVersion),
                scope,
                false);
            var removed = await bindings.RemoveAsync(route, binding, cancellationToken);
            if (!removed) return false;

            await AuditAsync(
                route,
                SecurityAuditEventType.ManagedPolicyBindingRemoved,
                identityScopeId,
                tenantId,
                application,
                BindingTarget(groupId, policyId, policyVersion, resourceScopeId),
                cancellationToken);
            return true;
        }

        private ValueTask<ResolvedDatabaseRoute> ResolveAsync(
            Guid identityScopeId,
            ApplicationKey application,
            CancellationToken cancellationToken) =>
            routeResolver.ResolveAsync(new DatabaseRouteRequest(application, identityScopeId), cancellationToken);

        private Task AuditAsync(
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

        private static TenantReference Tenant(Guid identityScopeId, Guid tenantId) =>
            new(identityScopeId, tenantId);

        private static GroupReference Group(
            Guid identityScopeId,
            Guid tenantId,
            ApplicationKey application,
            Guid groupId) =>
            new(Tenant(identityScopeId, tenantId), application, groupId);

        private static string BindingTarget(
            Guid groupId,
            Guid policyId,
            int policyVersion,
            Guid? resourceScopeId) =>
            resourceScopeId is null
                ? $"{groupId:D}:{policyId:D}:v{policyVersion}:tenant"
                : $"{groupId:D}:{policyId:D}:v{policyVersion}:{resourceScopeId.Value:D}";
    }
}
