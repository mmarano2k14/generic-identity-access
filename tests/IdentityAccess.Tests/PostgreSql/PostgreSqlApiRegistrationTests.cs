using IdentityAccess.Application.Storage;
using IdentityAccess.Application.Security;
using IdentityAccess.Application.Authorization;
using IdentityAccess.Infrastructure.PostgreSql;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace IdentityAccess.Tests.PostgreSql
{

    public sealed class PostgreSqlApiRegistrationTests
    {
        [Fact]
        public void Disabled_by_default_does_not_register_connection_factory()
        {
            using var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
                builder.UseEnvironment("Testing"));
            using var scope = factory.Services.CreateScope();
            Assert.Null(scope.ServiceProvider.GetService<IIdentityDatabaseConnectionFactory>());
        }

        [Fact]
        public void Explicit_enable_registers_bounded_connection_factory_without_opening_a_database()
        {
            using var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Testing");
                builder.UseSetting("IdentityAccess:PostgreSql:Enabled", "true");
                builder.UseSetting("IdentityAccess:PostgreSql:MaximumPoolSize", "7");
            });
            using var scope = factory.Services.CreateScope();
            Assert.NotNull(scope.ServiceProvider.GetService<IIdentityDatabaseConnectionFactory>());
            Assert.NotNull(scope.ServiceProvider.GetService<IIdentitySchemaMigrator>());
            Assert.NotNull(scope.ServiceProvider.GetService<IUserDirectoryStore>());
            Assert.NotNull(scope.ServiceProvider.GetService<ITenantDirectoryStore>());
            Assert.NotNull(scope.ServiceProvider.GetService<ITenantMembershipStore>());
            Assert.NotNull(scope.ServiceProvider.GetService<IUserGroupStore>());
            Assert.NotNull(scope.ServiceProvider.GetService<IGroupMembershipStore>());
            Assert.NotNull(scope.ServiceProvider.GetService<IApplicationSecurityModelStore>());
            Assert.NotNull(scope.ServiceProvider.GetService<IApplicationSecurityCatalogStore>());
            Assert.Null(scope.ServiceProvider.GetService<IPermissionPolicyStore>());
            Assert.Null(scope.ServiceProvider.GetService<IPolicyStatementStore>());
            Assert.Null(scope.ServiceProvider.GetService<IGroupPolicyBindingStore>());
            Assert.Null(scope.ServiceProvider.GetService<IGroupPolicyBindingMutationStore>());
            Assert.NotNull(scope.ServiceProvider.GetService<IManagedPolicyStore>());
            Assert.NotNull(scope.ServiceProvider.GetService<IManagedPolicyVersionStore>());
            Assert.NotNull(scope.ServiceProvider.GetService<IManagedPolicyStatementStore>());
            Assert.NotNull(scope.ServiceProvider.GetService<IManagedGroupPolicyBindingStore>());
            Assert.NotNull(scope.ServiceProvider.GetService<IManagedGroupPolicyBindingMutationStore>());
            Assert.NotNull(scope.ServiceProvider.GetService<IApplicationScopeTypeStore>());
            Assert.NotNull(scope.ServiceProvider.GetService<IResourceScopeStore>());
            Assert.NotNull(scope.ServiceProvider.GetService<IAssignedCapabilityReader>());
            Assert.NotNull(scope.ServiceProvider.GetService<IGroupCapabilityGrantReader>());
            Assert.NotNull(scope.ServiceProvider.GetService<ISecurityAuditReader>());
            var options = scope.ServiceProvider.GetRequiredService<PostgreSqlStorageOptions>();
            Assert.Equal(7, options.MaximumPoolSize);
        }

        [Theory]
        [InlineData("invalid", null, "InvalidConfiguration")]
        [InlineData("true", "0", "InvalidConfiguration")]
        public void Invalid_postgresql_configuration_prevents_startup(string enabled, string? maxPool, string expected)
        {
            var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Testing");
                builder.UseSetting("IdentityAccess:PostgreSql:Enabled", enabled);
                if (maxPool is not null)
                    builder.UseSetting("IdentityAccess:PostgreSql:MaximumPoolSize", maxPool);
            });
            using (factory)
            {
                var error = Assert.ThrowsAny<Exception>(() => factory.CreateClient());
                Assert.Contains(expected, error.ToString());
            }
        }
    }
}
