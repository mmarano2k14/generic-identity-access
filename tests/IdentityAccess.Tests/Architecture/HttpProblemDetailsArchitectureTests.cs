namespace IdentityAccess.Tests.Architecture
{
    /// <summary>
    /// Protects centralized HTTP problem handling from regressing into controller-local
    /// problem-details construction and exception mapping.
    /// </summary>
    public sealed class HttpProblemDetailsArchitectureTests
    {
        /// <summary>
        /// Verifies that controllers and authorization filters do not construct
        /// <c>ProblemDetails</c> directly.
        /// </summary>
        [Fact]
        public void Api_entry_points_do_not_construct_problem_details_directly()
        {
            var root = FindRepositoryRoot();
            var folders = new[]
            {
                Path.Combine(root, "src", "IdentityAccess.Api", "Controllers"),
                Path.Combine(root, "src", "IdentityAccess.Api", "Security")
            };

            foreach (var folder in folders)
            {
                foreach (var file in Directory.EnumerateFiles(folder, "*.cs"))
                {
                    var source = File.ReadAllText(file);
                    Assert.DoesNotContain("new ProblemDetails", source, StringComparison.Ordinal);
                }
            }
        }

        /// <summary>
        /// Verifies that optimistic-concurrency exceptions are not translated independently
        /// by individual MVC controllers.
        /// </summary>
        [Fact]
        public void Controllers_do_not_catch_identity_concurrency_exceptions()
        {
            var root = FindRepositoryRoot();
            var folder = Path.Combine(root, "src", "IdentityAccess.Api", "Controllers");

            foreach (var file in Directory.EnumerateFiles(folder, "*Controller.cs"))
            {
                var source = File.ReadAllText(file);
                Assert.DoesNotContain(
                    "catch (IdentityConcurrencyException",
                    source,
                    StringComparison.Ordinal);
            }
        }

        /// <summary>
        /// Verifies that the API host registers the centralized exception handler.
        /// </summary>
        [Fact]
        public void Program_registers_central_exception_handler()
        {
            var root = FindRepositoryRoot();
            var program = File.ReadAllText(
                Path.Combine(root, "src", "IdentityAccess.Api", "Program.cs"));

            Assert.Contains(
                "AddExceptionHandler<ApiExceptionHandler>()",
                program,
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
