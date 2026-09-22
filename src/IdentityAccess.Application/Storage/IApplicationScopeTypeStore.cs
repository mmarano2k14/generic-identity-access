using IdentityAccess.Application.Routing;
using IdentityAccess.Domain;

namespace IdentityAccess.Application.Storage
{

    /// <summary>Defines the contract for application scope type store.</summary>
    public interface IApplicationScopeTypeStore
    {
        /// <summary>Adds a application scope type record to the resolved database route.</summary>
        Task AddAsync(ResolvedDatabaseRoute route, ApplicationScopeTypeDefinition definition,
            CancellationToken cancellationToken);

        /// <summary>Lists application scope type records for the supplied scope.</summary>
        Task<IReadOnlyList<ApplicationScopeTypeDefinition>> ListAsync(ResolvedDatabaseRoute route,
            ApplicationSecurityModelReference model, CancellationToken cancellationToken);
    }
}
