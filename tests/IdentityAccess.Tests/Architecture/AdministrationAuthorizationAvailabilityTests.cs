using IdentityAccess.Api.Security;

namespace IdentityAccess.Tests.Architecture
{
    /// <summary>
    /// Verifies that authenticated-context establishment does not falsely advertise RBAC
    /// capability authorization as available.
    /// </summary>
    public sealed class AdministrationAuthorizationAvailabilityTests
    {
        /// <summary>
        /// Verifies the session-backed authorizer explicitly remains capability-unavailable until
        /// the authorization service is connected.
        /// </summary>
        [Fact]
        public void Session_backed_authorizer_remains_fail_closed_for_capabilities()
        {
            var source = Read(
                "src",
                "IdentityAccess.Api",
                "Security",
                "SessionBackedAdministrationRequestAuthorizer.cs");

            Assert.Contains(
                "CapabilityAuthorizationAvailable => false",
                source,
                StringComparison.Ordinal);

            Assert.Contains(
                "AdministrationAccessFailureCode.AuthorizationUnavailable",
                source,
                StringComparison.Ordinal);
        }

        private static string Read(params string[] segments)
        {
            var current = new DirectoryInfo(AppContext.BaseDirectory);

            while (current is not null)
            {
                if (File.Exists(Path.Combine(current.FullName, "IdentityAccess.sln")))
                {
                    return File.ReadAllText(
                        Path.Combine(
                            new[] { current.FullName }
                                .Concat(segments)
                                .ToArray()));
                }

                current = current.Parent;
            }

            throw new InvalidOperationException(
                "Repository root could not be located.");
        }
    }
}
