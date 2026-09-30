using Npgsql;
using NpgsqlTypes;
using OrganisationProfile.Application;
using OrganisationProfile.Application.Composition;
using OrganisationProfile.Application.Registry;
using OrganisationProfile.Application.Storage;
using OrganisationProfile.Application.Templates;
using OrganisationProfile.Domain;
using OrganisationProfile.Infrastructure.PostgreSql;

namespace OrganisationProfile.QualificationProbe
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
                    $"qualification-{Guid.NewGuid():N}"[..46]);

            Guid identityScopeId = Guid.Empty;
            Guid tenantId = Guid.Empty;
            Guid organizationId = Guid.Empty;

            try
            {
                var clock = new QualificationClock(
                    new DateTimeOffset(
                        2026,
                        9,
                        30,
                        3,
                        0,
                        0,
                        TimeSpan.Zero));

                var registry = new QualificationDomainRegistryReader();
                registry.Set("commerce", 4, DomainRegistryVersionStatus.Published);
                registry.Set("inventory", 3, DomainRegistryVersionStatus.Published);
                registry.Set("manufacturing", 2, DomainRegistryVersionStatus.Published);
                registry.Set("manufacturing", 3, DomainRegistryVersionStatus.Published);

                var validator =
                    new OrganisationProfileDomainRegistryValidator(registry);

                var profileStore =
                    new PostgreSqlOrganisationProfileStore(dataSource);
                var templateStore =
                    new PostgreSqlOrganisationProfileTemplateStore(dataSource);
                var templateVersionStore =
                    new PostgreSqlOrganisationProfileTemplateVersionStore(dataSource);
                var overrideStore =
                    new PostgreSqlOrganisationProfileDomainOverrideStore(dataSource);
                var effectiveVersionStore =
                    new PostgreSqlOrganisationProfileVersionStore(dataSource);
                var organizationReader =
                    new PostgreSqlOrganizationReferenceReader(dataSource);

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
                var profileDefinitions =
                    new OrganisationProfileDefinitionService(
                        profileStore,
                        organizationReader,
                        templateStore,
                        templateVersionStore,
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
                    "Qualification Profile",
                    CancellationToken.None);

                var templateVersion =
                    new OrganisationProfileTemplateVersionNumber(1);

                var draft = await drafts.CreateAsync(
                    templateKey,
                    templateVersion,
                    new[]
                    {
                        Domain("inventory", 3),
                        Domain("commerce", 4)
                    },
                    CancellationToken.None);

                var templateHasher =
                    new OrganisationProfileTemplateContentHasher();

                var canonicalTemplateHash = templateHasher.Compute(
                    templateKey,
                    templateVersion,
                    new[]
                    {
                        Domain("commerce", 4),
                        Domain("inventory", 3)
                    });

                var reversedTemplateHash = templateHasher.Compute(
                    templateKey,
                    templateVersion,
                    new[]
                    {
                        Domain("inventory", 3),
                        Domain("commerce", 4)
                    });

                Require(
                    canonicalTemplateHash == reversedTemplateHash,
                    "Template hash changed with input ordering.");

                clock.UtcNow = clock.UtcNow.AddMinutes(1);

                var published = await publication.PublishAsync(
                    templateKey,
                    templateVersion,
                    draft.RowVersion,
                    CancellationToken.None)
                    ?? throw new InvalidOperationException(
                        "Template publication returned null.");

                Require(
                    published.ContentHash == canonicalTemplateHash,
                    "Published template hash is not reproducible from canonical content.");

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

                var organization = new OrganizationReference(
                    identityScopeId,
                    tenantId,
                    organizationId);

                clock.UtcNow = clock.UtcNow.AddMinutes(1);

                var profile = await profileDefinitions.CreateAsync(
                    OrganisationProfileId.New(),
                    organization,
                    new OrganisationProfileTemplatePin(
                        templateKey,
                        templateVersion),
                    CancellationToken.None);

                await RequireExceptionAsync<OrganisationProfileOrganizationNotFoundException>(
                    () => profileDefinitions.CreateAsync(
                        OrganisationProfileId.New(),
                        new OrganizationReference(
                            identityScopeId,
                            Guid.NewGuid(),
                            organizationId),
                        null,
                        CancellationToken.None),
                    "Cross-tenant Organization reference was unexpectedly accepted.");

                clock.UtcNow = clock.UtcNow.AddMinutes(1);

                var firstMutation = TryReplaceOverridesAsync(
                    overrideService,
                    profile.OrganisationProfileId,
                    profile.RowVersion,
                    2);

                var secondMutation = TryReplaceOverridesAsync(
                    overrideService,
                    profile.OrganisationProfileId,
                    profile.RowVersion,
                    3);

                var mutationResults = await Task.WhenAll(
                    firstMutation,
                    secondMutation);

                Require(
                    mutationResults.Count(item => item.Succeeded) == 1 &&
                    mutationResults.Count(item => item.ConcurrencyRejected) == 1,
                    "Concurrent override writers did not produce exactly one success and one stale-write rejection.");

                var afterRace = await profileStore.GetAsync(
                    profile.OrganisationProfileId,
                    CancellationToken.None)
                    ?? throw new InvalidOperationException(
                        "Profile disappeared after concurrency race.");

                Require(
                    afterRace.RowVersion == profile.RowVersion + 1,
                    "Concurrent override race advanced profile RowVersion unexpectedly.");

                var currentOverrides = await overrideStore.ListAsync(
                    profile.OrganisationProfileId,
                    CancellationToken.None);

                var manufacturing = currentOverrides.Single(item =>
                    item.DomainKey.Value == "manufacturing" &&
                    item.Operation == OrganisationProfileDomainOverrideOperation.Enable);

                var winningManufacturingVersion =
                    manufacturing.DomainVersion?.Value
                    ?? throw new InvalidOperationException(
                        "Winning manufacturing override has no version.");

                Require(
                    currentOverrides.Any(item =>
                        item.DomainKey.Value == "inventory" &&
                        item.Operation == OrganisationProfileDomainOverrideOperation.Disable),
                    "Winning override set is incomplete after concurrent replacement.");

                var snapshotTaskA = compositionService.ResolveAndSnapshotAsync(
                    profile.OrganisationProfileId,
                    afterRace.RowVersion,
                    CancellationToken.None);

                var snapshotTaskB = compositionService.ResolveAndSnapshotAsync(
                    profile.OrganisationProfileId,
                    afterRace.RowVersion,
                    CancellationToken.None);

                var concurrentSnapshots = await Task.WhenAll(
                    snapshotTaskA,
                    snapshotTaskB);

                var firstSnapshot = concurrentSnapshots[0]
                    ?? throw new InvalidOperationException(
                        "First concurrent snapshot returned null.");
                var duplicateSnapshot = concurrentSnapshots[1]
                    ?? throw new InvalidOperationException(
                        "Second concurrent snapshot returned null.");

                Require(
                    firstSnapshot.Version.Value == 1 &&
                    duplicateSnapshot.Version == firstSnapshot.Version &&
                    duplicateSnapshot.ContentHash == firstSnapshot.ContentHash,
                    "Concurrent identical snapshot publication was not idempotent.");

                var firstHistory = await effectiveVersionStore.ListAsync(
                    profile.OrganisationProfileId,
                    0,
                    100,
                    CancellationToken.None);

                Require(
                    firstHistory.Count == 1,
                    "Concurrent identical snapshot publication created duplicate semantic versions.");

                var alternateManufacturingVersion =
                    winningManufacturingVersion == 2 ? 3 : 2;

                clock.UtcNow = clock.UtcNow.AddMinutes(1);

                var changed = await overrideService.ReplaceAsync(
                    profile.OrganisationProfileId,
                    afterRace.RowVersion,
                    OverrideSet(alternateManufacturingVersion),
                    CancellationToken.None)
                    ?? throw new InvalidOperationException(
                        "Changed override replacement returned null.");

                var changedSnapshot = await compositionService.ResolveAndSnapshotAsync(
                    profile.OrganisationProfileId,
                    changed.RowVersion,
                    CancellationToken.None)
                    ?? throw new InvalidOperationException(
                        "Changed snapshot returned null.");

                Require(
                    changedSnapshot.Version.Value == 2 &&
                    changedSnapshot.ContentHash != firstSnapshot.ContentHash,
                    "Changed semantic content did not produce a distinct version/hash.");

                await RequireExceptionAsync<OrganisationProfileConcurrencyException>(
                    () => compositionService.ResolveAndSnapshotAsync(
                        profile.OrganisationProfileId,
                        afterRace.RowVersion,
                        CancellationToken.None),
                    "Stale profile RowVersion unexpectedly resolved after a semantic mutation.");

                clock.UtcNow = clock.UtcNow.AddMinutes(1);

                var reverted = await overrideService.ReplaceAsync(
                    profile.OrganisationProfileId,
                    changed.RowVersion,
                    OverrideSet(winningManufacturingVersion),
                    CancellationToken.None)
                    ?? throw new InvalidOperationException(
                        "Reverted override replacement returned null.");

                var revertedSnapshot = await compositionService.ResolveAndSnapshotAsync(
                    profile.OrganisationProfileId,
                    reverted.RowVersion,
                    CancellationToken.None)
                    ?? throw new InvalidOperationException(
                        "Reverted snapshot returned null.");

                Require(
                    revertedSnapshot.Version.Value == 3,
                    "Semantic reversion must append history rather than rewrite an old version.");

                Require(
                    revertedSnapshot.ContentHash == firstSnapshot.ContentHash,
                    "Equivalent effective content did not reproduce the original content hash.");

                var replayed = await compositionService.ResolveAndSnapshotAsync(
                    profile.OrganisationProfileId,
                    reverted.RowVersion,
                    CancellationToken.None)
                    ?? throw new InvalidOperationException(
                        "Repeated reverted snapshot returned null.");

                Require(
                    replayed.Version == revertedSnapshot.Version &&
                    replayed.ContentHash == revertedSnapshot.ContentHash,
                    "Repeated resolution of unchanged reverted content was not idempotent.");

                var finalHistory = await effectiveVersionStore.ListAsync(
                    profile.OrganisationProfileId,
                    0,
                    100,
                    CancellationToken.None);

                Require(
                    finalHistory.Count == 3 &&
                    finalHistory[0].Version.Value == 1 &&
                    finalHistory[1].Version.Value == 2 &&
                    finalHistory[2].Version.Value == 3,
                    "Immutable semantic version history is not monotonic and complete.");

                clock.UtcNow = clock.UtcNow.AddMinutes(1);

                var disabled = await profileDefinitions.SetStatusAsync(
                    profile.OrganisationProfileId,
                    OrganisationProfileStatus.Disabled,
                    reverted.RowVersion,
                    CancellationToken.None)
                    ?? throw new InvalidOperationException(
                        "Profile disable returned null.");

                await RequireExceptionAsync<OrganisationProfileInactiveException>(
                    () => compositionService.ResolveAndSnapshotAsync(
                        profile.OrganisationProfileId,
                        disabled.RowVersion,
                        CancellationToken.None),
                    "Disabled profile unexpectedly produced an effective snapshot.");

                clock.UtcNow = clock.UtcNow.AddMinutes(1);

                var reenabled = await profileDefinitions.SetStatusAsync(
                    profile.OrganisationProfileId,
                    OrganisationProfileStatus.Active,
                    disabled.RowVersion,
                    CancellationToken.None)
                    ?? throw new InvalidOperationException(
                        "Profile re-enable returned null.");

                var afterLifecycle = await compositionService.ResolveAndSnapshotAsync(
                    profile.OrganisationProfileId,
                    reenabled.RowVersion,
                    CancellationToken.None)
                    ?? throw new InvalidOperationException(
                        "Re-enabled profile resolution returned null.");

                Require(
                    afterLifecycle.Version == revertedSnapshot.Version &&
                    afterLifecycle.ContentHash == revertedSnapshot.ContentHash,
                    "Lifecycle-only changes unexpectedly changed semantic content/version.");

                var durableVersionCount = await CountSemanticVersionsAsync(
                    dataSource,
                    profile.OrganisationProfileId);

                Require(
                    durableVersionCount == 3,
                    "Durable semantic-version count differs from the read model.");

                Console.WriteLine(
                    "OrganisationProfile qualification and adversarial probe: GREEN");
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

        private static async Task<(
            bool Succeeded,
            bool ConcurrencyRejected)> TryReplaceOverridesAsync(
            OrganisationProfileDomainOverrideService service,
            OrganisationProfileId organisationProfileId,
            long expectedRowVersion,
            int manufacturingVersion)
        {
            try
            {
                var updated = await service.ReplaceAsync(
                    organisationProfileId,
                    expectedRowVersion,
                    OverrideSet(manufacturingVersion),
                    CancellationToken.None);

                return (updated is not null, false);
            }
            catch (OrganisationProfileConcurrencyException)
            {
                return (false, true);
            }
        }

        private static IReadOnlyList<OrganisationProfileDomainOverride>
            OverrideSet(int manufacturingVersion) =>
            new[]
            {
                Disable("inventory"),
                Enable("manufacturing", manufacturingVersion)
            };

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
                $"profile-qualification-{organizationId:N}"[..54];

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
                    'OrganisationProfile Qualification Probe',
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

        private static async Task<long> CountSemanticVersionsAsync(
            NpgsqlDataSource dataSource,
            OrganisationProfileId organisationProfileId)
        {
            await using var command = dataSource.CreateCommand("""
                SELECT count(*)
                FROM organisation_profile.organisation_profile_versions
                WHERE organisation_profile_id = @organisation_profile_id;
                """);

            command.Parameters.AddWithValue(
                "organisation_profile_id",
                NpgsqlDbType.Uuid,
                organisationProfileId.Value);

            var result = await command.ExecuteScalarAsync();

            return Convert.ToInt64(
                result,
                System.Globalization.CultureInfo.InvariantCulture);
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
                    $"Qualification probe cleanup warning: {cleanupException.Message}");
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
