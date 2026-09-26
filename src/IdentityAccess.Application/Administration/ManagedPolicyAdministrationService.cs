using IdentityAccess.Application.Routing;
using IdentityAccess.Application.Security;
using IdentityAccess.Application.Storage;
using IdentityAccess.Domain;

namespace IdentityAccess.Application.Administration
{
    /// <summary>Coordinates application-scoped managed-policy catalog administration.</summary>
    public sealed class ManagedPolicyAdministrationService(
        IDatabaseRouteResolver routeResolver,
        IManagedPolicyStore policies,
        IManagedPolicyVersionStore versions,
        IManagedPolicyStatementStore statements,
        ISecurityAuditWriter auditWriter) : IManagedPolicyAdministrationService
    {
        public async Task<IReadOnlyList<VersionedRecord<ManagedPolicy>>> ListPoliciesAsync(
            Guid identityScopeId,
            ApplicationKey application,
            string? search,
            int offset,
            int limit,
            CancellationToken cancellationToken)
        {
            AdministrationPaging.EnsureValid(offset, limit);
            var normalizedSearch = AdministrationSearch.Normalize(search);
            var boundedLimit = AdministrationSearch.Limit(normalizedSearch, limit);
            var route = await ResolveAsync(identityScopeId, application, cancellationToken);
            return await policies.ListAsync(route, identityScopeId, application, normalizedSearch, offset, boundedLimit,
                cancellationToken);
        }

        public async Task<VersionedRecord<ManagedPolicy>?> GetPolicyAsync(
            Guid identityScopeId,
            ApplicationKey application,
            Guid policyId,
            CancellationToken cancellationToken)
        {
            var route = await ResolveAsync(identityScopeId, application, cancellationToken);
            return await policies.GetAsync(route, Policy(identityScopeId, application, policyId), cancellationToken);
        }

        public async Task<VersionedRecord<ManagedPolicy>> CreatePolicyAsync(
            Guid identityScopeId,
            ApplicationKey application,
            Guid policyId,
            ManagedPolicyKey policyKey,
            string displayName,
            PolicyStatus status,
            CancellationToken cancellationToken)
        {
            var route = await ResolveAsync(identityScopeId, application, cancellationToken);
            var policy = new ManagedPolicy(Policy(identityScopeId, application, policyId), policyKey, displayName, status);
            var created = await policies.CreateAsync(route, policy, cancellationToken);
            await AuditAsync(route, SecurityAuditEventType.ManagedPolicyCreated, identityScopeId, application,
                policyId.ToString("D"), cancellationToken);
            return created;
        }

        public async Task<VersionedRecord<ManagedPolicy>?> UpdatePolicyAsync(
            Guid identityScopeId,
            ApplicationKey application,
            Guid policyId,
            ManagedPolicyKey policyKey,
            string displayName,
            PolicyStatus status,
            long expectedVersion,
            CancellationToken cancellationToken)
        {
            var route = await ResolveAsync(identityScopeId, application, cancellationToken);
            var reference = Policy(identityScopeId, application, policyId);
            var current = await policies.GetAsync(route, reference, cancellationToken);
            if (current is null) return null;

            var updated = await policies.UpdateAsync(
                route,
                new ManagedPolicy(reference, policyKey, displayName, status, current.Value.DefaultVersion),
                expectedVersion,
                cancellationToken);
            await AuditAsync(route, SecurityAuditEventType.ManagedPolicyUpdated, identityScopeId, application,
                policyId.ToString("D"), cancellationToken);
            return updated;
        }

        public async Task<IReadOnlyList<ManagedPolicyVersion>> ListVersionsAsync(
            Guid identityScopeId,
            ApplicationKey application,
            Guid policyId,
            CancellationToken cancellationToken)
        {
            var route = await ResolveAsync(identityScopeId, application, cancellationToken);
            return await versions.ListAsync(route, Policy(identityScopeId, application, policyId), cancellationToken);
        }

        public async Task<ManagedPolicyVersion?> GetVersionAsync(
            Guid identityScopeId,
            ApplicationKey application,
            Guid policyId,
            int policyVersion,
            CancellationToken cancellationToken)
        {
            var route = await ResolveAsync(identityScopeId, application, cancellationToken);
            return await versions.GetAsync(route, Version(identityScopeId, application, policyId, policyVersion),
                cancellationToken);
        }

        public async Task<ManagedPolicyVersion?> CreateVersionAsync(
            Guid identityScopeId,
            ApplicationKey application,
            Guid policyId,
            int policyVersion,
            int modelVersion,
            CancellationToken cancellationToken)
        {
            var route = await ResolveAsync(identityScopeId, application, cancellationToken);
            var policy = Policy(identityScopeId, application, policyId);
            if (await policies.GetAsync(route, policy, cancellationToken) is null) return null;

            var version = new ManagedPolicyVersion(
                new ManagedPolicyVersionReference(policy, policyVersion),
                new ApplicationSecurityModelReference(identityScopeId, application, modelVersion));
            await versions.CreateAsync(route, version, cancellationToken);
            await AuditAsync(route, SecurityAuditEventType.ManagedPolicyVersionCreated, identityScopeId, application,
                $"{policyId:D}:v{policyVersion}", cancellationToken);
            return version;
        }

        public async Task<ManagedPolicyVersion?> PublishVersionAsync(
            Guid identityScopeId,
            ApplicationKey application,
            Guid policyId,
            int policyVersion,
            bool makeDefault,
            CancellationToken cancellationToken)
        {
            var route = await ResolveAsync(identityScopeId, application, cancellationToken);
            var reference = Version(identityScopeId, application, policyId, policyVersion);
            var published = await versions.PublishAsync(route, reference, cancellationToken);
            if (published is null) return null;

            if (makeDefault)
            {
                var current = await policies.GetAsync(route, reference.Policy, cancellationToken);
                if (current is null) return null;
                await policies.UpdateAsync(
                    route,
                    new ManagedPolicy(
                        current.Value.Reference,
                        current.Value.Key,
                        current.Value.DisplayName,
                        current.Value.Status,
                        policyVersion),
                    current.Version,
                    cancellationToken);
            }

            await AuditAsync(route, SecurityAuditEventType.ManagedPolicyVersionPublished, identityScopeId, application,
                $"{policyId:D}:v{policyVersion}", cancellationToken);
            return published;
        }

        public async Task<IReadOnlyList<ManagedPolicyStatement>> ListStatementsAsync(
            Guid identityScopeId,
            ApplicationKey application,
            Guid policyId,
            int policyVersion,
            CancellationToken cancellationToken)
        {
            var route = await ResolveAsync(identityScopeId, application, cancellationToken);
            var version = await versions.GetAsync(route, Version(identityScopeId, application, policyId, policyVersion),
                cancellationToken);
            return version is null
                ? Array.Empty<ManagedPolicyStatement>()
                : await statements.ListAsync(route, version, cancellationToken);
        }

        public async Task<ManagedPolicyStatement?> AddStatementAsync(
            Guid identityScopeId,
            ApplicationKey application,
            Guid policyId,
            int policyVersion,
            Guid statementId,
            CapabilityPattern pattern,
            CancellationToken cancellationToken)
        {
            var route = await ResolveAsync(identityScopeId, application, cancellationToken);
            var version = await versions.GetAsync(route, Version(identityScopeId, application, policyId, policyVersion),
                cancellationToken);
            if (version is null) return null;

            var statement = new ManagedPolicyStatement(statementId, version.Reference, version.Model, pattern);
            await statements.AddAsync(route, statement, cancellationToken);
            await AuditAsync(route, SecurityAuditEventType.ManagedPolicyStatementAdded, identityScopeId, application,
                statementId.ToString("D"), cancellationToken);
            return statement;
        }

        public async Task<bool> RemoveStatementAsync(
            Guid identityScopeId,
            ApplicationKey application,
            Guid policyId,
            int policyVersion,
            Guid statementId,
            CancellationToken cancellationToken)
        {
            var route = await ResolveAsync(identityScopeId, application, cancellationToken);
            var removed = await statements.RemoveAsync(
                route,
                Version(identityScopeId, application, policyId, policyVersion),
                statementId,
                cancellationToken);
            if (removed)
            {
                await AuditAsync(route, SecurityAuditEventType.ManagedPolicyStatementRemoved, identityScopeId,
                    application, statementId.ToString("D"), cancellationToken);
            }
            return removed;
        }

        private ValueTask<ResolvedDatabaseRoute> ResolveAsync(
            Guid identityScopeId,
            ApplicationKey application,
            CancellationToken cancellationToken) =>
            routeResolver.ResolveAsync(new DatabaseRouteRequest(application, identityScopeId), cancellationToken);

        private static ManagedPolicyReference Policy(Guid identityScopeId, ApplicationKey application, Guid policyId) =>
            new(identityScopeId, application, policyId);

        private static ManagedPolicyVersionReference Version(
            Guid identityScopeId,
            ApplicationKey application,
            Guid policyId,
            int policyVersion) =>
            new(Policy(identityScopeId, application, policyId), policyVersion);

        private Task AuditAsync(
            ResolvedDatabaseRoute route,
            SecurityAuditEventType eventType,
            Guid identityScopeId,
            ApplicationKey application,
            string targetId,
            CancellationToken cancellationToken) =>
            auditWriter.TryWriteAsync(
                route,
                new SecurityAuditEvent(
                    eventType,
                    SecurityAuditOutcome.Succeeded,
                    identityScopeId,
                    application: application,
                    targetId: targetId),
                cancellationToken);
    }
}
