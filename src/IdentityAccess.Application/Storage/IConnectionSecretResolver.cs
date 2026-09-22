using IdentityAccess.Application.Routing;

namespace IdentityAccess.Application.Storage
{

    /// <summary>Resolves a server-only secret reference into a connection string.</summary>
    public interface IConnectionSecretResolver
    {
        /// <summary>Resolves an opaque connection secret reference to connection material for server-side use.</summary>
        ValueTask<string> ResolveAsync(ConnectionSecretReference reference, CancellationToken cancellationToken);
    }
}
