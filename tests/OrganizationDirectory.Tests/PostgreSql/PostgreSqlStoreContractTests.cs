using OrganizationDirectory.Application.Storage;
using OrganizationDirectory.Infrastructure.PostgreSql;

namespace OrganizationDirectory.Tests.PostgreSql
{
    public sealed class PostgreSqlStoreContractTests
    {
        [Fact]
        public void PostgreSql_store_implements_application_persistence_contract()
        {
            Assert.True(typeof(IOrganizationStore).IsAssignableFrom(typeof(PostgreSqlOrganizationStore)));
        }
    }
}
