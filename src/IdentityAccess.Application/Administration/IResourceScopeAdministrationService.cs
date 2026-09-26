using IdentityAccess.Application.Storage;
using IdentityAccess.Domain;

namespace IdentityAccess.Application.Administration
{

    /// <summary>Defines the contract for resource scope administration service.</summary>
    public interface IResourceScopeAdministrationService
    {
        /// <summary>Lists resource scope types declared by the application security model.</summary>
        Task<IReadOnlyList<ApplicationScopeTypeDefinition>> ListTypesAsync(Guid identityScopeId,
            ApplicationKey application, int modelVersion, CancellationToken cancellationToken);

        /// <summary>Adds a resource scope type to the application security model.</summary>
        Task<ApplicationScopeTypeDefinition> AddTypeAsync(Guid identityScopeId, ApplicationKey application,
            int modelVersion, ResourceScopeTypeKey type, string displayName, ResourceScopeTypeKey? parentType,
            bool canAttachToTenant, CancellationToken cancellationToken);

        /// <summary>Gets the requested resource scope.</summary>
        Task<VersionedRecord<ResourceScope>?> GetAsync(Guid identityScopeId, Guid tenantId,
            ApplicationKey application, Guid resourceScopeId, CancellationToken cancellationToken);

        /// <summary>Lists resource scopes for the requested tenant and application.</summary>
        Task<IReadOnlyList<VersionedRecord<ResourceScope>>> ListAsync(Guid identityScopeId, Guid tenantId,
            ApplicationKey application, string? search, int offset, int limit, CancellationToken cancellationToken);

        /// <summary>Creates a resource scope.</summary>
        Task<VersionedRecord<ResourceScope>> CreateAsync(Guid identityScopeId, Guid tenantId,
            ApplicationKey application, Guid resourceScopeId, int modelVersion, ResourceScopeTypeKey type,
            string externalResourceId, string displayName, Guid? parentResourceScopeId, ResourceScopeStatus status,
            CancellationToken cancellationToken);

        /// <summary>Updates a resource scope using optimistic concurrency.</summary>
        Task<VersionedRecord<ResourceScope>> UpdateAsync(Guid identityScopeId, Guid tenantId,
            ApplicationKey application, Guid resourceScopeId, int modelVersion, ResourceScopeTypeKey type,
            string externalResourceId, string displayName, Guid? parentResourceScopeId, ResourceScopeStatus status,
            long expectedVersion, CancellationToken cancellationToken);
    }
}
