using IdentityAccess.Application.Routing;
using IdentityAccess.Domain;

namespace IdentityAccess.Application.Storage
{
    /// <summary>Persistence for atomic registration and reading of application security manifests.</summary>
    public interface IApplicationSecurityCatalogStore
    {
        /// <summary>Lists manifest-backed security models for one application.</summary>
        Task<IReadOnlyList<RegisteredApplicationSecurityModel>> ListAsync(
            ResolvedDatabaseRoute route,
            Guid identityScopeId,
            ApplicationKey application,
            CancellationToken cancellationToken);

        /// <summary>Gets one manifest-backed application security model.</summary>
        Task<RegisteredApplicationSecurityModel?> GetAsync(
            ResolvedDatabaseRoute route,
            ApplicationSecurityModelReference model,
            CancellationToken cancellationToken);

        /// <summary>
        /// Registers the complete model atomically. Re-registering the same fingerprint is idempotent;
        /// reusing a version for different semantics is a conflict.
        /// </summary>
        Task<RegisteredApplicationSecurityModel> RegisterAsync(
            ResolvedDatabaseRoute route,
            RegisteredApplicationSecurityModel model,
            CancellationToken cancellationToken);
    }
}
