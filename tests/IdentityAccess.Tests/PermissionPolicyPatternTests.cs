using IdentityAccess.Domain;
using IdentityAccess.Rbac;

namespace IdentityAccess.Tests
{

    public sealed class PermissionPolicyPatternTests
    {
        [Theory]
        [InlineData("billing", "invoice", "read")]
        [InlineData("billing", "invoice", "*")]
        [InlineData("billing", "*", "read")]
        [InlineData("billing", "*", "*")]
        [InlineData("*", "*", "read")]
        [InlineData("*", "*", "*")]
        public void Supported_external_rbac_pattern_shapes_are_accepted(string resource, string feature, string action)
        {
            var pattern = new CapabilityPattern(resource, feature, action);
            Assert.Equal(resource, pattern.Resource);
            Assert.Equal(feature, pattern.Feature);
            Assert.Equal(action, pattern.Action);
        }

        [Theory]
        [InlineData("*", "invoice", "read")]
        [InlineData("*", "invoice", "*")]
        [InlineData("bi*lling", "invoice", "read")]
        [InlineData("billing", "invo*", "read")]
        [InlineData("billing", "invoice", "*read")]
        public void Unsupported_or_partial_wildcards_are_rejected_before_persistence(string resource, string feature, string action)
        {
            Assert.Throws<ArgumentException>(() => new CapabilityPattern(resource, feature, action));
        }

        [Fact]
        public void Trn_compiler_materializes_pattern_without_evaluating_it()
        {
            var compiler = new RbacTrnCompiler();
            var trn = compiler.Compile("project-a", "tenant-a", new CapabilityPattern("billing", "invoice", "*"));
            Assert.Equal("trn:project-a:tenant-a:billing:invoice:*", trn);
        }

        [Fact]
        public void Concrete_pattern_round_trips_to_capability_key()
        {
            var pattern = new CapabilityPattern("billing", "invoice", "read");
            Assert.Equal(new CapabilityKey("billing", "invoice", "read"), pattern.ToConcreteCapability());
        }

        [Fact]
        public void Wildcard_pattern_cannot_be_converted_to_concrete_capability_key()
        {
            var pattern = new CapabilityPattern("billing", "invoice", "*");
            Assert.Throws<InvalidOperationException>(() => pattern.ToConcreteCapability());
        }
    }
}
