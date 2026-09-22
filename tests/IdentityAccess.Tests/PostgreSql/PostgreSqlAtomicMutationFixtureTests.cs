using System.Text.RegularExpressions;

namespace IdentityAccess.Tests.PostgreSql
{
    /// <summary>
    /// Verifies that PostgreSQL atomic-mutation validation fixtures use valid UUID literals.
    /// </summary>
    public sealed class PostgreSqlAtomicMutationFixtureTests
    {
        /// <summary>
        /// Verifies every UUID-like literal in the atomic mutation SQL fixture can be parsed by
        /// <see cref="Guid"/>.
        /// </summary>
        [Fact]
        public void Atomic_mutation_fixture_uses_valid_uuid_literals()
        {
            var root = FindRepositoryRoot();
            var path = Path.Combine(
                root,
                "scripts",
                "postgresql",
                "validate-atomic-mutations.sql");

            var source = File.ReadAllText(path);
            var matches = Regex.Matches(
                source,
                @"[0-9A-Fa-f]{8}-[0-9A-Fa-f]{4}-[0-9A-Fa-f]{4}-[0-9A-Fa-f]{4}-[0-9A-Fa-f]{12}");

            Assert.NotEmpty(matches);

            foreach (Match match in matches)
            {
                Assert.True(
                    Guid.TryParseExact(match.Value, "D", out _),
                    $"Invalid UUID fixture: {match.Value}");
            }

            Assert.DoesNotContain(
                "25aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
                source,
                StringComparison.Ordinal);
        }

        private static string FindRepositoryRoot()
        {
            var current = new DirectoryInfo(AppContext.BaseDirectory);

            while (current is not null)
            {
                if (File.Exists(Path.Combine(current.FullName, "IdentityAccess.sln")))
                {
                    return current.FullName;
                }

                current = current.Parent;
            }

            throw new InvalidOperationException("Repository root could not be located.");
        }
    }
}
