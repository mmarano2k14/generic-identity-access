using IdentityAccess.Domain;
using IdentityAccess.Rbac;

namespace IdentityAccess.Tests.Authorization
{

    public sealed class RbacTrnCompilerTests
    {
        [Fact]
        public void Compiler_canonicalizes_context_and_capability_segments()
        {
            var compiler = new RbacTrnCompiler();
            var capability = new CapabilityKey("Billing", "Invoice", "Read");

            var trn = compiler.Compile(" Generic-App ", " Tenant-Scope ", capability);

            Assert.Equal("trn:generic-app:tenant-scope:billing:invoice:read", trn);
        }

        [Theory]
        [InlineData("billing", "invoice", "read", "trn:project-a:namespace-a:billing:invoice:read")]
        [InlineData("billing", "invoice", "*", "trn:project-a:namespace-a:billing:invoice:*")]
        [InlineData("billing", "*", "read", "trn:project-a:namespace-a:billing:*:read")]
        [InlineData("billing", "*", "*", "trn:project-a:namespace-a:billing:*:*")]
        [InlineData("*", "*", "read", "trn:project-a:namespace-a:*:*:read")]
        [InlineData("*", "*", "*", "trn:project-a:namespace-a:*:*:*")]
        public void Compiler_materializes_supported_patterns_without_evaluating_them(
            string resource,
            string feature,
            string action,
            string expected)
        {
            var compiler = new RbacTrnCompiler();

            var trn = compiler.Compile("project-a", "namespace-a", new CapabilityPattern(resource, feature, action));

            Assert.Equal(expected, trn);
        }

        [Theory]
        [InlineData("project:*", "namespace-a")]
        [InlineData("project:a", "namespace-a")]
        [InlineData("project-a", "namespace:one")]
        [InlineData("project-a", "namespace*")]
        public void Compiler_rejects_invalid_rbac_context_segments(string project, string @namespace)
        {
            var compiler = new RbacTrnCompiler();

            Assert.Throws<ArgumentException>(() =>
                compiler.Compile(project, @namespace, new CapabilityKey("billing", "invoice", "read")));
        }
    }
}
