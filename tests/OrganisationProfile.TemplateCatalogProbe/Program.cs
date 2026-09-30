using Npgsql;
using NpgsqlTypes;
using OrganisationProfile.Application.Registry;
using OrganisationProfile.Application.Storage;
using OrganisationProfile.Application.Templates;
using OrganisationProfile.Domain;
using OrganisationProfile.Infrastructure.PostgreSql;
using OrganisationProfileAggregate =
    global::OrganisationProfile.Domain.OrganisationProfile;

namespace OrganisationProfile.TemplateCatalogProbe
{
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

            var templateKey =
                new OrganisationProfileTemplateKey(
                    $"probe-template-{Guid.NewGuid():N}"[..40]);

            Guid identityScopeId = Guid.Empty;
            Guid tenantId = Guid.Empty;
            Guid organizationId = Guid.Empty;

            try
            {
                var clock = new ProbeClock(
                    new DateTimeOffset(
                        2026,
                        9,
                        30,
                        1,
                        0,
                        0,
                        TimeSpan.Zero));

                var registry =
                    new ProbeDomainRegistryReader();

                registry.AddPublished("inventory", 3);
                registry.AddPublished("commerce", 4);
                registry.AddPublished("finance", 5);

                var domainValidator =
                    new OrganisationProfileDomainRegistryValidator(
                        registry);

                var templateStore =
                    new PostgreSqlOrganisationProfileTemplateStore(dataSource);
                var versionStore =
                    new PostgreSqlOrganisationProfileTemplateVersionStore(dataSource);
                var profileStore =
                    new PostgreSqlOrganisationProfileStore(dataSource);

                var definitions =
                    new OrganisationProfileTemplateDefinitionService(
                        templateStore,
                        clock);

                var drafts =
                    new OrganisationProfileTemplateDraftService(
                        templateStore,
                        versionStore,
                        clock);

                var publication =
                    new OrganisationProfileTemplatePublicationService(
                        templateStore,
                        versionStore,
                        domainValidator,
                        new OrganisationProfileTemplateContentHasher(),
                        clock);

                var template =
                    await definitions.CreateAsync(
                        templateKey,
                        "Probe Template",
                        CancellationToken.None);

                Require(
                    template.RowVersion == 1,
                    "Template row version must start at 1.");

                var versionOne =
                    new OrganisationProfileTemplateVersionNumber(1);

                var draft =
                    await drafts.CreateAsync(
                        templateKey,
                        versionOne,
                        new[]
                        {
                            Domain("inventory", 3),
                            Domain("commerce", 4)
                        },
                        CancellationToken.None);

                Require(
                    draft.Status ==
                        OrganisationProfileTemplateVersionStatus.Draft &&
                    draft.RowVersion == 1,
                    "Draft template version was not persisted.");

                clock.UtcNow = clock.UtcNow.AddMinutes(1);

                var replaced =
                    await drafts.ReplaceDomainsAsync(
                        templateKey,
                        versionOne,
                        new[]
                        {
                            Domain("finance", 5),
                            Domain("commerce", 4),
                            Domain("inventory", 3)
                        },
                        draft.RowVersion,
                        CancellationToken.None);

                Require(
                    replaced is not null &&
                    replaced.RowVersion == 2 &&
                    replaced.Domains.Select(item => item.DomainKey.Value)
                        .SequenceEqual(
                            new[] { "commerce", "finance", "inventory" }),
                    "Draft replacement is not deterministic.");

                clock.UtcNow = clock.UtcNow.AddMinutes(1);

                var published =
                    await publication.PublishAsync(
                        templateKey,
                        versionOne,
                        replaced!.RowVersion,
                        CancellationToken.None);

                var publishedValue =
                    published
                    ?? throw new InvalidOperationException(
                        "Template publication returned no durable version.");

                var publishedHash =
                    publishedValue.ContentHash
                    ?? throw new InvalidOperationException(
                        "Published template is missing its content hash.");

                Require(
                    publishedValue.Status ==
                        OrganisationProfileTemplateVersionStatus.Published &&
                    publishedValue.RowVersion == 3,
                    "Template publication failed.");

                var reorderedHash =
                    new OrganisationProfileTemplateContentHasher().Compute(
                        templateKey,
                        versionOne,
                        publishedValue.Domains.Reverse());

                Require(
                    reorderedHash == publishedHash,
                    "Content hash must not depend on domain input order.");

                await RequireExceptionAsync<
                    OrganisationProfileTemplateVersionImmutableException>(
                    () => drafts.ReplaceDomainsAsync(
                        templateKey,
                        versionOne,
                        new[] { Domain("commerce", 9) },
                        publishedValue.RowVersion,
                        CancellationToken.None),
                    "Published template content was unexpectedly mutable.");

                var versionTwo =
                    new OrganisationProfileTemplateVersionNumber(2);

                _ = await drafts.CreateAsync(
                    templateKey,
                    versionTwo,
                    new[] { Domain("commerce", 5) },
                    CancellationToken.None);

                var tenant =
                    await ResolveActiveTenantAsync(dataSource);

                if (tenant is null)
                {
                    throw new InvalidOperationException(
                        "No active Identity Access tenant exists.");
                }

                identityScopeId = tenant.Value.IdentityScopeId;
                tenantId = tenant.Value.TenantId;
                organizationId = Guid.NewGuid();

                await InsertProbeOrganizationAsync(
                    dataSource,
                    identityScopeId,
                    tenantId,
                    organizationId);

                var organization =
                    new OrganizationReference(
                        identityScopeId,
                        tenantId,
                        organizationId);

                var profile =
                    await profileStore.CreateAsync(
                        OrganisationProfileAggregate.Create(
                            OrganisationProfileId.New(),
                            organization,
                            null,
                            clock.UtcNow),
                        CancellationToken.None);

                await RequireExceptionAsync<
                    OrganisationProfileTemplateVersionNotPublishedException>(
                    () => profileStore.UpdateAsync(
                        profile.WithTemplate(
                            new OrganisationProfileTemplatePin(
                                templateKey,
                                versionTwo),
                            clock.UtcNow.AddSeconds(1)),
                        CancellationToken.None),
                    "Draft version was unexpectedly assignable.");

                var pinned =
                    await profileStore.UpdateAsync(
                        profile.WithTemplate(
                            new OrganisationProfileTemplatePin(
                                templateKey,
                                versionOne),
                            clock.UtcNow.AddSeconds(2)),
                        CancellationToken.None);

                Require(
                    pinned is not null &&
                    pinned.TemplatePin?.TemplateKey == templateKey &&
                    pinned.TemplatePin?.Version == versionOne,
                    "Published template version could not be pinned.");

                clock.UtcNow = clock.UtcNow.AddMinutes(1);

                var retired =
                    await publication.RetireAsync(
                        templateKey,
                        versionOne,
                        publishedValue.RowVersion,
                        CancellationToken.None);

                Require(
                    retired is not null &&
                    retired.Status ==
                        OrganisationProfileTemplateVersionStatus.Retired &&
                    retired.ContentHash == publishedHash &&
                    retired.Domains.SequenceEqual(publishedValue.Domains),
                    "Retirement changed immutable published content.");

                var profileAfterRetirement =
                    await profileStore.UpdateAsync(
                        pinned!.WithStatus(
                            OrganisationProfileStatus.Disabled,
                            clock.UtcNow.AddSeconds(1)),
                        CancellationToken.None);

                Require(
                    profileAfterRetirement is not null &&
                    profileAfterRetirement.Status ==
                        OrganisationProfileStatus.Disabled &&
                    profileAfterRetirement.TemplatePin?.Version == versionOne,
                    "Historical pin did not survive template retirement.");

                Console.WriteLine(
                    "OrganisationProfile template catalog probe: GREEN");
                return 0;
            }
            catch (Exception exception)
            {
                Console.Error.WriteLine(exception);
                return 1;
            }
            finally
            {
                await CleanupAsync(
                    dataSource,
                    templateKey,
                    identityScopeId,
                    tenantId,
                    organizationId);
            }
        }

        private static OrganisationProfileDomainSelection Domain(
            string key,
            int version) =>
            new(
                new DomainKey(key),
                new DomainVersion(version));

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
                return null;

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
            var organizationKey =
                $"profile-template-probe-{organizationId:N}"[..50];

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
                    'OrganisationProfile Template Probe',
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
                organizationKey);

            await command.ExecuteNonQueryAsync();
        }

        private static async Task CleanupAsync(
            NpgsqlDataSource dataSource,
            OrganisationProfileTemplateKey templateKey,
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

                    DELETE FROM organisation_profile.organisation_profile_template_versions
                    WHERE template_key = @template_key;

                    DELETE FROM organisation_profile.organisation_profile_templates
                    WHERE template_key = @template_key;

                    DELETE FROM organization_directory.organizations
                    WHERE identity_scope_id = @identity_scope_id
                      AND tenant_id = @tenant_id
                      AND organization_id = @organization_id;
                    """);

                command.Parameters.AddWithValue(
                    "template_key",
                    NpgsqlDbType.Varchar,
                    templateKey.Value);
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
                    $"Template probe cleanup warning: {cleanupException.Message}");
            }
        }

        private static void Require(
            bool condition,
            string message)
        {
            if (!condition)
                throw new InvalidOperationException(message);
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
