using IdentityAccess.Application.Routing;
using IdentityAccess.Application.Security;
using IdentityAccess.Application.Storage;
using IdentityAccess.Domain;

namespace IdentityAccess.Application.Administration
{

    /// <summary>Provides application operations for resource scope administration.</summary>
    public sealed class ResourceScopeAdministrationService(
        IDatabaseRouteResolver routeResolver,
        IApplicationScopeTypeStore scopeTypes,
        IResourceScopeStore resourceScopes,
        ISecurityAuditWriter auditWriter) : IResourceScopeAdministrationService
    {
        /// <summary>Lists resource scope types declared by the application security model.</summary>
        public async Task<IReadOnlyList<ApplicationScopeTypeDefinition>> ListTypesAsync(Guid identityScopeId,
            ApplicationKey application, int modelVersion, CancellationToken cancellationToken)
        {
            var route = await ResolveAsync(identityScopeId, application, cancellationToken);
            return await scopeTypes.ListAsync(route,
                new ApplicationSecurityModelReference(identityScopeId, application, modelVersion), cancellationToken);
        }

        /// <summary>Adds a resource scope type to the application security model.</summary>
        public async Task<ApplicationScopeTypeDefinition> AddTypeAsync(Guid identityScopeId, ApplicationKey application,
            int modelVersion, ResourceScopeTypeKey type, string displayName, ResourceScopeTypeKey? parentType,
            bool canAttachToTenant, CancellationToken cancellationToken)
        {
            var route = await ResolveAsync(identityScopeId, application, cancellationToken);
            var definition = new ApplicationScopeTypeDefinition(
                new ApplicationSecurityModelReference(identityScopeId, application, modelVersion),
                type, displayName, parentType, canAttachToTenant);
            await scopeTypes.AddAsync(route, definition, cancellationToken);
            await auditWriter.TryWriteAsync(
                route,
                new SecurityAuditEvent(
                    SecurityAuditEventType.ScopeTypeAdded,
                    SecurityAuditOutcome.Succeeded,
                    identityScopeId,
                    application: application,
                    targetId: type.Value),
                cancellationToken);
            return definition;
        }

        /// <summary>Gets the requested resource scope.</summary>
        public async Task<VersionedRecord<ResourceScope>?> GetAsync(Guid identityScopeId, Guid tenantId,
            ApplicationKey application, Guid resourceScopeId, CancellationToken cancellationToken)
        {
            var route = await ResolveAsync(identityScopeId, application, cancellationToken);
            return await resourceScopes.GetAsync(route, Reference(identityScopeId, tenantId, application, resourceScopeId),
                cancellationToken);
        }

        /// <summary>Lists resource scopes for the requested tenant and application.</summary>
        public async Task<IReadOnlyList<VersionedRecord<ResourceScope>>> ListAsync(Guid identityScopeId, Guid tenantId,
            ApplicationKey application, string? search, int offset, int limit, CancellationToken cancellationToken)
        {
            AdministrationPaging.EnsureValid(offset, limit);
            var normalizedSearch = AdministrationSearch.Normalize(search);
            var boundedLimit = AdministrationSearch.Limit(normalizedSearch, limit);
            var route = await ResolveAsync(identityScopeId, application, cancellationToken);
            return await resourceScopes.ListAsync(route, new TenantReference(identityScopeId, tenantId), application,
                normalizedSearch, offset, boundedLimit, cancellationToken);
        }

        /// <summary>Creates a resource scope.</summary>
        public async Task<VersionedRecord<ResourceScope>> CreateAsync(Guid identityScopeId, Guid tenantId,
            ApplicationKey application, Guid resourceScopeId, int modelVersion, ResourceScopeTypeKey type,
            string externalResourceId, string displayName, Guid? parentResourceScopeId, ResourceScopeStatus status,
            CancellationToken cancellationToken)
        {
            var route = await ResolveAsync(identityScopeId, application, cancellationToken);
            var value = Value(identityScopeId, tenantId, application, resourceScopeId, modelVersion, type,
                externalResourceId, displayName, parentResourceScopeId, status);
            var created = await resourceScopes.CreateAsync(route, value, cancellationToken);
            await auditWriter.TryWriteAsync(
                route,
                new SecurityAuditEvent(
                    SecurityAuditEventType.ResourceScopeCreated,
                    SecurityAuditOutcome.Succeeded,
                    identityScopeId,
                    tenantId,
                    application: application,
                    targetId: resourceScopeId.ToString("D")),
                cancellationToken);
            return created;
        }

        /// <summary>Updates a resource scope using optimistic concurrency.</summary>
        public async Task<VersionedRecord<ResourceScope>> UpdateAsync(Guid identityScopeId, Guid tenantId,
            ApplicationKey application, Guid resourceScopeId, int modelVersion, ResourceScopeTypeKey type,
            string externalResourceId, string displayName, Guid? parentResourceScopeId, ResourceScopeStatus status,
            long expectedVersion, CancellationToken cancellationToken)
        {
            var route = await ResolveAsync(identityScopeId, application, cancellationToken);
            var value = Value(identityScopeId, tenantId, application, resourceScopeId, modelVersion, type,
                externalResourceId, displayName, parentResourceScopeId, status);
            var updated = await resourceScopes.UpdateAsync(route, value, expectedVersion, cancellationToken);
            await auditWriter.TryWriteAsync(
                route,
                new SecurityAuditEvent(
                    SecurityAuditEventType.ResourceScopeUpdated,
                    SecurityAuditOutcome.Succeeded,
                    identityScopeId,
                    tenantId,
                    application: application,
                    targetId: resourceScopeId.ToString("D")),
                cancellationToken);
            return updated;
        }

        private ValueTask<ResolvedDatabaseRoute> ResolveAsync(Guid identityScopeId, ApplicationKey application,
            CancellationToken cancellationToken) =>
            routeResolver.ResolveAsync(new DatabaseRouteRequest(application, identityScopeId), cancellationToken);

        private static ResourceScopeReference Reference(Guid scope, Guid tenantId, ApplicationKey application, Guid id) =>
            new(new TenantReference(scope, tenantId), application, id);

        private static ResourceScope Value(Guid scope, Guid tenantId, ApplicationKey application, Guid id,
            int modelVersion, ResourceScopeTypeKey type, string externalId, string displayName,
            Guid? parentId, ResourceScopeStatus status) =>
            new(Reference(scope, tenantId, application, id), modelVersion, type, externalId, displayName, parentId, status);
    }
}
