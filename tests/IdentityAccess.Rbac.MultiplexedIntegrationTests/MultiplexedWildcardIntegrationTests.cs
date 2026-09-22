using IdentityAccess.Domain;
using IdentityAccess.Rbac;
using IdentityAccess.Rbac.MultiplexedAdapter;

namespace IdentityAccess.Rbac.MultiplexedIntegrationTests
{

    public sealed class MultiplexedWildcardIntegrationTests
    {
        private static MultiplexedRbacAuthorizationAdapter Adapter()
        {
            var dir = Environment.GetEnvironmentVariable("MULTIPLEXED_RBAC_REFERENCE_DIR");
            Assert.False(string.IsNullOrWhiteSpace(dir),
                "MULTIPLEXED_RBAC_REFERENCE_DIR must point to the external RBAC net10.0 output directory.");
            return new MultiplexedRbacAuthorizationAdapter(new MultiplexedRbacAdapterOptions(dir!));
        }

        [Fact]
        public void External_distribution_passes_contract_preflight_with_stable_fingerprints()
        {
            var adapter = Adapter();

            var report = adapter.ProbeCompatibility();

            Assert.True(
                report.IsCompatible,
                $"External RBAC compatibility preflight failed: {report.FailureCode}: {report.DiagnosticDetail}");

            Assert.False(string.IsNullOrWhiteSpace(report.CoreAssemblyVersion));
            Assert.False(string.IsNullOrWhiteSpace(report.AbstractionsAssemblyVersion));

            Assert.Matches(
                "^[0-9A-F]{64}$",
                report.CoreAssemblySha256!);

            Assert.Matches(
                "^[0-9A-F]{64}$",
                report.AbstractionsAssemblySha256!);

            Assert.Same(
                report,
                adapter.ProbeCompatibility());
        }

        public static TheoryData<string, string, string, string, string, string, bool> Wildcards => new()
        {
            { "billing", "invoice", "read",   "billing", "invoice", "read",   true  },
            { "billing", "invoice", "*",      "billing", "invoice", "refund", true  },
            { "billing", "*",       "read",   "billing", "order",   "read",   true  },
            { "billing", "*",       "*",      "billing", "order",   "delete", true  },
            { "*",       "*",       "read",   "ledger",  "entry",   "read",   true  },
            { "*",       "*",       "*",      "ledger",  "entry",   "delete", true  },
            { "billing", "invoice", "*",      "billing", "order",   "read",   false },
            { "billing", "*",       "read",   "billing", "order",   "write",  false },
            { "*",       "*",       "read",   "ledger",  "entry",   "write",  false },
        };

        [Theory]
        [MemberData(nameof(Wildcards))]
        public async Task Real_external_engine_evaluates_supported_wildcard_matrix(
            string grantedResource,
            string grantedFeature,
            string grantedAction,
            string requestedResource,
            string requestedFeature,
            string requestedAction,
            bool expected)
        {
            var adapter = Adapter();
            var request = Request(
                $"trn:test-project:test-ns:{grantedResource}:{grantedFeature}:{grantedAction}",
                requestedResource, requestedFeature, requestedAction);

            var result = await adapter.AuthorizeAsync(request, TestContext.Current.CancellationToken);

            Assert.True(
                result.Decision != RbacAuthorizationDecision.TechnicalFailure,
                $"External RBAC adapter failed before wildcard evaluation: {result.FailureCode}: {result.DiagnosticDetail}");
            Assert.Equal(
                expected ? RbacAuthorizationDecision.Allowed : RbacAuthorizationDecision.Denied,
                result.Decision);
        }

        [Theory]
        [InlineData("billing:invo*:read")]
        [InlineData("billing:invoice:*read")]
        [InlineData("bi*lling:invoice:read")]
        public async Task Partial_wildcards_are_rejected_by_real_external_engine(string pattern)
        {
            var adapter = Adapter();
            var request = Request($"trn:test-project:test-ns:{pattern}", "billing", "invoice", "read");

            var result = await adapter.AuthorizeAsync(request, TestContext.Current.CancellationToken);

            Assert.True(
                result.Decision != RbacAuthorizationDecision.TechnicalFailure,
                $"External RBAC adapter failed before wildcard evaluation: {result.FailureCode}: {result.DiagnosticDetail}");
            Assert.Equal(RbacAuthorizationDecision.Denied, result.Decision);
        }

        [Theory]
        [InlineData("trn:other-project:test-ns:*:*:*")]
        [InlineData("trn:test-project:other-ns:*:*:*")]
        [InlineData("not-a-trn")]
        public async Task Adapter_never_imports_grants_from_another_context(string grant)
        {
            var adapter = Adapter();
            var request = Request(grant, "billing", "invoice", "read");

            var result = await adapter.AuthorizeAsync(request, TestContext.Current.CancellationToken);

            Assert.True(
                result.Decision != RbacAuthorizationDecision.TechnicalFailure,
                $"External RBAC adapter failed before wildcard evaluation: {result.FailureCode}: {result.DiagnosticDetail}");
            Assert.Equal(RbacAuthorizationDecision.Denied, result.Decision);
        }

        [Fact]
        public async Task Concurrent_external_engine_contexts_remain_independent()
        {
            var adapter = Adapter();
            var cancellationToken = TestContext.Current.CancellationToken;
            var tasks = Enumerable.Range(0, 64).Select(async i =>
            {
                var allow = i % 2 == 0;
                var grant = allow
                    ? "trn:test-project:test-ns:*:*:read"
                    : "trn:test-project:test-ns:billing:invoice:write";
                var result = await adapter.AuthorizeAsync(
                    Request(grant, "billing", "invoice", "read"), cancellationToken);
                return (allow, result.Decision, result.FailureCode, result.DiagnosticDetail);
            });

            foreach (var item in await Task.WhenAll(tasks))
            {
                Assert.True(
                    item.Decision != RbacAuthorizationDecision.TechnicalFailure,
                    $"External RBAC adapter failed before wildcard evaluation: {item.FailureCode}: {item.DiagnosticDetail}");
                Assert.Equal(item.allow ? RbacAuthorizationDecision.Allowed : RbacAuthorizationDecision.Denied, item.Decision);
            }
        }

        private static RbacAuthorizationRequest Request(
            string grant,
            string resource,
            string feature,
            string action) =>
            new(
                "test-project",
                "test-ns",
                new CapabilityKey(resource, feature, action),
                [grant]);
    }
}
