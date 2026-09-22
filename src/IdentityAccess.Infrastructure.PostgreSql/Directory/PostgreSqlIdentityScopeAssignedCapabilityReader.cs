using IdentityAccess.Application.Authorization;
using IdentityAccess.Application.Routing;
using IdentityAccess.Application.Storage;
using IdentityAccess.Domain;
using Npgsql;

namespace IdentityAccess.Infrastructure.PostgreSql.Directory
{
    /// <summary>
    /// Projects identity-scope administration grants from active users, authority groups, policies,
    /// and policy statements. Wildcard evaluation remains external.
    /// </summary>
    internal sealed class PostgreSqlIdentityScopeAssignedCapabilityReader(
        IIdentityDatabaseConnectionFactory connectionFactory)
        : IIdentityScopeAssignedCapabilityReader
    {
        /// <inheritdoc />
        public async Task<IReadOnlyList<AssignedIdentityScopeCapabilityGrant>> ListAsync(
            ResolvedDatabaseRoute route,
            Guid identityScopeId,
            SubjectReference subject,
            ApplicationKey application,
            CancellationToken cancellationToken)
        {
            if (identityScopeId == Guid.Empty)
                throw new ArgumentException("An identity scope is required.", nameof(identityScopeId));

            ArgumentNullException.ThrowIfNull(subject);
            ArgumentNullException.ThrowIfNull(application);

            PostgreSqlDirectoryGuard.EnsureScope(
                route,
                identityScopeId);

            if (subject.IdentityScopeId != identityScopeId)
            {
                throw new InvalidOperationException(
                    "The subject must belong to the requested identity scope.");
            }

            if (route.Request.Application != application)
            {
                throw new InvalidOperationException(
                    "The application must match the resolved database route.");
            }

            await using var connection = (NpgsqlConnection)await connectionFactory
                .OpenAsync(route, cancellationToken)
                .ConfigureAwait(false);

            await using var command = new NpgsqlCommand("""
                SELECT
                    g.group_id,
                    p.policy_id,
                    s.statement_id,
                    s.model_version,
                    s.capability_resource,
                    s.capability_feature,
                    s.capability_action
                FROM identity_access.users AS u
                JOIN identity_access.identity_scope_administration_group_memberships AS gm
                  ON gm.identity_scope_id = u.identity_scope_id
                 AND gm.user_id = u.user_id
                 AND gm.application_key = @application_key
                JOIN identity_access.identity_scope_administration_groups AS g
                  ON g.identity_scope_id = gm.identity_scope_id
                 AND g.application_key = gm.application_key
                 AND g.group_id = gm.group_id
                JOIN identity_access.identity_scope_administration_group_policy_bindings AS b
                  ON b.identity_scope_id = g.identity_scope_id
                 AND b.application_key = g.application_key
                 AND b.group_id = g.group_id
                JOIN identity_access.identity_scope_administration_policies AS p
                  ON p.identity_scope_id = b.identity_scope_id
                 AND p.application_key = b.application_key
                 AND p.policy_id = b.policy_id
                JOIN identity_access.identity_scope_administration_policy_statements AS s
                  ON s.identity_scope_id = p.identity_scope_id
                 AND s.application_key = p.application_key
                 AND s.policy_id = p.policy_id
                WHERE u.identity_scope_id = @scope
                  AND u.user_id = @user_id
                  AND u.status = 1
                  AND g.status = 1
                  AND p.status = 1
                ORDER BY g.group_id, p.policy_id, s.statement_id;
                """, connection);

            command.Parameters.AddWithValue(
                "scope",
                identityScopeId);
            command.Parameters.AddWithValue(
                "user_id",
                subject.UserId);
            command.Parameters.AddWithValue(
                "application_key",
                application.Value);

            var grants = new List<AssignedIdentityScopeCapabilityGrant>();

            await using var reader = await command
                .ExecuteReaderAsync(cancellationToken)
                .ConfigureAwait(false);

            while (await reader
                .ReadAsync(cancellationToken)
                .ConfigureAwait(false))
            {
                grants.Add(
                    new AssignedIdentityScopeCapabilityGrant(
                        identityScopeId,
                        subject,
                        application,
                        reader.GetGuid(0),
                        reader.GetGuid(1),
                        reader.GetGuid(2),
                        new ApplicationSecurityModelReference(
                            identityScopeId,
                            application,
                            reader.GetInt32(3)),
                        new CapabilityPattern(
                            reader.GetString(4),
                            reader.GetString(5),
                            reader.GetString(6))));
            }

            return grants.AsReadOnly();
        }
    }
}
