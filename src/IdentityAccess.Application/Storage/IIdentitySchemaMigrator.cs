using IdentityAccess.Application.Routing;

namespace IdentityAccess.Application.Storage
{

    /// <summary>Applies owned identity schema migrations to one already-resolved destination.</summary>
    public interface IIdentitySchemaMigrator
    {
        /// <summary>Applies pending identity schema migrations to the resolved database destination.</summary>
        Task MigrateAsync(ResolvedDatabaseRoute route, CancellationToken cancellationToken);
    }
}
