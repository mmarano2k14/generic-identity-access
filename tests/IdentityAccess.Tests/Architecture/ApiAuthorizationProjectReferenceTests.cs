using System.Xml.Linq;

namespace IdentityAccess.Tests.Architecture
{
    /// <summary>
    /// Protects the API composition root from losing the authorization/RBAC project references it
    /// requires to register real administration authorization.
    /// </summary>
    public sealed class ApiAuthorizationProjectReferenceTests
    {
        /// <summary>
        /// Verifies the API directly references the authorization contract/orchestration assembly,
        /// the neutral RBAC boundary, and the external adapter assembly.
        /// </summary>
        [Fact]
        public void Api_project_contains_required_authorization_references()
        {
            var root = FindRepositoryRoot();
            var projectPath = Path.Combine(
                root,
                "src",
                "IdentityAccess.Api",
                "IdentityAccess.Api.csproj");

            var document = XDocument.Load(projectPath);

            var references = document
                .Descendants("ProjectReference")
                .Select(
                    element => element
                        .Attribute("Include")?
                        .Value
                        .Replace('\\', '/'))
                .Where(value => value is not null)
                .Cast<string>()
                .ToHashSet(StringComparer.Ordinal);

            Assert.Contains(
                "../IdentityAccess.Authorization/IdentityAccess.Authorization.csproj",
                references);

            Assert.Contains(
                "../IdentityAccess.Rbac/IdentityAccess.Rbac.csproj",
                references);

            Assert.Contains(
                "../IdentityAccess.Rbac.MultiplexedAdapter/IdentityAccess.Rbac.MultiplexedAdapter.csproj",
                references);
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
