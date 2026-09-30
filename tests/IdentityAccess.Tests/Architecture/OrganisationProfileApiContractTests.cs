namespace IdentityAccess.Tests.Architecture
{
    /// <summary>Protects OrganisationProfile HTTP, RBAC, audit, tenant-boundary, and SDK integration.</summary>
    public sealed class OrganisationProfileApiContractTests
    {
        [Fact]
        public void Controllers_use_focused_capabilities_and_tenant_boundary_guards()
        {
            var root = FindRepositoryRoot();

            var profiles = Read(
                root,
                "src",
                "IdentityAccess.Api",
                "Controllers",
                "OrganisationProfilesController.cs");

            Assert.Contains(
                "OrganisationProfileAdministrationCapabilities.Profiles",
                profiles,
                StringComparison.Ordinal);
            Assert.Contains(
                "OrganisationProfileTenantBoundary.Matches",
                profiles,
                StringComparison.Ordinal);
            Assert.Contains(
                "SecurityAuditEventType.OrganisationProfileCreated",
                profiles,
                StringComparison.Ordinal);
            Assert.Contains(
                "SecurityAuditEventType.OrganisationProfileTemplatePinChanged",
                profiles,
                StringComparison.Ordinal);
            Assert.Contains(
                "SecurityAuditEventType.OrganisationProfileStatusChanged",
                profiles,
                StringComparison.Ordinal);

            var overrides = Read(
                root,
                "src",
                "IdentityAccess.Api",
                "Controllers",
                "OrganisationProfileDomainOverridesController.cs");

            Assert.Contains(
                "OrganisationProfileAdministrationCapabilities.DomainOverrides",
                overrides,
                StringComparison.Ordinal);
            Assert.Contains(
                "OrganisationProfileTenantBoundary.Matches",
                overrides,
                StringComparison.Ordinal);

            var effective = Read(
                root,
                "src",
                "IdentityAccess.Api",
                "Controllers",
                "OrganisationProfileEffectiveVersionsController.cs");

            Assert.Contains(
                "OrganisationProfileAdministrationCapabilities.EffectiveVersions",
                effective,
                StringComparison.Ordinal);
            Assert.Contains(
                "OrganisationProfileTenantBoundary.Matches",
                effective,
                StringComparison.Ordinal);
        }

        [Fact]
        public void Template_api_uses_scope_administration_and_emits_semantic_audit_events()
        {
            var root = FindRepositoryRoot();

            var templates = Read(
                root,
                "src",
                "IdentityAccess.Api",
                "Controllers",
                "OrganisationProfileTemplatesController.cs");

            Assert.Contains(
                "OrganisationProfileAdministrationCapabilities.Templates",
                templates,
                StringComparison.Ordinal);
            Assert.Contains(
                "tenantId: null",
                templates,
                StringComparison.Ordinal);

            var versions = Read(
                root,
                "src",
                "IdentityAccess.Api",
                "Controllers",
                "OrganisationProfileTemplateVersionsController.cs");

            Assert.Contains(
                "OrganisationProfileTemplateDraftCreated",
                versions,
                StringComparison.Ordinal);
            Assert.Contains(
                "OrganisationProfileTemplateVersionPublished",
                versions,
                StringComparison.Ordinal);
            Assert.Contains(
                "OrganisationProfileTemplateVersionRetired",
                versions,
                StringComparison.Ordinal);
        }

        [Fact]
        public void Api_exception_handler_maps_profile_conflicts_without_leaking_internal_details()
        {
            var root = FindRepositoryRoot();

            var handler = Read(
                root,
                "src",
                "IdentityAccess.Api",
                "Http",
                "ApiExceptionHandler.cs");

            Assert.Contains(
                "OrganisationProfileConcurrencyException",
                handler,
                StringComparison.Ordinal);
            Assert.Contains(
                "OrganisationProfileTemplateVersionImmutableException",
                handler,
                StringComparison.Ordinal);
            Assert.Contains(
                "DomainRegistryVersionUnavailableException",
                handler,
                StringComparison.Ordinal);
            Assert.Contains(
                "StatusCodes.Status409Conflict",
                handler,
                StringComparison.Ordinal);
        }

        [Fact]
        public void TypeScript_sdk_remains_split_into_focused_clients()
        {
            var root = FindRepositoryRoot();

            var administration = Read(
                root,
                "clients",
                "typescript",
                "src",
                "client",
                "administration",
                "IdentityAccessAdministrationClient.ts");

            foreach (var marker in new[]
            {
                "organisationProfiles",
                "organisationProfileDomainOverrides",
                "organisationProfileEffectiveVersions",
                "organisationProfileTemplates",
                "organisationProfileTemplateVersions"
            })
            {
                Assert.Contains(
                    marker,
                    administration,
                    StringComparison.Ordinal);
            }

            Assert.True(
                File.Exists(
                    Path.Combine(
                        root,
                        "clients",
                        "typescript",
                        "src",
                        "client",
                        "administration",
                        "IdentityAccessOrganisationProfilesClient.ts")));

            Assert.True(
                File.Exists(
                    Path.Combine(
                        root,
                        "clients",
                        "typescript",
                        "src",
                        "client",
                        "administration",
                        "IdentityAccessOrganisationProfileTemplateVersionsClient.ts")));
        }

        private static string Read(
            string root,
            params string[] parts) =>
            File.ReadAllText(
                Path.Combine(
                    new[] { root }.Concat(parts).ToArray()));

        private static string FindRepositoryRoot()
        {
            var current =
                new DirectoryInfo(AppContext.BaseDirectory);

            while (current is not null)
            {
                if (File.Exists(
                    Path.Combine(
                        current.FullName,
                        "IdentityAccess.sln")))
                {
                    return current.FullName;
                }

                current = current.Parent;
            }

            throw new InvalidOperationException(
                "Repository root could not be located.");
        }
    }
}
