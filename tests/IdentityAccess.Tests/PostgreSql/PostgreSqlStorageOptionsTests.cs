using IdentityAccess.Infrastructure.PostgreSql;

namespace IdentityAccess.Tests.PostgreSql
{

    public sealed class PostgreSqlStorageOptionsTests
    {
        [Fact]
        public void Defaults_are_bounded_and_valid()
        {
            var options = new PostgreSqlStorageOptions();
            options.Validate();
            Assert.Equal(0, options.MinimumPoolSize);
            Assert.Equal(20, options.MaximumPoolSize);
        }

        [Theory]
        [InlineData(-1, 20, 10, 30)]
        [InlineData(21, 20, 10, 30)]
        [InlineData(0, 0, 10, 30)]
        [InlineData(0, 1001, 10, 30)]
        [InlineData(0, 20, 0, 30)]
        [InlineData(0, 20, 301, 30)]
        [InlineData(0, 20, 10, 0)]
        [InlineData(0, 20, 10, 3601)]
        public void Invalid_limits_fail_closed(int min, int max, int timeout, int commandTimeout)
        {
            var options = new PostgreSqlStorageOptions
            {
                MinimumPoolSize = min,
                MaximumPoolSize = max,
                ConnectionTimeoutSeconds = timeout,
                CommandTimeoutSeconds = commandTimeout
            };
            var error = Assert.Throws<PostgreSqlStorageException>(options.Validate);
            Assert.Equal(PostgreSqlStorageFailure.InvalidConfiguration, error.Failure);
        }
    }
}
