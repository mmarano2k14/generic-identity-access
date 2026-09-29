using IdentityAccess.Application.Routing;
using IdentityAccess.Application.Storage;

namespace IdentityAccess.Infrastructure.PostgreSql.Secrets
{

    /// <summary>
    /// Resolves only env: references. Secret values never appear in exception messages or ToString output.
    /// </summary>
    internal sealed class EnvironmentConnectionSecretResolver : IConnectionSecretResolver
    {
        /// <summary>Resolves a supported environment-backed connection secret reference.</summary>
        public ValueTask<string> ResolveAsync(ConnectionSecretReference reference,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ArgumentNullException.ThrowIfNull(reference);

            var value = reference.Value;
            var separator = value.IndexOf(':');
            if (separator <= 0 || !string.Equals(value[..separator], "env", StringComparison.Ordinal))
                throw new PostgreSqlStorageException(PostgreSqlStorageFailure.UnsupportedSecretScheme);

            var variableName = value[(separator + 1)..];
            var secret = Environment.GetEnvironmentVariable(variableName);
            if (string.IsNullOrWhiteSpace(secret))
                throw new PostgreSqlStorageException(PostgreSqlStorageFailure.SecretNotFound);

            return ValueTask.FromResult(secret);
        }
    }
}
