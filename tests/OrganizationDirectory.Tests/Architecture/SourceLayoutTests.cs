using System.Text.RegularExpressions;

namespace OrganizationDirectory.Tests.Architecture
{
    public sealed class SourceLayoutTests
    {
        private static readonly Regex TypeDeclarationPattern = new(
            @"^\s*(?:(?:public|internal|private|protected|file|sealed|abstract|static|partial|readonly|ref|unsafe|new)\s+)*(?:record\s+(?:class\s+|struct\s+)?|class\s+|struct\s+|interface\s+|enum\s+)(?<name>[A-Za-z_][A-Za-z0-9_]*)",
            RegexOptions.Multiline | RegexOptions.CultureInvariant | RegexOptions.Compiled);

        private static readonly Regex FileScopedNamespacePattern = new(
            @"^\s*namespace\s+[A-Za-z_][A-Za-z0-9_]*(?:\.[A-Za-z_][A-Za-z0-9_]*)*\s*;\s*$",
            RegexOptions.Multiline | RegexOptions.CultureInvariant | RegexOptions.Compiled);

        [Fact]
        public void Every_source_file_contains_at_most_one_declared_type_matches_its_file_name_and_uses_block_scoped_namespaces()
        {
            var root = FindRepositoryRoot();
            var sourceRoots = new[] { Path.Combine(root, "src"), Path.Combine(root, "tests") };
            var failures = new List<string>();

            foreach (var sourceRoot in sourceRoots)
            {
                foreach (var file in Directory.EnumerateFiles(sourceRoot, "*.cs", SearchOption.AllDirectories))
                {
                    if (IsBuildArtifact(file)) continue;
                    var source = File.ReadAllText(file);

                    if (FileScopedNamespacePattern.IsMatch(source))
                        failures.Add($"{Path.GetRelativePath(root, file)} uses a file-scoped namespace; block-scoped namespaces are required.");

                    var declarations = TypeDeclarationPattern.Matches(source)
                        .Select(match => match.Groups["name"].Value)
                        .ToArray();

                    if (declarations.Length > 1)
                    {
                        failures.Add($"{Path.GetRelativePath(root, file)} declares multiple types: {string.Join(", ", declarations)}");
                        continue;
                    }

                    if (declarations.Length == 1 &&
                        !string.Equals(Path.GetFileNameWithoutExtension(file), declarations[0], StringComparison.Ordinal))
                    {
                        failures.Add($"{Path.GetRelativePath(root, file)} must be named {declarations[0]}.cs");
                    }
                }
            }

            Assert.True(failures.Count == 0, string.Join(Environment.NewLine, failures));
        }

        private static bool IsBuildArtifact(string file) =>
            file.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase) ||
            file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase);

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
