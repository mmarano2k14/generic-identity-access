

namespace IdentityAccess.Infrastructure.PostgreSql
{

    /// <summary>Defines failure codes for PostgreSQL storage.</summary>
    public enum PostgreSqlStorageFailure
    {
        /// <summary>Indicates the unsupported secret scheme failure condition.</summary>
        UnsupportedSecretScheme,
        /// <summary>Indicates the secret not found failure condition.</summary>
        SecretNotFound,
        /// <summary>Indicates the invalid connection string failure condition.</summary>
        InvalidConnectionString,
        /// <summary>Indicates the invalid configuration failure condition.</summary>
        InvalidConfiguration,
        /// <summary>Indicates the destination definition changed failure condition.</summary>
        DestinationDefinitionChanged,
        /// <summary>Indicates the connection failed failure condition.</summary>
        ConnectionFailed,
        /// <summary>Indicates that persisted migration metadata no longer matches the embedded migration set.</summary>
        MigrationIntegrityViolation
    }
}
