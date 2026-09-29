using Npgsql;
using NpgsqlTypes;
using OrganizationDirectory.Application.Storage;
using OrganizationDirectory.Domain;

namespace OrganizationDirectory.Infrastructure.PostgreSql
{
    /// <summary>Reads tenant-membership reference state from the shared Identity Access database.</summary>
    public sealed class PostgreSqlTenantMembershipReferenceReader(
        NpgsqlDataSource dataSource) : ITenantMembershipReferenceReader
    {
        /// <inheritdoc />
        public async Task<TenantMembershipReferenceState?> GetAsync(
            TenantMembershipReference reference,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(reference);

            await using var command = dataSource.CreateCommand("""
                SELECT status
                FROM identity_access.tenant_memberships
                WHERE identity_scope_id = @identity_scope_id
                  AND tenant_id = @tenant_id
                  AND membership_id = @tenant_membership_id;
                """);

            command.Parameters.AddWithValue(
                "identity_scope_id",
                NpgsqlDbType.Uuid,
                reference.IdentityScopeId.Value);
            command.Parameters.AddWithValue(
                "tenant_id",
                NpgsqlDbType.Uuid,
                reference.TenantId.Value);
            command.Parameters.AddWithValue(
                "tenant_membership_id",
                NpgsqlDbType.Uuid,
                reference.TenantMembershipId.Value);

            var result = await command.ExecuteScalarAsync(cancellationToken);
            if (result is null or DBNull)
            {
                return null;
            }

            var status = Convert.ToInt16(
                result,
                System.Globalization.CultureInfo.InvariantCulture);

            return new TenantMembershipReferenceState(
                reference,
                IsActive: status == 1);
        }
    }
}
