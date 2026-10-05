using Npgsql;
using NpgsqlTypes;
using OrganisationProfile.Application.Storage;
using OrganisationProfile.Domain;
using OrganisationProfile.Infrastructure.PostgreSql;
using OrganisationProfileAggregate = global::OrganisationProfile.Domain.OrganisationProfile;

namespace OrganisationProfile.PostgreSqlProbe
{
    /// <summary>Live shared-database probe for OrganisationProfile PostgreSQL persistence.</summary>
    internal static class Program
    {
        private static async Task<int> Main()
        {
            var connectionString =
                Environment.GetEnvironmentVariable(
                    "ORGANISATION_PROFILE_POSTGRES_DEFAULT")
                ?? Environment.GetEnvironmentVariable(
                    "IDENTITY_ACCESS_POSTGRES_DEFAULT");

            if (string.IsNullOrWhiteSpace(connectionString))
            {
                Console.Error.WriteLine(
                    "Set ORGANISATION_PROFILE_POSTGRES_DEFAULT or IDENTITY_ACCESS_POSTGRES_DEFAULT.");
                return 2;
            }

            await using var dataSource =
                NpgsqlDataSource.Create(connectionString);

            Guid identityScopeId = Guid.Empty;
            Guid tenantId = Guid.Empty;
            Guid organizationId = Guid.Empty;

            try
            {
                var tenant = await ResolveActiveTenantAsync(dataSource);

                if (tenant is null)
                {
                    throw new InvalidOperationException(
                        "No active Identity Access tenant exists. Bootstrap the development environment first.");
                }

                identityScopeId = tenant.Value.IdentityScopeId;
                tenantId = tenant.Value.TenantId;
                organizationId = Guid.NewGuid();

                await InsertProbeOrganizationAsync(
                    dataSource,
                    identityScopeId,
                    tenantId,
                    organizationId);

                var organization = new OrganizationReference(
                    identityScopeId,
                    tenantId,
                    organizationId);

                var organizationReader =
                    new PostgreSqlOrganizationReferenceReader(dataSource);

                var organizationState =
                    await organizationReader.GetAsync(
                        organization,
                        CancellationToken.None);

                Require(
                    organizationState is not null &&
                    organizationState.IsActive,
                    "Temporary Organization could not be resolved through the adapter.");

                var store =
                    new PostgreSqlOrganisationProfileStore(dataSource);

                var now = DateTimeOffset.UtcNow;
                var profile = OrganisationProfileAggregate.Create(
                    OrganisationProfileId.New(),
                    organization,
                    templatePin: null,
                    now);

                var created = await store.CreateAsync(
                    profile,
                    CancellationToken.None);

                Require(
                    created.RowVersion == 1,
                    "Created profile row version must be 1.");

                var byId = await store.GetAsync(
                    created.OrganisationProfileId,
                    CancellationToken.None);

                Require(
                    byId is not null &&
                    byId.Organization == organization,
                    "GetAsync did not return the created profile.");

                var byOrganization =
                    await store.FindByOrganizationAsync(
                        organization,
                        CancellationToken.None);

                Require(
                    byOrganization is not null &&
                    byOrganization.OrganisationProfileId ==
                        created.OrganisationProfileId,
                    "FindByOrganizationAsync did not return the created profile.");

                var listed = await store.ListAsync(
                    identityScopeId,
                    tenantId,
                    0,
                    500,
                    CancellationToken.None);

                Require(
                    listed.Any(item =>
                        item.OrganisationProfileId ==
                            created.OrganisationProfileId),
                    "Tenant-scoped list did not include the created profile.");

                var activeUpdate = created.WithStatus(
                    OrganisationProfileStatus.Active,
                    now.AddSeconds(1));

                var updated = await store.UpdateAsync(
                    activeUpdate,
                    CancellationToken.None);

                Require(
                    updated is not null &&
                    updated.RowVersion == 2 &&
                    updated.TemplatePin is null,
                    "Profile optimistic-concurrency update did not persist.");

                await RequireExceptionAsync<OrganisationProfileConcurrencyException>(
                    () => store.UpdateAsync(
                        activeUpdate,
                        CancellationToken.None),
                    "Stale profile update was not rejected.");

                var disabled = updated!.WithStatus(
                    OrganisationProfileStatus.Disabled,
                    now.AddSeconds(2));

                var disabledPersisted =
                    await store.UpdateAsync(
                        disabled,
                        CancellationToken.None);

                Require(
                    disabledPersisted is not null &&
                    disabledPersisted.RowVersion == 3 &&
                    disabledPersisted.Status ==
                        OrganisationProfileStatus.Disabled,
                    "Profile lifecycle update did not persist.");

                await RequireExceptionAsync<OrganisationProfileAlreadyExistsException>(
                    () => store.CreateAsync(
                        OrganisationProfileAggregate.Create(
                            OrganisationProfileId.New(),
                            organization,
                            null,
                            now.AddSeconds(3)),
                        CancellationToken.None),
                    "Second profile for the same Organization was not rejected.");

                Console.WriteLine(
                    "OrganisationProfile PostgreSQL persistence probe: GREEN");
                return 0;
            }
            catch (Exception exception)
            {
                Console.Error.WriteLine(exception);
                return 1;
            }
            finally
            {
                if (identityScopeId != Guid.Empty &&
                    tenantId != Guid.Empty &&
                    organizationId != Guid.Empty)
                {
                    await CleanupAsync(
                        dataSource,
                        identityScopeId,
                        tenantId,
                        organizationId);
                }
            }
        }

        private static async Task<(Guid IdentityScopeId, Guid TenantId)?>
            ResolveActiveTenantAsync(
                NpgsqlDataSource dataSource)
        {
            await using var command = dataSource.CreateCommand("""
                SELECT identity_scope_id, tenant_id
                FROM identity_access.tenants
                WHERE status = 1
                ORDER BY tenant_id
                LIMIT 1;
                """);

            await using var reader =
                await command.ExecuteReaderAsync();

            if (!await reader.ReadAsync())
            {
                return null;
            }

            return (
                reader.GetGuid(0),
                reader.GetGuid(1));
        }

        private static async Task InsertProbeOrganizationAsync(
            NpgsqlDataSource dataSource,
            Guid identityScopeId,
            Guid tenantId,
            Guid organizationId)
        {
            var key =
                $"profile-probe-{organizationId:N}"
                    .Substring(0, 40);

            await using var command = dataSource.CreateCommand("""
                INSERT INTO organization_directory.organizations
                (
                    identity_scope_id,
                    tenant_id,
                    organization_id,
                    organization_key,
                    display_name,
                    organization_type,
                    parent_organization_id,
                    status,
                    row_version,
                    created_at,
                    updated_at
                )
                VALUES
                (
                    @identity_scope_id,
                    @tenant_id,
                    @organization_id,
                    @organization_key,
                    'OrganisationProfile Probe',
                    'organization',
                    NULL,
                    1,
                    1,
                    transaction_timestamp(),
                    transaction_timestamp()
                );
                """);

            command.Parameters.AddWithValue(
                "identity_scope_id",
                NpgsqlDbType.Uuid,
                identityScopeId);
            command.Parameters.AddWithValue(
                "tenant_id",
                NpgsqlDbType.Uuid,
                tenantId);
            command.Parameters.AddWithValue(
                "organization_id",
                NpgsqlDbType.Uuid,
                organizationId);
            command.Parameters.AddWithValue(
                "organization_key",
                NpgsqlDbType.Varchar,
                key);

            await command.ExecuteNonQueryAsync();
        }

        private static async Task CleanupAsync(
            NpgsqlDataSource dataSource,
            Guid identityScopeId,
            Guid tenantId,
            Guid organizationId)
        {
            try
            {
                await using var command = dataSource.CreateCommand("""
                    DELETE FROM organisation_profile.organisation_profiles
                    WHERE identity_scope_id = @identity_scope_id
                      AND tenant_id = @tenant_id
                      AND organization_id = @organization_id;

                    DELETE FROM organization_directory.organizations
                    WHERE identity_scope_id = @identity_scope_id
                      AND tenant_id = @tenant_id
                      AND organization_id = @organization_id;
                    """);

                command.Parameters.AddWithValue(
                    "identity_scope_id",
                    NpgsqlDbType.Uuid,
                    identityScopeId);
                command.Parameters.AddWithValue(
                    "tenant_id",
                    NpgsqlDbType.Uuid,
                    tenantId);
                command.Parameters.AddWithValue(
                    "organization_id",
                    NpgsqlDbType.Uuid,
                    organizationId);

                await command.ExecuteNonQueryAsync();
            }
            catch (Exception cleanupException)
            {
                Console.Error.WriteLine(
                    $"Probe cleanup warning: {cleanupException.Message}");
            }
        }

        private static void Require(
            bool condition,
            string message)
        {
            if (!condition)
            {
                throw new InvalidOperationException(message);
            }
        }

        private static async Task RequireExceptionAsync<TException>(
            Func<Task> action,
            string message)
            where TException : Exception
        {
            try
            {
                await action();
            }
            catch (TException)
            {
                return;
            }

            throw new InvalidOperationException(message);
        }
    }
}
