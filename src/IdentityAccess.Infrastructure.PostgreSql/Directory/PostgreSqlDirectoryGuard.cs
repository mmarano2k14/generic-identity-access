using IdentityAccess.Application.Routing;

namespace IdentityAccess.Infrastructure.PostgreSql.Directory
{

    internal static class PostgreSqlDirectoryGuard
    {
        public static void EnsureScope(ResolvedDatabaseRoute route, Guid identityScopeId)
        {
            ArgumentNullException.ThrowIfNull(route);
            if (identityScopeId == Guid.Empty)
                throw new ArgumentException("Identity scope must not be empty.", nameof(identityScopeId));
            if (route.Request.IdentityScopeId != identityScopeId)
                throw new InvalidOperationException("The record identity scope does not match the resolved database route.");
        }

        public static void EnsureVersion(long expectedVersion)
        {
            if (expectedVersion <= 0)
                throw new ArgumentOutOfRangeException(nameof(expectedVersion));
        }
    }
}
