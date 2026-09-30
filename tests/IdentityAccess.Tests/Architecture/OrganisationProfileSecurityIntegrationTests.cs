using IdentityAccess.Api.Security;
using IdentityAccess.Application.Security;
using IdentityAccess.Domain;

namespace IdentityAccess.Tests.Architecture
{
    /// <summary>
    /// Protects OrganisationProfile authorization metadata, audit vocabulary, and host integration
    /// without moving Identity Access dependencies into reusable OrganisationProfile projects.
    /// </summary>
    public sealed class OrganisationProfileSecurityIntegrationTests
    {
        [Fact]
        public void Capability_catalog_uses_dedicated_resource_and_concrete_segments()
        {
            Assert.Equal(
                IdentityAccessAdministrationCapabilities.Resource,
                OrganisationProfileAdministrationCapabilities.Resource);

            var features = new[]
            {
                OrganisationProfileAdministrationCapabilities.Profiles,
                OrganisationProfileAdministrationCapabilities.Templates,
                OrganisationProfileAdministrationCapabilities.DomainOverrides,
                OrganisationProfileAdministrationCapabilities.EffectiveVersions
            };

            Assert.Equal(
                features.Length,
                features.Distinct(StringComparer.Ordinal).Count());

            foreach (var feature in features)
            {
                _ = new CapabilityKey(
                    OrganisationProfileAdministrationCapabilities.Resource,
                    feature,
                    OrganisationProfileAdministrationCapabilities.Read);

                _ = new CapabilityKey(
                    OrganisationProfileAdministrationCapabilities.Resource,
                    feature,
                    OrganisationProfileAdministrationCapabilities.Write);
            }
        }

        [Fact]
        public void Audit_event_ids_are_stable_and_append_only()
        {
            Assert.Equal(
                79,
                (int)SecurityAuditEventType.OrganisationProfileCreated);
            Assert.Equal(
                80,
                (int)SecurityAuditEventType.OrganisationProfileUpdated);
            Assert.Equal(
                81,
                (int)SecurityAuditEventType.OrganisationProfileStatusChanged);
            Assert.Equal(
                82,
                (int)SecurityAuditEventType.OrganisationProfileTemplatePinChanged);
            Assert.Equal(
                83,
                (int)SecurityAuditEventType.OrganisationProfileDomainOverridesReplaced);
            Assert.Equal(
                84,
                (int)SecurityAuditEventType.OrganisationProfileEffectiveVersionResolved);
            Assert.Equal(
                85,
                (int)SecurityAuditEventType.OrganisationProfileTemplateCreated);
            Assert.Equal(
                86,
                (int)SecurityAuditEventType.OrganisationProfileTemplateUpdated);
            Assert.Equal(
                87,
                (int)SecurityAuditEventType.OrganisationProfileTemplateStatusChanged);
            Assert.Equal(
                88,
                (int)SecurityAuditEventType.OrganisationProfileTemplateDraftCreated);
            Assert.Equal(
                89,
                (int)SecurityAuditEventType.OrganisationProfileTemplateDraftCompositionReplaced);
            Assert.Equal(
                90,
                (int)SecurityAuditEventType.OrganisationProfileTemplateVersionPublished);
            Assert.Equal(
                91,
                (int)SecurityAuditEventType.OrganisationProfileTemplateVersionRetired);
        }

        [Fact]
        public void Api_host_registers_profile_module_without_reversing_module_dependencies()
        {
            var root = FindRepositoryRoot();

            var program = Read(
                root,
                "src",
                "IdentityAccess.Api",
                "Program.cs");

            Assert.Contains(
                "builder.AddOrganisationProfile();",
                program,
                StringComparison.Ordinal);

            Assert.True(
                program.IndexOf(
                    "builder.AddOrganizationDirectory();",
                    StringComparison.Ordinal) <
                program.IndexOf(
                    "builder.AddOrganisationProfile();",
                    StringComparison.Ordinal));

            var registration = Read(
                root,
                "src",
                "IdentityAccess.Api",
                "OrganisationProfileRegistration.cs");

            Assert.Contains(
                "TryAddSingleton",
                registration,
                StringComparison.Ordinal);
            Assert.Contains(
                "UnavailableDomainRegistryReader",
                registration,
                StringComparison.Ordinal);
            Assert.Contains(
                "PostgreSqlOrganisationProfileStore",
                registration,
                StringComparison.Ordinal);
            Assert.Contains(
                "PostgreSqlOrganisationProfileTemplateVersionStore",
                registration,
                StringComparison.Ordinal);
            Assert.Contains(
                "PostgreSqlOrganisationProfileDomainOverrideStore",
                registration,
                StringComparison.Ordinal);
            Assert.Contains(
                "PostgreSqlOrganisationProfileVersionStore",
                registration,
                StringComparison.Ordinal);
            Assert.Contains(
                "IOrganisationProfileSecurityAuditWriter",
                registration,
                StringComparison.Ordinal);

            foreach (var project in new[]
            {
                "OrganisationProfile.Domain",
                "OrganisationProfile.Application",
                "OrganisationProfile.Infrastructure.PostgreSql"
            })
            {
                var projectSource = Read(
                    root,
                    "src",
                    project,
                    $"{project}.csproj");

                Assert.DoesNotContain(
                    "IdentityAccess.",
                    projectSource,
                    StringComparison.Ordinal);
            }
        }

        [Fact]
        public void Audit_bridge_uses_existing_routed_security_audit_pipeline_only()
        {
            var root = FindRepositoryRoot();

            var source = Read(
                root,
                "src",
                "IdentityAccess.Api",
                "Security",
                "OrganisationProfileSecurityAuditWriter.cs");

            Assert.Contains(
                "IDatabaseRouteResolver",
                source,
                StringComparison.Ordinal);
            Assert.Contains(
                "ISecurityAuditWriter",
                source,
                StringComparison.Ordinal);
            Assert.Contains(
                "SecurityAuditEvent",
                source,
                StringComparison.Ordinal);
            Assert.Contains(
                "SecurityAuditOutcome.Succeeded",
                source,
                StringComparison.Ordinal);

            Assert.DoesNotContain(
                "IsAllowed(",
                source,
                StringComparison.Ordinal);
            Assert.DoesNotContain(
                "authorization.evaluate",
                source,
                StringComparison.Ordinal);
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
