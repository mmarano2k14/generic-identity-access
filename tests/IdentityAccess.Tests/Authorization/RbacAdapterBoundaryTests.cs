using IdentityAccess.Domain;
using IdentityAccess.Rbac;
using IdentityAccess.Rbac.MultiplexedAdapter;

namespace IdentityAccess.Tests.Authorization
{

    public sealed class RbacAdapterBoundaryTests
    {
        [Fact]
        public void Trn_compiler_materializes_wire_format_without_authorizing()
        {
            var compiler = new RbacTrnCompiler();
            var trn = compiler.Compile(
                "project-a",
                "namespace-a",
                new CapabilityKey("billing", "invoice", "read"));

            Assert.Equal("trn:project-a:namespace-a:billing:invoice:read", trn);
        }

        [Fact]
        public void Missing_external_distribution_fails_compatibility_preflight()
        {
            var adapter = new MultiplexedRbacAuthorizationAdapter(
                new MultiplexedRbacAdapterOptions(
                    Path.Combine(
                        Path.GetTempPath(),
                        Guid.NewGuid().ToString("N"))));

            var report = adapter.ProbeCompatibility();

            Assert.False(report.IsCompatible);
            Assert.Equal(
                RbacAuthorizationFailureCode.ExternalBinariesMissing,
                report.FailureCode);
            Assert.Null(report.CoreAssemblySha256);
            Assert.Null(report.AbstractionsAssemblySha256);
        }

        [Fact]
        public void Invalid_external_binary_images_fail_load_preflight()
        {
            var directory = Path.Combine(
                Path.GetTempPath(),
                "identity-access-rbac-invalid-" + Guid.NewGuid().ToString("N"));

            Directory.CreateDirectory(directory);

            try
            {
                File.WriteAllText(
                    Path.Combine(directory, "Multiplexed.Rbac.Core.dll"),
                    "not-an-assembly");

                File.WriteAllText(
                    Path.Combine(directory, "Multiplexed.Abstractions.dll"),
                    "not-an-assembly");

                var adapter = new MultiplexedRbacAuthorizationAdapter(
                    new MultiplexedRbacAdapterOptions(directory));

                var report = adapter.ProbeCompatibility();

                Assert.False(report.IsCompatible);
                Assert.Equal(
                    RbacAuthorizationFailureCode.ExternalLoadFailed,
                    report.FailureCode);
            }
            finally
            {
                Directory.Delete(
                    directory,
                    recursive: true);
            }
        }

        [Fact]
        public void Compatibility_preflight_is_process_pinned_per_adapter_instance()
        {
            var adapter = new MultiplexedRbacAuthorizationAdapter(
                new MultiplexedRbacAdapterOptions(
                    Path.Combine(
                        Path.GetTempPath(),
                        Guid.NewGuid().ToString("N"))));

            var first = adapter.ProbeCompatibility();
            var second = adapter.ProbeCompatibility();

            Assert.Same(first, second);
        }

        [Fact]
        public async Task Missing_external_distribution_is_a_technical_failure_not_a_denial()
        {
            var adapter = new MultiplexedRbacAuthorizationAdapter(
                new MultiplexedRbacAdapterOptions(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"))));
            var request = new RbacAuthorizationRequest(
                "project-a",
                "namespace-a",
                new CapabilityKey("billing", "invoice", "read"),
                ["trn:project-a:namespace-a:*:*:*"]);

            var result = await adapter.AuthorizeAsync(request, TestContext.Current.CancellationToken);

            Assert.Equal(RbacAuthorizationDecision.TechnicalFailure, result.Decision);
            Assert.Equal(RbacAuthorizationFailureCode.ExternalBinariesMissing, result.FailureCode);
        }

        [Fact]
        public async Task Cancellation_is_not_converted_to_a_business_decision()
        {
            var adapter = new MultiplexedRbacAuthorizationAdapter(
                new MultiplexedRbacAdapterOptions(Path.GetTempPath()));
            var request = new RbacAuthorizationRequest(
                "project-a",
                "namespace-a",
                new CapabilityKey("billing", "invoice", "read"),
                []);
            using var source = new CancellationTokenSource();
            source.Cancel();

            await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
                await adapter.AuthorizeAsync(request, source.Token));
        }

        [Fact]
        public void Adapter_contract_requires_an_explicit_cancellation_token()
        {
            var method = typeof(IRbacAuthorizationAdapter).GetMethod(nameof(IRbacAuthorizationAdapter.AuthorizeAsync));
            Assert.NotNull(method);
            var cancellation = method!.GetParameters().Single(x => x.ParameterType == typeof(CancellationToken));
            Assert.False(cancellation.HasDefaultValue);
            Assert.False(cancellation.IsOptional);
        }

        [Fact]
        public void Core_projects_do_not_reference_external_rbac_assemblies()
        {
            static IEnumerable<string> References(Type marker) =>
                marker.Assembly.GetReferencedAssemblies().Select(x => x.Name ?? string.Empty);

            Assert.DoesNotContain(References(typeof(CapabilityKey)), x => x.StartsWith("Multiplexed.", StringComparison.Ordinal));
            Assert.DoesNotContain(References(typeof(IdentityAccess.Application.Authorization.AssignedCapabilityGrant)),
                x => x.StartsWith("Multiplexed.", StringComparison.Ordinal));
            Assert.DoesNotContain(References(typeof(IRbacAuthorizationAdapter)),
                x => x.StartsWith("Multiplexed.", StringComparison.Ordinal));
        }
    }
}
