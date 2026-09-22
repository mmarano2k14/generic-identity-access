using System.Data.Common;
using IdentityAccess.Application.Routing;

namespace IdentityAccess.Application.Storage
{

    /// <summary>
    /// Opens a connection for an already resolved, immutable route snapshot.
    /// Callers must keep the same route for the complete logical operation.
    /// </summary>
    public interface IIdentityDatabaseConnectionFactory
    {
        /// <summary>Opens a database connection for the supplied resolved route.</summary>
        ValueTask<DbConnection> OpenAsync(ResolvedDatabaseRoute route, CancellationToken cancellationToken);
    }
}
