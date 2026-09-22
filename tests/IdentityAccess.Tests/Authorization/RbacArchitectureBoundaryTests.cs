using IdentityAccess.Application.Authorization;
using IdentityAccess.Rbac;

namespace IdentityAccess.Tests.Authorization
{

    public sealed class RbacArchitectureBoundaryTests
    {
        [Fact]
        public void Application_layer_does_not_contain_rbac_compiler_or_wildcard_evaluator_types()
        {
            var applicationTypes = typeof(AssignedCapabilityGrant).Assembly.GetTypes();

            Assert.DoesNotContain(applicationTypes, type => type.Name == nameof(RbacTrnCompiler));
            Assert.DoesNotContain(applicationTypes, type => type.Name == "RbacScopeConformanceEvaluator");
            Assert.DoesNotContain(applicationTypes, type => type.Name == "RbacPermissionPattern");
            Assert.DoesNotContain(applicationTypes, type => type.Name == "RbacTrnContext");
        }

        [Fact]
        public void Rbac_compiler_is_owned_only_by_the_neutral_rbac_boundary()
        {
            Assert.Equal("IdentityAccess.Rbac", typeof(RbacTrnCompiler).Assembly.GetName().Name);
        }
    }
}
