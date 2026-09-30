namespace IdentityAccess.Tests.Architecture
{
    /// <summary>
    /// Protects pre-existing Identity Access API helpers required by Organization Directory
    /// while OrganisationProfile host integration evolves.
    /// </summary>
    public sealed class OrganisationProfileHostRegressionTests
    {
        [Fact]
        public void ApiProblems_preserves_existing_organization_directory_helpers()
        {
            var root = FindRepositoryRoot();
            var source = File.ReadAllText(
                Path.Combine(
                    root,
                    "src",
                    "IdentityAccess.Api",
                    "Http",
                    "ApiProblems.cs"));

            Assert.Contains(
                "UnprocessableEntity(",
                source,
                StringComparison.Ordinal);

            Assert.Contains(
                "OrganizationMembershipAdministrationUnavailable()",
                source,
                StringComparison.Ordinal);

            Assert.Contains(
                "OrganizationResourceScopeLinkAdministrationUnavailable()",
                source,
                StringComparison.Ordinal);
        }

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
