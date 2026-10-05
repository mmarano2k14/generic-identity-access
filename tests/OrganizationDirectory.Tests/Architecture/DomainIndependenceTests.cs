namespace OrganizationDirectory.Tests.Architecture
{
    public sealed class DomainIndependenceTests
    {
        [Fact]
        public void Domain_source_does_not_embed_consumer_specific_semantics()
        {
            var root = FindRepositoryRoot();
            var domainRoot = Path.Combine(root, "src", "OrganizationDirectory.Domain");
            var prohibited = new[]
            {
                "consumer-app",
                "ecommerce",
                "restaurant",
                "shopify",
                "stripe",
                "businessprofile"
            };
            var failures = new List<string>();

            foreach (var file in Directory.EnumerateFiles(domainRoot, "*.cs", SearchOption.AllDirectories))
            {
                var source = File.ReadAllText(file);
                foreach (var term in prohibited)
                {
                    if (source.Contains(term, StringComparison.OrdinalIgnoreCase))
                        failures.Add($"{Path.GetRelativePath(root, file)} contains prohibited consumer-specific term '{term}'.");
                }
            }

            Assert.True(failures.Count == 0, string.Join(Environment.NewLine, failures));
        }

        private static string FindRepositoryRoot()
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);
            while (directory is not null)
            {
                if (File.Exists(Path.Combine(directory.FullName, "IdentityAccess.sln"))) return directory.FullName;
                directory = directory.Parent;
            }
            throw new InvalidOperationException("Repository root containing IdentityAccess.sln was not found.");
        }
    }
}
