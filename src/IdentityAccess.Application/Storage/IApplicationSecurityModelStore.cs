using IdentityAccess.Application.Routing;
using IdentityAccess.Domain;

namespace IdentityAccess.Application.Storage
{

    /// <summary>Persistence for immutable application security-model versions and declared capabilities.</summary>
    public interface IApplicationSecurityModelStore
    {
        /// <summary>Creates an application security model version.</summary>
        Task CreateModelAsync(ResolvedDatabaseRoute route, ApplicationSecurityModelReference model,
            CancellationToken cancellationToken);

        /// <summary>Adds a capability to an application security model version.</summary>
        Task AddCapabilityAsync(ResolvedDatabaseRoute route, ApplicationCapability capability,
            CancellationToken cancellationToken);

        /// <summary>Lists capabilities declared by an application security model version.</summary>
        Task<IReadOnlyList<ApplicationCapability>> ListCapabilitiesAsync(ResolvedDatabaseRoute route,
            ApplicationSecurityModelReference model, CancellationToken cancellationToken);
    }
}
