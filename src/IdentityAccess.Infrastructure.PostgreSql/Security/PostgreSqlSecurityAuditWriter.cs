using System.Diagnostics;
using IdentityAccess.Application.Routing;
using IdentityAccess.Application.Security;
using IdentityAccess.Application.Storage;
using Microsoft.Extensions.Logging;
using Npgsql;
using NpgsqlTypes;

namespace IdentityAccess.Infrastructure.PostgreSql.Security
{
    /// <summary>Persists secret-safe security audit events in PostgreSQL.</summary>
    internal sealed class PostgreSqlSecurityAuditWriter(
        IIdentityDatabaseConnectionFactory connectionFactory,
        ILogger<PostgreSqlSecurityAuditWriter> logger) : ISecurityAuditWriter
    {
        /// <inheritdoc />
        public async Task<bool> TryWriteAsync(
            ResolvedDatabaseRoute route,
            SecurityAuditEvent auditEvent,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(route);
            ArgumentNullException.ThrowIfNull(auditEvent);

            if (route.Request.IdentityScopeId != auditEvent.IdentityScopeId)
                throw new ArgumentException(
                    "Audit event identity scope must match the resolved route.",
                    nameof(auditEvent));

            if (auditEvent.Application is not null &&
                auditEvent.Application != route.Request.Application)
                throw new ArgumentException(
                    "Audit event application must match the resolved route.",
                    nameof(auditEvent));

            try
            {
                await using var connection = (NpgsqlConnection)await connectionFactory
                    .OpenAsync(route, cancellationToken)
                    .ConfigureAwait(false);

                await using var command = new NpgsqlCommand("""
                    INSERT INTO identity_access.security_events
                    (
                        event_id, event_type, outcome, identity_scope_id, tenant_id, user_id,
                        application_key, client_id, target_id, reason_code, correlation_id
                    )
                    VALUES
                    (
                        @event_id, @event_type, @outcome, @identity_scope_id, @tenant_id, @user_id,
                        @application_key, @client_id, @target_id, @reason_code, @correlation_id
                    );
                    """, connection);

                command.Parameters.AddWithValue("event_id", Guid.NewGuid());
                command.Parameters.AddWithValue("event_type", auditEvent.EventType.ToString());
                command.Parameters.AddWithValue("outcome", auditEvent.Outcome.ToString());
                command.Parameters.AddWithValue("identity_scope_id", auditEvent.IdentityScopeId);
                AddNullable(command, "tenant_id", NpgsqlDbType.Uuid, auditEvent.TenantId);
                AddNullable(command, "user_id", NpgsqlDbType.Uuid, auditEvent.UserId);
                AddNullable(command, "application_key", NpgsqlDbType.Varchar, auditEvent.Application?.Value);
                AddNullable(command, "client_id", NpgsqlDbType.Varchar, auditEvent.ClientId);
                AddNullable(command, "target_id", NpgsqlDbType.Varchar, auditEvent.TargetId);
                AddNullable(command, "reason_code", NpgsqlDbType.Varchar, auditEvent.ReasonCode?.ToString());
                AddNullable(command, "correlation_id", NpgsqlDbType.Varchar, Activity.Current?.TraceId.ToString());

                await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
                return true;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception exception)
            {
                logger.LogError(
                    exception,
                    "Security audit persistence failed for event type {AuditEventType}.",
                    auditEvent.EventType);
                return false;
            }
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
