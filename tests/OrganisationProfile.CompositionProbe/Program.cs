using Npgsql;
using NpgsqlTypes;
using OrganisationProfile.Application.Composition;
using OrganisationProfile.Application.Registry;
using OrganisationProfile.Application.Storage;
using OrganisationProfile.Application.Templates;
using OrganisationProfile.Domain;
using OrganisationProfile.Infrastructure.PostgreSql;
using OrganisationProfileAggregate =
    global::OrganisationProfile.Domain.OrganisationProfile;

namespace OrganisationProfile.CompositionProbe
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
                    $"composition-probe-{Guid.NewGuid():N}"[..40]);

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
                        2,
                        0,
                        0,
                        TimeSpan.Zero));

                var registry =
                    new ProbeDomainRegistryReader();

                registry.Set(
                    "commerce",
                    4,
                    DomainRegistryVersionStatus.Published);
                registry.Set(
                    "inventory",
                    3,
                    DomainRegistryVersionStatus.Published);
                registry.Set(
                    "manufacturing",
                    2,
                    DomainRegistryVersionStatus.Published);
                registry.Set(
                    "manufacturing",
                    3,
                    DomainRegistryVersionStatus.Published);
                registry.Set(
                    "finance",
                    5,
                    DomainRegistryVersionStatus.Retired);

                var validator =
                    new OrganisationProfileDomainRegistryValidator(
                        registry);

                var templateStore =
                    new PostgreSqlOrganisationProfileTemplateStore(
                        dataSource);
                var templateVersionStore =
                    new PostgreSqlOrganisationProfileTemplateVersionStore(
                        dataSource);
                var profileStore =
                    new PostgreSqlOrganisationProfileStore(
                        dataSource);
                var overrideStore =
                    new PostgreSqlOrganisationProfileDomainOverrideStore(
                        dataSource);
                var effectiveVersionStore =
                    new PostgreSqlOrganisationProfileVersionStore(
                        dataSource);

                var definitions =
                    new OrganisationProfileTemplateDefinitionService(
                        templateStore,
                        clock);

                var drafts =
                    new OrganisationProfileTemplateDraftService(
                        templateStore,
                        templateVersionStore,
                        clock);

                var publication =
                    new OrganisationProfileTemplatePublicationService(
                        templateStore,
                        templateVersionStore,
                        validator,
                        new OrganisationProfileTemplateContentHasher(),
                        clock);

                var overrideService =
                    new OrganisationProfileDomainOverrideService(
                        profileStore,
                        overrideStore,
                        validator,
                        clock);

                var compositionService =
                    new OrganisationProfileCompositionService(
                        profileStore,
                        templateVersionStore,
                        overrideStore,
                        effectiveVersionStore,
                        new OrganisationProfileCompositionResolver(),
                        validator,
                        new OrganisationProfileEffectiveContentHasher(),
                        clock);

                _ = await definitions.CreateAsync(
                    templateKey,
                    "Composition Probe",
                    CancellationToken.None);

                var templateVersion =
                    new OrganisationProfileTemplateVersionNumber(1);

                var draft = await drafts.CreateAsync(
                    templateKey,
                    templateVersion,
                    new[]
                    {
                        Domain("commerce", 4),
                        Domain("inventory", 3)
                    },
                    CancellationToken.None);

                clock.UtcNow = clock.UtcNow.AddMinutes(1);

                var published = await publication.PublishAsync(
                    templateKey,
                    templateVersion,
                    draft.RowVersion,
                    CancellationToken.None)
                    ?? throw new InvalidOperationException(
                        "Template publication returned null.");

                var tenant =
                    await ResolveActiveTenantAsync(dataSource)
                    ?? throw new InvalidOperationException(
                        "No active Identity Access tenant exists.");

                identityScopeId = tenant.IdentityScopeId;
                tenantId = tenant.TenantId;
                organizationId = Guid.NewGuid();

                await InsertProbeOrganizationAsync(
                    dataSource,
                    identityScopeId,
                    tenantId,
                    organizationId);

                var profile = await profileStore.CreateAsync(
                    OrganisationProfileAggregate.Create(
                        OrganisationProfileId.New(),
                        new OrganizationReference(
                            identityScopeId,
                            tenantId,
                            organizationId),
                        new OrganisationProfileTemplatePin(
                            templateKey,
                            templateVersion),
                        clock.UtcNow),
                    CancellationToken.None);

                clock.UtcNow = clock.UtcNow.AddMinutes(1);

                var withOverrides = await overrideService.ReplaceAsync(
                    profile.OrganisationProfileId,
                    profile.RowVersion,
                    new[]
                    {
                        Disable("inventory"),
                        Enable("manufacturing", 2)
                    },
                    CancellationToken.None)
                    ?? throw new InvalidOperationException(
                        "Override replacement returned null.");

                Require(
                    withOverrides.RowVersion == 2,
                    "Override replacement must advance parent profile RowVersion.");

                var first = await compositionService.ResolveAndSnapshotAsync(
                    profile.OrganisationProfileId,
                    withOverrides.RowVersion,
                    CancellationToken.None)
                    ?? throw new InvalidOperationException(
                        "First effective snapshot returned null.");

                Require(
                    first.Version.Value == 1,
                    "First semantic profile version must be 1.");

                Require(
                    first.Domains.Select(
                        item =>
                            $"{item.DomainKey.Value}@{item.DomainVersion.Value}")
                        .SequenceEqual(
                            new[]
                            {
                                "commerce@4",
                                "manufacturing@2"
                            }),
                    "Template plus override resolution is incorrect.");

                var repeated = await compositionService.ResolveAndSnapshotAsync(
                    profile.OrganisationProfileId,
                    withOverrides.RowVersion,
                    CancellationToken.None)
                    ?? throw new InvalidOperationException(
                        "Repeated effective snapshot returned null.");

                Require(
                    repeated.Version == first.Version &&
                    repeated.ContentHash == first.ContentHash,
                    "Unchanged content must not create a duplicate semantic version.");

                await RequireExceptionAsync<
                    DomainRegistryVersionUnavailableException>(
                    () => overrideService.ReplaceAsync(
                        profile.OrganisationProfileId,
                        withOverrides.RowVersion,
                        new[]
                        {
                            Enable("finance", 5)
                        },
                        CancellationToken.None),
                    "Retired domain version was unexpectedly selectable.");

                await RequireExceptionAsync<
                    OrganisationProfileConcurrencyException>(
                    () => overrideService.ReplaceAsync(
                        profile.OrganisationProfileId,
                        profile.RowVersion,
                        new[]
                        {
                            Enable("manufacturing", 3)
                        },
                        CancellationToken.None),
                    "Stale profile RowVersion was unexpectedly accepted.");

                clock.UtcNow = clock.UtcNow.AddMinutes(1);

                var changed = await overrideService.ReplaceAsync(
                    profile.OrganisationProfileId,
                    withOverrides.RowVersion,
                    new[]
                    {
                        Disable("inventory"),
                        Enable("manufacturing", 3)
                    },
                    CancellationToken.None)
                    ?? throw new InvalidOperationException(
                        "Second override replacement returned null.");

                var second = await compositionService.ResolveAndSnapshotAsync(
                    profile.OrganisationProfileId,
                    changed.RowVersion,
                    CancellationToken.None)
                    ?? throw new InvalidOperationException(
                        "Second effective snapshot returned null.");

                Require(
                    second.Version.Value == 2 &&
                    second.ContentHash != first.ContentHash,
                    "Changed composition must produce a new semantic version.");

                registry.Set(
                    "commerce",
                    4,
                    DomainRegistryVersionStatus.Retired);

                var historical = await compositionService.ResolveAndSnapshotAsync(
                    profile.OrganisationProfileId,
                    changed.RowVersion,
                    CancellationToken.None)
                    ?? throw new InvalidOperationException(
                        "Historical-domain resolution returned null.");

                Require(
                    historical.Version == second.Version,
                    "Retired but resolvable historical domain must not force a new version.");

                var versions = await effectiveVersionStore.ListAsync(
                    profile.OrganisationProfileId,
                    0,
                    100,
                    CancellationToken.None);

                Require(
                    versions.Count == 2 &&
                    versions[0].Domains.Any(
                        item =>
                            item.DomainKey.Value == "manufacturing" &&
                            item.DomainVersion.Value == 2) &&
                    versions[1].Domains.Any(
                        item =>
                            item.DomainKey.Value == "manufacturing" &&
                            item.DomainVersion.Value == 3),
                    "Immutable effective-version history is incomplete.");

                await RequireConstraintViolationAsync(
                    () => AttemptVersionMutationAsync(
                        dataSource,
                        profile.OrganisationProfileId,
                        first.Version),
                    "ck_organisation_profile_version_immutable",
                    "Effective profile version metadata was unexpectedly mutable.");

                await RequireConstraintViolationAsync(
                    () => AttemptVersionDomainMutationAsync(
                        dataSource,
                        profile.OrganisationProfileId,
                        first.Version),
                    "ck_organisation_profile_version_domain_immutable",
                    "Effective profile version domains were unexpectedly mutable.");

                Require(
                    published.ContentHash is not null,
                    "Published template hash unexpectedly missing.");

                Console.WriteLine(
                    "OrganisationProfile effective composition probe: GREEN");
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

        private static OrganisationProfileDomainOverride Enable(
            string key,
            int version) =>
            new(
                new DomainKey(key),
                new DomainVersion(version),
                OrganisationProfileDomainOverrideOperation.Enable);

        private static OrganisationProfileDomainOverride Disable(
            string key) =>
            new(
                new DomainKey(key),
                null,
                OrganisationProfileDomainOverrideOperation.Disable);

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
            var organizationKey =
                $"profile-composition-probe-{organizationId:N}"[..54];

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
                    'OrganisationProfile Composition Probe',
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

        private static async Task AttemptVersionMutationAsync(
            NpgsqlDataSource dataSource,
            OrganisationProfileId organisationProfileId,
            OrganisationProfileVersionNumber version)
        {
            await using var command = dataSource.CreateCommand("""
                UPDATE organisation_profile.organisation_profile_versions
                SET resolved_at = resolved_at + interval '1 second'
                WHERE organisation_profile_id = @organisation_profile_id
                  AND profile_version = @profile_version;
                """);

            command.Parameters.AddWithValue(
                "organisation_profile_id",
                NpgsqlDbType.Uuid,
                organisationProfileId.Value);
            command.Parameters.AddWithValue(
                "profile_version",
                NpgsqlDbType.Bigint,
                version.Value);

            await command.ExecuteNonQueryAsync();
        }

        private static async Task AttemptVersionDomainMutationAsync(
            NpgsqlDataSource dataSource,
            OrganisationProfileId organisationProfileId,
            OrganisationProfileVersionNumber version)
        {
            await using var command = dataSource.CreateCommand("""
                UPDATE organisation_profile.organisation_profile_version_domains
                SET domain_version = domain_version + 1
                WHERE organisation_profile_id = @organisation_profile_id
                  AND profile_version = @profile_version;
                """);

            command.Parameters.AddWithValue(
                "organisation_profile_id",
                NpgsqlDbType.Uuid,
                organisationProfileId.Value);
            command.Parameters.AddWithValue(
                "profile_version",
                NpgsqlDbType.Bigint,
                version.Value);

            await command.ExecuteNonQueryAsync();
        }

        private static async Task RequireConstraintViolationAsync(
            Func<Task> action,
            string constraintName,
            string message)
        {
            try
            {
                await action();
            }
            catch (PostgresException exception) when (
                exception.SqlState == PostgresErrorCodes.CheckViolation &&
                string.Equals(
                    exception.ConstraintName,
                    constraintName,
                    StringComparison.Ordinal))
            {
                return;
            }

            throw new InvalidOperationException(message);
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
                    $"Composition probe cleanup warning: {cleanupException.Message}");
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
