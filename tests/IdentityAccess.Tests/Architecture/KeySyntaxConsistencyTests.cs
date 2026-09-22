using IdentityAccess.Application.Authentication;
using IdentityAccess.Authorization;
using IdentityAccess.Domain;
using IdentityAccess.Rbac;

namespace IdentityAccess.Tests.Architecture
{
    public sealed class KeySyntaxConsistencyTests
    {
        [Theory]
        [InlineData("a")]
        [InlineData("app-a")]
        [InlineData("scope-type-2")]
        public void Application_and_resource_scope_keys_share_the_same_canonical_slug_grammar(string value)
        {
            Assert.Equal(value, new ApplicationKey(value).Value);
            Assert.Equal(value, new ResourceScopeTypeKey(value).Value);
        }

        [Theory]
        [InlineData("APP-A")]
        [InlineData(" app-a")]
        [InlineData("app-a ")]
        [InlineData("1app")]
        [InlineData("app_a")]
        public void Canonical_slug_value_objects_reject_noncanonical_input(string value)
        {
            Assert.Throws<ArgumentException>(() => new ApplicationKey(value));
            Assert.Throws<ArgumentException>(() => new ResourceScopeTypeKey(value));
        }

        [Fact]
        public void Capability_keys_keep_their_existing_trim_and_lowercase_normalization()
        {
            var capability = new CapabilityKey(" Billing ", " Invoice ", " Read ");

            Assert.Equal("billing", capability.Resource);
            Assert.Equal("invoice", capability.Feature);
            Assert.Equal("read", capability.Action);
        }

        [Fact]
        public void Capability_patterns_normalize_segments_without_evaluating_wildcards()
        {
            var pattern = new CapabilityPattern(" Billing ", " * ", " Read ");

            Assert.Equal("billing", pattern.Resource);
            Assert.Equal("*", pattern.Feature);
            Assert.Equal("read", pattern.Action);
        }

        [Fact]
        public void Authentication_context_keys_reuse_application_key_grammar()
        {
            var client = new AuthenticationClientRegistration(
                "web-client",
                new ApplicationKey("app-a"),
                "app-a-primary",
                ["https://example.test/auth/callback"]);

            Assert.Equal("app-a-primary", client.AuthenticationContextKey);

            Assert.Throws<ArgumentException>(() => new AuthenticationClientRegistration(
                "web-client",
                new ApplicationKey("app-a"),
                "APP-A-PRIMARY",
                ["https://example.test/auth/callback"]));
        }

        [Fact]
        public void Rbac_context_canonicalization_is_shared_by_requests_and_trn_compilation()
        {
            var context = new RbacContextKey(" Project-A ");
            var request = new RbacAuthorizationRequest(
                " Project-A ",
                " Namespace-A ",
                new CapabilityKey("billing", "invoice", "read"),
                []);
            var compiler = new RbacTrnCompiler();

            Assert.Equal("project-a", context.Value);
            Assert.Equal("project-a", request.Project);
            Assert.Equal("namespace-a", request.Namespace);
            Assert.Equal(
                "trn:project-a:namespace-a:billing:invoice:read",
                compiler.Compile(
                    " Project-A ",
                    " Namespace-A ",
                    new CapabilityKey("billing", "invoice", "read")));
        }

        [Fact]
        public void Identity_authorization_request_uses_the_same_rbac_context_contract()
        {
            var scopeId = Guid.NewGuid();
            var tenant = new TenantReference(scopeId, Guid.NewGuid());
            var subject = new SubjectReference(scopeId, Guid.NewGuid());

            var request = new IdentityAuthorizationRequest(
                tenant,
                subject,
                new ApplicationKey("app-a"),
                " Project-A ",
                " Namespace-A ",
                new CapabilityKey("billing", "invoice", "read"));

            Assert.Equal("project-a", request.RbacProject);
            Assert.Equal("namespace-a", request.RbacNamespace);
        }

        [Fact]
        public void Rbac_context_segments_are_bounded_consistently()
        {
            var oversized = new string('a', 129);

            Assert.Throws<ArgumentException>(() => new RbacContextKey(oversized));
            Assert.Throws<ArgumentException>(() => new RbacTrnCompiler().Compile(
                oversized,
                "namespace-a",
                new CapabilityKey("billing", "invoice", "read")));

            Assert.Throws<ArgumentException>(() => new RbacAuthorizationRequest(
                oversized,
                "namespace-a",
                new CapabilityKey("billing", "invoice", "read"),
                []));
        }
    }
}
