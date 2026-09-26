using IdentityAccess.Application.Administration;
using IdentityAccess.Domain;

namespace IdentityAccess.Tests
{
    public sealed class ApplicationSecurityManifestTests
    {
        [Fact]
        public void Manifest_normalizes_rbac_context_and_orders_capabilities()
        {
            var manifest = new ApplicationSecurityManifest(
                1,
                new ApplicationKey("app-a"),
                3,
                " Example Project ",
                new[] { "BackOffice", " crm ", "crm" },
                new[]
                {
                    Capability("billing", "invoice", "refund", "Refund invoices"),
                    Capability("billing", "invoice", "read", "Read invoices")
                });

            Assert.Equal("example project", manifest.RbacProject);
            Assert.Equal(new[] { "backoffice", "crm" }, manifest.RbacNamespaces);
            Assert.Equal("read", manifest.Capabilities[0].Key.Action);
            Assert.Equal("refund", manifest.Capabilities[1].Key.Action);
        }

        [Fact]
        public void Manifest_rejects_duplicate_concrete_capabilities()
        {
            Assert.Throws<ArgumentException>(() => new ApplicationSecurityManifest(
                1,
                new ApplicationKey("app-a"),
                1,
                "project-a",
                new[] { "namespace-a" },
                new[]
                {
                    Capability("billing", "invoice", "read", "Read invoices"),
                    Capability("billing", "invoice", "read", "Read invoices again")
                }));
        }

        [Fact]
        public void Fingerprint_is_stable_across_input_order()
        {
            var fingerprint = new ApplicationSecurityManifestFingerprint();
            var left = new ApplicationSecurityManifest(
                1,
                new ApplicationKey("app-a"),
                2,
                "project-a",
                new[] { "namespace-b", "namespace-a" },
                new[]
                {
                    Capability("billing", "invoice", "refund", "Refund invoices"),
                    Capability("billing", "invoice", "read", "Read invoices")
                });
            var right = new ApplicationSecurityManifest(
                1,
                new ApplicationKey("app-a"),
                2,
                "project-a",
                new[] { "namespace-a", "namespace-b" },
                new[]
                {
                    Capability("billing", "invoice", "read", "Read invoices"),
                    Capability("billing", "invoice", "refund", "Refund invoices")
                });

            Assert.Equal(fingerprint.Compute(left), fingerprint.Compute(right));
        }

        [Fact]
        public void Registered_model_projects_manifest_capabilities_without_adding_namespace_to_capability_key()
        {
            var manifest = new ApplicationSecurityManifest(
                1,
                new ApplicationKey("app-a"),
                1,
                "project-a",
                new[] { "crm" },
                new[] { Capability("billing", "invoice", "read", "Read invoices") });
            var fingerprint = new ApplicationSecurityManifestFingerprint().Compute(manifest);
            var registered = new RegisteredApplicationSecurityModel(Guid.NewGuid(), manifest, fingerprint);

            var capability = Assert.Single(registered.Capabilities);
            Assert.Equal("billing", capability.Key.Resource);
            Assert.Equal("invoice", capability.Key.Feature);
            Assert.Equal("read", capability.Key.Action);
            Assert.Equal("crm", Assert.Single(registered.Manifest.RbacNamespaces));
        }

        private static ApplicationSecurityManifestCapability Capability(
            string resource,
            string feature,
            string action,
            string displayName) =>
            new(new CapabilityKey(resource, feature, action), displayName);
    }
}
