using System.Text.Json;

namespace IdentityAccess.Tests.Architecture
{
    /// <summary>Protects the published administration security model required by Organization Directory.</summary>
    public sealed class OrganizationSecurityManifestTests
    {
        [Fact]
        public void Admin_manifest_declares_versioned_organization_security_capabilities()
        {
            var root = FindRepositoryRoot();
            var path = Path.Combine(
                root,
                "config",
                "identity-access-admin-security-manifest.json");

            using var document = JsonDocument.Parse(File.ReadAllText(path));
            var rootElement = document.RootElement;

            Assert.Equal(
                4,
                rootElement.GetProperty("modelVersion").GetInt32());

            var identityResource = rootElement
                .GetProperty("resources")
                .EnumerateArray()
                .Single(item =>
                    item.GetProperty("name").GetString() == "identity-access");

            var features = identityResource
                .GetProperty("features")
                .EnumerateArray()
                .ToDictionary(
                    item => item.GetProperty("name").GetString()!,
                    item => item,
                    StringComparer.Ordinal);

            AssertReadWrite(features, "organization");
            AssertReadWrite(features, "organization-membership");
            AssertReadWrite(features, "organization-scope-link");
        }

        private static void AssertReadWrite(
            IReadOnlyDictionary<string, JsonElement> features,
            string feature)
        {
            Assert.True(features.TryGetValue(feature, out var definition));

            var actions = definition
                .GetProperty("actions")
                .EnumerateArray()
                .Select(item => item.GetProperty("name").GetString())
                .ToArray();

            Assert.Contains("read", actions);
            Assert.Contains("write", actions);
        }

        private static string FindRepositoryRoot()
        {
            var current = new DirectoryInfo(AppContext.BaseDirectory);

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
