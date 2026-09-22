using Microsoft.AspNetCore.Mvc;

namespace IdentityAccess.Tests.Architecture
{
    /// <summary>
    /// Protects controller construction from regressing to the service-locator pattern.
    /// </summary>
    public sealed class ControllerDependencyInjectionTests
    {
        /// <summary>
        /// Verifies that no controller constructor accepts <see cref="IServiceProvider"/>.
        /// </summary>
        [Fact]
        public void Controllers_do_not_accept_service_provider()
        {
            var controllers = typeof(Program).Assembly
                .GetTypes()
                .Where(type =>
                    !type.IsAbstract &&
                    typeof(ControllerBase).IsAssignableFrom(type) &&
                    type.Namespace == "IdentityAccess.Api.Controllers")
                .ToArray();

            Assert.NotEmpty(controllers);

            foreach (var controller in controllers)
            {
                foreach (var constructor in controller.GetConstructors())
                {
                    Assert.DoesNotContain(
                        constructor.GetParameters(),
                        parameter => parameter.ParameterType == typeof(IServiceProvider));
                }
            }
        }

        /// <summary>
        /// Verifies that controller source files do not resolve optional application services
        /// through <c>IServiceProvider</c> or <c>HttpContext.RequestServices</c>.
        /// </summary>
        [Fact]
        public void Controller_sources_do_not_use_service_location()
        {
            var root = FindRepositoryRoot();
            var controllers = Path.Combine(root, "src", "IdentityAccess.Api", "Controllers");

            foreach (var file in Directory.EnumerateFiles(controllers, "*Controller.cs"))
            {
                var source = File.ReadAllText(file);

                Assert.DoesNotContain("IServiceProvider", source, StringComparison.Ordinal);
                Assert.DoesNotContain(".GetService<", source, StringComparison.Ordinal);
                Assert.DoesNotContain("RequestServices", source, StringComparison.Ordinal);
            }
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
