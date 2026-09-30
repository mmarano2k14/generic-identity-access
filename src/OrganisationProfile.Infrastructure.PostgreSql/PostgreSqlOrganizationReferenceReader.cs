using Npgsql;
using NpgsqlTypes;
using OrganisationProfile.Application;
using OrganisationProfile.Domain;

namespace OrganisationProfile.Infrastructure.PostgreSql
{
    /// <summary>
    /// Reads the minimum Organization Directory state required by OrganisationProfile without
    /// referencing Organization Directory implementation assemblies.
    /// </summary>
    public sealed class PostgreSqlOrganizationReferenceReader(
        NpgsqlDataSource dataSource) : IOrganizationReferenceReader
    {
        /// <inheritdoc />
        public async Task<OrganizationReferenceState?> GetAsync(
            OrganizationReference reference,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(reference);

            await using var command = dataSource.CreateCommand("""
                SELECT status
                FROM organization_directory.organizations
                WHERE identity_scope_id = @identity_scope_id
                  AND tenant_id = @tenant_id
                  AND organization_id = @organization_id;
                """);

            command.Parameters.AddWithValue(
                "identity_scope_id",
                NpgsqlDbType.Uuid,
                reference.IdentityScopeId);
            command.Parameters.AddWithValue(
                "tenant_id",
                NpgsqlDbType.Uuid,
                reference.TenantId);
            command.Parameters.AddWithValue(
                "organization_id",
                NpgsqlDbType.Uuid,
                reference.OrganizationId);

            var result = await command.ExecuteScalarAsync(cancellationToken);

            if (result is null or DBNull)
            {
                return null;
            }

            var status = Convert.ToInt16(
                result,
                System.Globalization.CultureInfo.InvariantCulture);

            return new OrganizationReferenceState(
                reference,
                IsActive: status == 1);
        }
    }
}
