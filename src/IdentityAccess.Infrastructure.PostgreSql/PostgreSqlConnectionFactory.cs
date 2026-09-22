using System.Collections.Concurrent;
using System.Data.Common;
using System.Diagnostics;
using IdentityAccess.Application.Routing;
using IdentityAccess.Application.Security;
using IdentityAccess.Application.Storage;
using Npgsql;

namespace IdentityAccess.Infrastructure.PostgreSql
{
    /// <summary>
    /// Maintains one bounded Npgsql data source per registered destination. A destination cannot be
    /// silently rebound to another secret reference during the process lifetime.
    /// </summary>
    internal sealed class PostgreSqlConnectionFactory :
        IIdentityDatabaseConnectionFactory,
        IAsyncDisposable
    {
        private readonly IConnectionSecretResolver secretResolver;
        private readonly PostgreSqlStorageOptions options;
        private readonly ConcurrentDictionary<string, Lazy<DataSourceRegistration>> dataSources =
            new(StringComparer.Ordinal);
        private int disposed;

        /// <summary>Initializes a new connection factory.</summary>
        public PostgreSqlConnectionFactory(
            IConnectionSecretResolver secretResolver,
            PostgreSqlStorageOptions options)
        {
            this.secretResolver =
                secretResolver ??
                throw new ArgumentNullException(nameof(secretResolver));

            this.options =
                options ??
                throw new ArgumentNullException(nameof(options));

            this.options.Validate();
        }

        /// <summary>Gets the registered destination count.</summary>
        public int RegisteredDestinationCount =>
            dataSources.Count;

        /// <inheritdoc />
        public async ValueTask<DbConnection> OpenAsync(
            ResolvedDatabaseRoute route,
            CancellationToken cancellationToken)
        {
            ObjectDisposedException.ThrowIf(
                Volatile.Read(ref disposed) != 0,
                this);

            ArgumentNullException.ThrowIfNull(route);
            cancellationToken.ThrowIfCancellationRequested();

            if (!string.Equals(
                    route.Provider,
                    "postgresql",
                    StringComparison.Ordinal))
            {
                throw new PostgreSqlStorageException(
                    PostgreSqlStorageFailure.InvalidConfiguration);
            }

            var registration = await GetOrCreateRegistrationAsync(
                route,
                cancellationToken).ConfigureAwait(false);

            return await OpenExistingAsync(
                registration,
                route,
                cancellationToken).ConfigureAwait(false);
        }


        private async ValueTask<DataSourceRegistration> GetOrCreateRegistrationAsync(
            ResolvedDatabaseRoute route,
            CancellationToken cancellationToken)
        {
            if (dataSources.TryGetValue(
                    route.DestinationKey,
                    out var existing))
            {
                return MaterializeRegistration(
                    route.DestinationKey,
                    existing);
            }

            var connectionString = await secretResolver
                .ResolveAsync(
                    route.ConnectionSecretReference,
                    cancellationToken)
                .ConfigureAwait(false);

            var candidate =
                new Lazy<DataSourceRegistration>(
                    () => CreateRegistration(
                        route,
                        connectionString),
                    LazyThreadSafetyMode.ExecutionAndPublication);

            var selected = dataSources.GetOrAdd(
                route.DestinationKey,
                candidate);

            return MaterializeRegistration(
                route.DestinationKey,
                selected);
        }

        private DataSourceRegistration MaterializeRegistration(
            string destinationKey,
            Lazy<DataSourceRegistration> registration)
        {
            try
            {
                return registration.Value;
            }
            catch
            {
                if (dataSources.TryGetValue(
                        destinationKey,
                        out var current) &&
                    ReferenceEquals(
                        current,
                        registration))
                {
                    dataSources.TryRemove(
                        destinationKey,
                        out _);
                }

                throw;
            }
        }

        private async ValueTask<DbConnection> OpenExistingAsync(
            DataSourceRegistration registration,
            ResolvedDatabaseRoute route,
            CancellationToken cancellationToken)
        {
            if (!string.Equals(
                    registration.SecretReference,
                    route.ConnectionSecretReference.Value,
                    StringComparison.Ordinal))
            {
                throw new PostgreSqlStorageException(
                    PostgreSqlStorageFailure.DestinationDefinitionChanged);
            }

            NpgsqlConnection? connection = null;

            try
            {
                connection = await registration.DataSource
                    .OpenConnectionAsync(cancellationToken)
                    .ConfigureAwait(false);

                await ApplyAuditSessionContextAsync(
                    connection,
                    cancellationToken).ConfigureAwait(false);

                return connection;
            }
            catch (OperationCanceledException)
                when (cancellationToken.IsCancellationRequested)
            {
                if (connection is not null)
                {
                    await connection.DisposeAsync().ConfigureAwait(false);
                }

                throw;
            }
            catch (NpgsqlException error)
            {
                if (connection is not null)
                {
                    await connection.DisposeAsync().ConfigureAwait(false);
                }

                throw new PostgreSqlStorageException(
                    PostgreSqlStorageFailure.ConnectionFailed,
                    error);
            }
        }

        private DataSourceRegistration CreateRegistration(
            ResolvedDatabaseRoute route,
            string rawConnectionString)
        {
            try
            {
                var connection =
                    new NpgsqlConnectionStringBuilder(
                        rawConnectionString)
                    {
                        Pooling = true,
                        MinPoolSize = options.MinimumPoolSize,
                        MaxPoolSize = options.MaximumPoolSize,
                        Timeout = options.ConnectionTimeoutSeconds,
                        CommandTimeout = options.CommandTimeoutSeconds
                    };

                if (string.IsNullOrWhiteSpace(
                        connection.ApplicationName))
                {
                    connection.ApplicationName =
                        "generic-identity-access";
                }

                var dataSource =
                    NpgsqlDataSource.Create(
                        connection.ConnectionString);

                return new DataSourceRegistration(
                    route.ConnectionSecretReference.Value,
                    dataSource);
            }
            catch (ArgumentException)
            {
                throw new PostgreSqlStorageException(
                    PostgreSqlStorageFailure.InvalidConnectionString);
            }
        }

        private static async Task ApplyAuditSessionContextAsync(
            NpgsqlConnection connection,
            CancellationToken cancellationToken)
        {
            var activity = Activity.Current;

            var correlationId =
                Tag(
                    activity,
                    SecurityAuditActivityTagNames.CorrelationId);

            if (string.IsNullOrWhiteSpace(correlationId) &&
                activity is not null)
            {
                correlationId =
                    activity.TraceId.ToString();
            }

            await using var command =
                new NpgsqlCommand(
                    """
                    SELECT
                        set_config('identity_access.correlation_id', @correlation_id, false),
                        set_config('identity_access.actor_identity_scope_id', @actor_scope_id, false),
                        set_config('identity_access.actor_user_id', @actor_user_id, false),
                        set_config('identity_access.actor_session_id', @actor_session_id, false),
                        set_config('identity_access.actor_client_id', @actor_client_id, false),
                        set_config('identity_access.actor_application_key', @actor_application_key, false),
                        set_config('identity_access.authentication_context_key', @authentication_context_key, false);
                    """,
                    connection);

            command.Parameters.AddWithValue(
                "correlation_id",
                correlationId ?? string.Empty);

            command.Parameters.AddWithValue(
                "actor_scope_id",
                Tag(
                    activity,
                    SecurityAuditActivityTagNames.ActorIdentityScopeId));

            command.Parameters.AddWithValue(
                "actor_user_id",
                Tag(
                    activity,
                    SecurityAuditActivityTagNames.ActorUserId));

            command.Parameters.AddWithValue(
                "actor_session_id",
                Tag(
                    activity,
                    SecurityAuditActivityTagNames.ActorSessionId));

            command.Parameters.AddWithValue(
                "actor_client_id",
                Tag(
                    activity,
                    SecurityAuditActivityTagNames.ActorClientId));

            command.Parameters.AddWithValue(
                "actor_application_key",
                Tag(
                    activity,
                    SecurityAuditActivityTagNames.ActorApplicationKey));

            command.Parameters.AddWithValue(
                "authentication_context_key",
                Tag(
                    activity,
                    SecurityAuditActivityTagNames.AuthenticationContextKey));

            await command
                .ExecuteNonQueryAsync(cancellationToken)
                .ConfigureAwait(false);
        }

        private static string Tag(
            Activity? activity,
            string name) =>
            activity?
                .GetTagItem(name)?
                .ToString() ??
            string.Empty;

        /// <inheritdoc />
        public async ValueTask DisposeAsync()
        {
            if (Interlocked.Exchange(
                    ref disposed,
                    1) != 0)
            {
                return;
            }

            foreach (var entry in dataSources.Values)
            {
                if (entry.IsValueCreated)
                {
                    await entry.Value.DataSource
                        .DisposeAsync()
                        .ConfigureAwait(false);
                }
            }

            dataSources.Clear();
        }
    }
}
