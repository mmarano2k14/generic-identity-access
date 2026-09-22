using IdentityAccess.Application.Routing;
using IdentityAccess.Infrastructure.PostgreSql;
using IdentityAccess.Infrastructure.PostgreSql.Secrets;

namespace IdentityAccess.Tests.PostgreSql
{

    public sealed class EnvironmentConnectionSecretResolverTests
    {
        [Fact]
        public async Task Env_reference_resolves_without_exposing_value_in_reference_text()
        {
            const string key = "IDENTITY_ACCESS_TEST_CONNECTION_8F4D";
            const string value = "Host=127.0.0.1;Database=test;Username=test;Password=secret";
            Environment.SetEnvironmentVariable(key, value);
            try
            {
                var reference = new ConnectionSecretReference($"env:{key}");
                var resolved = await new EnvironmentConnectionSecretResolver().ResolveAsync(reference, TestContext.Current.CancellationToken);
                Assert.Equal(value, resolved);
                Assert.DoesNotContain(key, reference.ToString());
                Assert.DoesNotContain("secret", reference.ToString().ToLowerInvariant());
            }
            finally
            {
                Environment.SetEnvironmentVariable(key, null);
            }
        }

        [Fact]
        public async Task Missing_environment_secret_has_sanitized_failure()
        {
            var reference = new ConnectionSecretReference("env:IDENTITY_ACCESS_MISSING_TEST_CONNECTION_8F4D");
            Environment.SetEnvironmentVariable("IDENTITY_ACCESS_MISSING_TEST_CONNECTION_8F4D", null);
            var error = await Assert.ThrowsAsync<PostgreSqlStorageException>(async () =>
                await new EnvironmentConnectionSecretResolver().ResolveAsync(reference, TestContext.Current.CancellationToken));
            Assert.Equal(PostgreSqlStorageFailure.SecretNotFound, error.Failure);
            Assert.DoesNotContain("IDENTITY_ACCESS_MISSING", error.Message);
        }

        [Fact]
        public async Task Unsupported_scheme_is_rejected()
        {
            var reference = new ConnectionSecretReference("vault:identity/postgres");
            var error = await Assert.ThrowsAsync<PostgreSqlStorageException>(async () =>
                await new EnvironmentConnectionSecretResolver().ResolveAsync(reference, TestContext.Current.CancellationToken));
            Assert.Equal(PostgreSqlStorageFailure.UnsupportedSecretScheme, error.Failure);
        }
    }
}
