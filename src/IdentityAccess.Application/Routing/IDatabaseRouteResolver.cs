

namespace IdentityAccess.Application.Routing
{

    /// <summary>One authoritative provider resolves registered placement, without fallback or authorization.</summary>
    public interface IDatabaseRouteResolver
    {
        /// <summary>Resolves the immutable database route for the requested logical operation.</summary>
        ValueTask<ResolvedDatabaseRoute> ResolveAsync(DatabaseRouteRequest request,
            CancellationToken cancellationToken);
    }
}
