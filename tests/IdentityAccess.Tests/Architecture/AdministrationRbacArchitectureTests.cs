namespace IdentityAccess.Tests.Architecture
{
    /// <summary>
    /// Protects the administration authorization bridge from caller-controlled RBAC context and
    /// local wildcard reimplementation.
    /// </summary>
    public sealed class AdministrationRbacArchitectureTests
    {
        /// <summary>
        /// Verifies RBAC project/namespace and adapter directory come from trusted server
        /// configuration rather than HTTP request values.
        /// </summary>
        [Fact]
        public void Administration_rbac_context_is_server_configured()
        {
            var source = Read(
                "src",
                "IdentityAccess.Api",
                "Security",
                "AdministrationAuthorizationServiceRegistration.cs");

            Assert.Contains(
                "IdentityAccess:Authorization",
                source,
                StringComparison.Ordinal);

            Assert.Contains(
                "RbacProject",
                source,
                StringComparison.Ordinal);

            Assert.Contains(
                "RbacNamespace",
                source,
                StringComparison.Ordinal);

            Assert.Contains(
                "ReferenceDirectory",
                source,
                StringComparison.Ordinal);

            Assert.Contains(
                "ProbeCompatibility",
                source,
                StringComparison.Ordinal);

            Assert.Contains(
                "compatibility.IsCompatible",
                source,
                StringComparison.Ordinal);
        }

        /// <summary>
        /// Verifies the HTTP authorization bridge delegates to Identity Authorization rather than
        /// evaluating wildcard grants itself.
        /// </summary>
        [Fact]
        public void Administration_authorizer_delegates_wildcard_decision()
        {
            var source = Read(
                "src",
                "IdentityAccess.Api",
                "Security",
                "RbacAdministrationRequestAuthorizer.cs");

            Assert.Contains(
                "IIdentityAuthorizationService",
                source,
                StringComparison.Ordinal);

            Assert.DoesNotContain(
                "Split(':')",
                source,
                StringComparison.Ordinal);

            Assert.DoesNotContain(
                "StartsWith",
                source,
                StringComparison.Ordinal);
        }

        private static string Read(
            params string[] segments)
        {
            var current =
                new DirectoryInfo(
                    AppContext.BaseDirectory);

            while (current is not null)
            {
                if (File.Exists(
                        Path.Combine(
                            current.FullName,
                            "IdentityAccess.sln")))
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
