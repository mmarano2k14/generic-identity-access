

namespace IdentityAccess.Infrastructure.PostgreSql
{

    /// <summary>Represents a failure raised by PostgreSQL storage.</summary>
    public sealed class PostgreSqlStorageException : Exception
    {
        /// <summary>Gets the failure.</summary>
        public PostgreSqlStorageFailure Failure { get; }

        /// <summary>Initializes a new instance of <see cref="PostgreSqlStorageException"/>.</summary>
        public PostgreSqlStorageException(PostgreSqlStorageFailure failure)
            : base($"PostgreSQL storage operation failed ({failure}).")
        {
            Failure = failure;
        }

        /// <summary>Initializes a new instance of <see cref="PostgreSqlStorageException"/>.</summary>
        public PostgreSqlStorageException(PostgreSqlStorageFailure failure, Exception innerException)
            : base($"PostgreSQL storage operation failed ({failure}).", innerException)
        {
            Failure = failure;
        }
    }
}
