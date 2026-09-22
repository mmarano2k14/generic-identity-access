

namespace IdentityAccess.Infrastructure.PostgreSql
{

    /// <summary>Defines configuration options for PostgreSQL storage.</summary>
    internal sealed record PostgreSqlStorageOptions
    {
        /// <summary>Defines the default minimum pool size constant.</summary>
        public const int DefaultMinimumPoolSize = 0;
        /// <summary>Defines the default maximum pool size constant.</summary>
        public const int DefaultMaximumPoolSize = 20;
        /// <summary>Defines the default connection timeout seconds constant.</summary>
        public const int DefaultConnectionTimeoutSeconds = 10;
        /// <summary>Defines the default command timeout seconds constant.</summary>
        public const int DefaultCommandTimeoutSeconds = 30;

        /// <summary>Gets or initializes the minimum pool size.</summary>
        public int MinimumPoolSize { get; init; } = DefaultMinimumPoolSize;
        /// <summary>Gets or initializes the maximum pool size.</summary>
        public int MaximumPoolSize { get; init; } = DefaultMaximumPoolSize;
        /// <summary>Gets or initializes the connection timeout seconds.</summary>
        public int ConnectionTimeoutSeconds { get; init; } = DefaultConnectionTimeoutSeconds;
        /// <summary>Gets or initializes the command timeout seconds.</summary>
        public int CommandTimeoutSeconds { get; init; } = DefaultCommandTimeoutSeconds;

        /// <summary>Validates the current value and throws when it violates the contract.</summary>
        public void Validate()
        {
            if (MinimumPoolSize < 0 || MinimumPoolSize > 1000)
                throw new PostgreSqlStorageException(PostgreSqlStorageFailure.InvalidConfiguration);
            if (MaximumPoolSize is < 1 or > 1000 || MaximumPoolSize < MinimumPoolSize)
                throw new PostgreSqlStorageException(PostgreSqlStorageFailure.InvalidConfiguration);
            if (ConnectionTimeoutSeconds is < 1 or > 300)
                throw new PostgreSqlStorageException(PostgreSqlStorageFailure.InvalidConfiguration);
            if (CommandTimeoutSeconds is < 1 or > 3600)
                throw new PostgreSqlStorageException(PostgreSqlStorageFailure.InvalidConfiguration);
        }
    }
}
