using IdentityAccess.Application.Routing;
using IdentityAccess.Application.Storage;

namespace IdentityAccess.Tests.PostgreSql
{

    internal sealed class MustNotOpenConnectionFactory : IIdentityDatabaseConnectionFactory
    {
        public ValueTask<System.Data.Common.DbConnection> OpenAsync(
            ResolvedDatabaseRoute route,
            CancellationToken cancellationToken) =>
            throw new InvalidOperationException("Connection must not be opened for an invalid scope.");
    }
}
