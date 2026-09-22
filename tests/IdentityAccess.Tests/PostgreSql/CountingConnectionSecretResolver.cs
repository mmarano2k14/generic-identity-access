using IdentityAccess.Application.Routing;
using IdentityAccess.Application.Storage;

namespace IdentityAccess.Tests.PostgreSql
{
    internal sealed class CountingConnectionSecretResolver : IConnectionSecretResolver
    {
        private readonly string secret;
        private int resolveCount;

        public CountingConnectionSecretResolver(string secret)
        {
            this.secret = secret;
        }

        public int ResolveCount => Volatile.Read(ref resolveCount);

        public ValueTask<string> ResolveAsync(
            ConnectionSecretReference reference,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(reference);
            cancellationToken.ThrowIfCancellationRequested();
            Interlocked.Increment(ref resolveCount);
            return ValueTask.FromResult(secret);
        }
    }
}
