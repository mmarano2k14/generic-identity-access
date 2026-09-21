namespace IdentityAccess.Application.Routing;

/// <summary>One authoritative provider resolves registered placement, without fallback or authorization.</summary>
public interface IDatabaseRouteResolver
{
    ValueTask<ResolvedDatabaseRoute> ResolveAsync(DatabaseRouteRequest request,
        CancellationToken cancellationToken = default);
}
