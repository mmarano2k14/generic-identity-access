using IdentityAccess.Application.Routing;
using IdentityAccess.Application.Security;
using IdentityAccess.Application.Storage;
using IdentityAccess.Domain;
using IdentityAccess.Infrastructure.PostgreSql.Directory;
using Npgsql;
using NpgsqlTypes;

namespace IdentityAccess.Infrastructure.PostgreSql.Security
{
    /// <summary>Reads secret-safe security audit records from PostgreSQL.</summary>
    internal sealed class PostgreSqlSecurityAuditReader(IIdentityDatabaseConnectionFactory connectionFactory)
        : ISecurityAuditReader
    {
        /// <inheritdoc />
        public async Task<IReadOnlyList<SecurityAuditRecord>> ListAsync(
            ResolvedDatabaseRoute route,
            ApplicationKey application,
            SecurityAuditQuery query,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(route);
            ArgumentNullException.ThrowIfNull(application);
            ArgumentNullException.ThrowIfNull(query);
            PostgreSqlDirectoryGuard.EnsureScope(route, route.Request.IdentityScopeId);

            await using var connection = (NpgsqlConnection)await connectionFactory
                .OpenAsync(route, cancellationToken)
                .ConfigureAwait(false);

            await using var command = new NpgsqlCommand("""
                SELECT event_id, occurred_at, event_type, outcome, identity_scope_id,
                       tenant_id, user_id, application_key, client_id, target_id,
                       reason_code, correlation_id
                FROM identity_access.security_events
                WHERE identity_scope_id = @scope
                  AND application_key = @application_key
                  AND (@tenant_id IS NULL OR tenant_id = @tenant_id)
                  AND (@user_id IS NULL OR user_id = @user_id)
                  AND (@event_type IS NULL OR event_type = @event_type)
                  AND (@outcome IS NULL OR outcome = @outcome)
                  AND (@correlation_id IS NULL OR correlation_id = @correlation_id)
                ORDER BY occurred_at DESC, event_id DESC
                OFFSET @offset
                LIMIT @limit;
                """, connection);

            command.Parameters.AddWithValue("scope", route.Request.IdentityScopeId);
            command.Parameters.AddWithValue("application_key", application.Value);
            AddNullable(command, "tenant_id", NpgsqlDbType.Uuid, query.TenantId);
            AddNullable(command, "user_id", NpgsqlDbType.Uuid, query.UserId);
            AddNullable(command, "event_type", NpgsqlDbType.Varchar, query.EventType?.ToString());
            AddNullable(command, "outcome", NpgsqlDbType.Varchar, query.Outcome?.ToString());
            AddNullable(command, "correlation_id", NpgsqlDbType.Varchar, query.CorrelationId);
            command.Parameters.AddWithValue("offset", query.Offset);
            command.Parameters.AddWithValue("limit", query.Limit);

            var result = new List<SecurityAuditRecord>();
            await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                result.Add(Read(reader));
            }

            return result.AsReadOnly();
        }

        private static SecurityAuditRecord Read(NpgsqlDataReader reader)
        {
            var eventType = ParseEnum<SecurityAuditEventType>(reader.GetString(2), "event_type");
            var outcome = ParseEnum<SecurityAuditOutcome>(reader.GetString(3), "outcome");
            var application = reader.IsDBNull(7) ? null : new ApplicationKey(reader.GetString(7));
            SecurityAuditReasonCode? reasonCode = reader.IsDBNull(10)
                ? null
                : ParseEnum<SecurityAuditReasonCode>(reader.GetString(10), "reason_code");

            return new SecurityAuditRecord(
                reader.GetGuid(0),
                reader.GetFieldValue<DateTimeOffset>(1),
                eventType,
                outcome,
                reader.GetGuid(4),
                reader.IsDBNull(5) ? null : reader.GetGuid(5),
                reader.IsDBNull(6) ? null : reader.GetGuid(6),
                application,
                reader.IsDBNull(8) ? null : reader.GetString(8),
                reader.IsDBNull(9) ? null : reader.GetString(9),
                reasonCode,
                reader.IsDBNull(11) ? null : reader.GetString(11));
        }

        private static TEnum ParseEnum<TEnum>(string value, string column)
            where TEnum : struct, Enum
        {
            if (Enum.TryParse<TEnum>(value, ignoreCase: false, out var parsed)) return parsed;
            throw new InvalidOperationException($"Persisted security audit column '{column}' contains an unsupported category.");
        }

        private static void AddNullable(
            NpgsqlCommand command,
            string parameterName,
            NpgsqlDbType type,
            object? value)
        {
            var parameter = command.Parameters.Add(parameterName, type);
            parameter.Value = value ?? DBNull.Value;
        }
    }
}
