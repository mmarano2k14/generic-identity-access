using System.Text.RegularExpressions;

namespace IdentityAccess.Tests.Architecture
{
    public sealed class CancellationTokenContractTests
    {
        private static readonly Regex OptionalCancellationTokenPattern = new(
            @"CancellationToken\s+[A-Za-z_][A-Za-z0-9_]*\s*=\s*default\b",
            RegexOptions.CultureInvariant | RegexOptions.Compiled);

        [Fact]
        public void Production_source_requires_explicit_cancellation_tokens()
        {
            var root = FindRepositoryRoot();
            var sourceRoot = Path.Combine(root, "src");
            var failures = new List<string>();

            foreach (var file in Directory.EnumerateFiles(sourceRoot, "*.cs", SearchOption.AllDirectories))
            {
                if (IsBuildArtifact(file))
                    continue;

                var source = File.ReadAllText(file);
                if (OptionalCancellationTokenPattern.IsMatch(source))
                    failures.Add(Path.GetRelativePath(root, file));
            }

            Assert.True(
                failures.Count == 0,
                "Production I/O contracts must require explicit CancellationToken values. Optional tokens were found in:" +
                Environment.NewLine + string.Join(Environment.NewLine, failures));
        }

        private static bool IsBuildArtifact(string file) =>
            file.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase) ||
            file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase);

        private static string FindRepositoryRoot()
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);
            while (directory is not null)
            {
                if (File.Exists(Path.Combine(directory.FullName, "IdentityAccess.sln")))
                    return directory.FullName;

                directory = directory.Parent;
            }

            throw new InvalidOperationException("Repository root containing IdentityAccess.sln was not found.");
        }
    }
}
