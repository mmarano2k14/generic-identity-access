using IdentityAccess.Application.Routing;
using IdentityAccess.Application.Storage;
using IdentityAccess.Domain;
using Npgsql;

namespace IdentityAccess.Infrastructure.PostgreSql.Directory
{

    /// <summary>Provides persistence operations for PostgreSQL tenant membership.</summary>
    internal sealed class PostgreSqlTenantMembershipStore(IIdentityDatabaseConnectionFactory connectionFactory)
        : ITenantMembershipStore
    {
        /// <summary>Gets the requested tenant membership record from the resolved database route.</summary>
        public async Task<VersionedRecord<TenantMembership>?> GetAsync(ResolvedDatabaseRoute route, Guid identityScopeId,
            Guid membershipId, CancellationToken cancellationToken)
        {
            PostgreSqlDirectoryGuard.EnsureScope(route, identityScopeId);
            if (membershipId == Guid.Empty) throw new ArgumentException("Membership id must not be empty.", nameof(membershipId));
            return await ReadAsync(route, "membership_id = @membership_id", command =>
            {
                command.Parameters.AddWithValue("membership_id", membershipId);
            }, identityScopeId, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>Finds the tenant membership record matching the supplied logical keys.</summary>
        public async Task<VersionedRecord<TenantMembership>?> FindAsync(ResolvedDatabaseRoute route, TenantReference tenant,
            SubjectReference subject, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(tenant);
            ArgumentNullException.ThrowIfNull(subject);
            if (tenant.IdentityScopeId != subject.IdentityScopeId)
                throw new ArgumentException("Tenant and subject must belong to the same identity scope.", nameof(subject));
            PostgreSqlDirectoryGuard.EnsureScope(route, tenant.IdentityScopeId);
            return await ReadAsync(route, "tenant_id = @tenant_id AND user_id = @user_id", command =>
            {
                command.Parameters.AddWithValue("tenant_id", tenant.TenantId);
                command.Parameters.AddWithValue("user_id", subject.UserId);
            }, tenant.IdentityScopeId, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>Lists tenant membership records for the requested tenant in a bounded deterministic window.</summary>
        public async Task<IReadOnlyList<VersionedRecord<TenantMembership>>> ListAsync(ResolvedDatabaseRoute route,
            TenantReference tenant, string? search, int offset, int limit, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(tenant);
            PostgreSqlDirectoryGuard.EnsureScope(route, tenant.IdentityScopeId);

            await using var connection = (NpgsqlConnection)await connectionFactory.OpenAsync(route, cancellationToken)
                .ConfigureAwait(false);
            await using var command = new NpgsqlCommand("""
                SELECT memberships.membership_id, memberships.user_id, memberships.status, memberships.row_version
                FROM identity_access.tenant_memberships AS memberships
                JOIN identity_access.users AS users
                  ON users.identity_scope_id = memberships.identity_scope_id
                 AND users.user_id = memberships.user_id
                WHERE memberships.identity_scope_id = @scope
                  AND memberships.tenant_id = @tenant_id
                  AND (@search_pattern IS NULL
                       OR lower(users.display_name) LIKE @search_pattern
                       OR memberships.membership_id = @search_id
                       OR memberships.user_id = @search_id)
                ORDER BY memberships.membership_id
                OFFSET @offset
                LIMIT @limit;
                """, connection);
            command.Parameters.AddWithValue("scope", tenant.IdentityScopeId);
            command.Parameters.AddWithValue("tenant_id", tenant.TenantId);
            PostgreSqlAdministrationSearch.AddParameters(command, search);
            command.Parameters.AddWithValue("offset", offset);
            command.Parameters.AddWithValue("limit", limit);

            await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            var records = new List<VersionedRecord<TenantMembership>>();
            while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                records.Add(new VersionedRecord<TenantMembership>(
                    new TenantMembership(
                        reader.GetGuid(0),
                        tenant,
                        new SubjectReference(tenant.IdentityScopeId, reader.GetGuid(1)),
                        (MembershipStatus)reader.GetInt16(2)),
                    reader.GetInt64(3)));
            }

            return records;
        }

        /// <summary>Lists tenant membership records for one subject across the resolved identity scope.</summary>
        public async Task<IReadOnlyList<VersionedRecord<TenantMembership>>> ListForSubjectAsync(
            ResolvedDatabaseRoute route, SubjectReference subject, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(subject);
            PostgreSqlDirectoryGuard.EnsureScope(route, subject.IdentityScopeId);

            await using var connection = (NpgsqlConnection)await connectionFactory.OpenAsync(route, cancellationToken)
                .ConfigureAwait(false);
            await using var command = new NpgsqlCommand("""
                SELECT membership_id, tenant_id, status, row_version
                FROM identity_access.tenant_memberships
                WHERE identity_scope_id = @scope
                  AND user_id = @user_id
                ORDER BY tenant_id, membership_id;
                """, connection);
            command.Parameters.AddWithValue("scope", subject.IdentityScopeId);
            command.Parameters.AddWithValue("user_id", subject.UserId);

            await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            var records = new List<VersionedRecord<TenantMembership>>();
            while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                records.Add(new VersionedRecord<TenantMembership>(
                    new TenantMembership(
                        reader.GetGuid(0),
                        new TenantReference(subject.IdentityScopeId, reader.GetGuid(1)),
                        subject,
                        (MembershipStatus)reader.GetInt16(2)),
                    reader.GetInt64(3)));
            }

            return records;
        }

        /// <summary>Creates a tenant membership record in the resolved database route.</summary>
        public async Task<VersionedRecord<TenantMembership>> CreateAsync(ResolvedDatabaseRoute route,
            TenantMembership membership, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(membership);
            PostgreSqlDirectoryGuard.EnsureScope(route, membership.Tenant.IdentityScopeId);
            await using var connection = (NpgsqlConnection)await connectionFactory.OpenAsync(route, cancellationToken)
                .ConfigureAwait(false);
            await using var command = new NpgsqlCommand("""
                INSERT INTO identity_access.tenant_memberships
                    (identity_scope_id, membership_id, tenant_id, user_id, status)
                VALUES (@scope, @membership_id, @tenant_id, @user_id, @status)
                RETURNING row_version;
                """, connection);
            AddIdentity(command, membership);
            command.Parameters.AddWithValue("status", (short)membership.Status);
            var version = (long)(await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false)
                ?? throw new InvalidOperationException("The inserted membership did not return a row version."));
            return new VersionedRecord<TenantMembership>(membership, version);
        }

        /// <summary>Updates a tenant membership record using optimistic concurrency.</summary>
        public async Task<VersionedRecord<TenantMembership>> UpdateAsync(ResolvedDatabaseRoute route,
            TenantMembership membership, long expectedVersion, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(membership);
            PostgreSqlDirectoryGuard.EnsureVersion(expectedVersion);
            PostgreSqlDirectoryGuard.EnsureScope(route, membership.Tenant.IdentityScopeId);
            await using var connection = (NpgsqlConnection)await connectionFactory.OpenAsync(route, cancellationToken)
                .ConfigureAwait(false);
            await using var command = new NpgsqlCommand("""
                UPDATE identity_access.tenant_memberships
                SET status = @status,
                    row_version = row_version + 1,
                    updated_at = transaction_timestamp()
                WHERE identity_scope_id = @scope
                  AND membership_id = @membership_id
                  AND tenant_id = @tenant_id
                  AND user_id = @user_id
                  AND row_version = @expected_version
                RETURNING row_version;
                """, connection);
            AddIdentity(command, membership);
            command.Parameters.AddWithValue("status", (short)membership.Status);
            command.Parameters.AddWithValue("expected_version", expectedVersion);
            var result = await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
            if (result is null or DBNull) throw new IdentityConcurrencyException();
            return new VersionedRecord<TenantMembership>(membership, (long)result);
        }

        private async Task<VersionedRecord<TenantMembership>?> ReadAsync(ResolvedDatabaseRoute route, string predicate,
            Action<NpgsqlCommand> bind, Guid identityScopeId, CancellationToken cancellationToken)
        {
            await using var connection = (NpgsqlConnection)await connectionFactory.OpenAsync(route, cancellationToken)
                .ConfigureAwait(false);
            await using var command = new NpgsqlCommand($"""
                SELECT membership_id, tenant_id, user_id, status, row_version
                FROM identity_access.tenant_memberships
                WHERE identity_scope_id = @scope AND {predicate};
                """, connection);
            command.Parameters.AddWithValue("scope", identityScopeId);
            bind(command);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false)) return null;
            var membership = new TenantMembership(reader.GetGuid(0),
                new TenantReference(identityScopeId, reader.GetGuid(1)),
                new SubjectReference(identityScopeId, reader.GetGuid(2)),
                (MembershipStatus)reader.GetInt16(3));
            return new VersionedRecord<TenantMembership>(membership, reader.GetInt64(4));
        }

        private static void AddIdentity(NpgsqlCommand command, TenantMembership membership)
        {
            command.Parameters.AddWithValue("scope", membership.Tenant.IdentityScopeId);
            command.Parameters.AddWithValue("membership_id", membership.MembershipId);
            command.Parameters.AddWithValue("tenant_id", membership.Tenant.TenantId);
            command.Parameters.AddWithValue("user_id", membership.Subject.UserId);
        }
    }
}
