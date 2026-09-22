using System.Reflection;
using IdentityAccess.Application.Administration;

namespace IdentityAccess.Tests.Administration
{

    public sealed class AdministrationServiceContractTests
    {
        [Theory]
        [InlineData(typeof(IDirectoryAdministrationService))]
        [InlineData(typeof(IPolicyAdministrationService))]
        [InlineData(typeof(IResourceScopeAdministrationService))]
        public void Administration_contracts_require_explicit_cancellation_tokens(Type contract)
        {
            foreach (var method in contract.GetMethods())
            {
                var cancellation = method.GetParameters().LastOrDefault();
                Assert.NotNull(cancellation);
                Assert.Equal(typeof(CancellationToken), cancellation!.ParameterType);
                Assert.False(cancellation.HasDefaultValue);
                Assert.False(cancellation.IsOptional);
            }
        }

        [Fact]
        public void Administration_services_are_application_layer_types()
        {
            Assert.Equal("IdentityAccess.Application", typeof(IDirectoryAdministrationService).Assembly.GetName().Name);
            Assert.Equal("IdentityAccess.Application", typeof(IPolicyAdministrationService).Assembly.GetName().Name);
            Assert.Equal("IdentityAccess.Application", typeof(IResourceScopeAdministrationService).Assembly.GetName().Name);
        }

        [Fact]
        public void Administration_contracts_do_not_expose_http_types()
        {
            foreach (var contract in new[] { typeof(IDirectoryAdministrationService), typeof(IPolicyAdministrationService), typeof(IResourceScopeAdministrationService) })
            foreach (var method in contract.GetMethods())
            foreach (var type in method.GetParameters().Select(parameter => parameter.ParameterType).Append(method.ReturnType))
                Assert.False((type.FullName ?? string.Empty).Contains("Microsoft.AspNetCore", StringComparison.Ordinal));
        }
    }
}
