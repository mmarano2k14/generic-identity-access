using OrganizationDirectory.Domain;

namespace OrganizationDirectory.Tests.Domain
{
    public sealed class OrganizationResourceScopeLinkTests
    {
        [Fact]
        public void Link_is_application_aware_and_tenant_local()
        {
            var now = DateTimeOffset.UtcNow;
            var organization = OrganizationRef(1, 2, 3);
            var scope = ScopeRef(1, 2, 4);

            var link = new OrganizationResourceScopeLink(
                organization,
                scope,
                OrganizationResourceScopeLinkStatus.Active,
                0,
                now,
                now);

            Assert.Equal("magellan", link.ResourceScope.ApplicationKey.Value);
            Assert.Equal("organization", link.ResourceScope.ScopeType.Value);
            Assert.Equal(2, link.ResourceScope.ModelVersion.Value);
        }

        [Fact]
        public void Link_rejects_cross_tenant_resource_scope()
        {
            var now = DateTimeOffset.UtcNow;

            Assert.Throws<ArgumentException>(() => new OrganizationResourceScopeLink(
                OrganizationRef(1, 2, 3),
                ScopeRef(1, 9, 4),
                OrganizationResourceScopeLinkStatus.Active,
                0,
                now,
                now));
        }

        private static OrganizationReference OrganizationRef(int scope, int tenant, int organization) => new(
            new IdentityScopeId(Guid.Parse($"00000000-0000-0000-0000-{scope:D12}")),
            new TenantId(Guid.Parse($"00000000-0000-0000-0000-{tenant:D12}")),
            new OrganizationId(Guid.Parse($"00000000-0000-0000-0000-{organization:D12}")));

        private static ResourceScopeReference ScopeRef(int scope, int tenant, int resourceScope) => new(
            new IdentityScopeId(Guid.Parse($"00000000-0000-0000-0000-{scope:D12}")),
            new TenantId(Guid.Parse($"00000000-0000-0000-0000-{tenant:D12}")),
            new ApplicationKey("magellan"),
            new ResourceScopeId(Guid.Parse($"00000000-0000-0000-0000-{resourceScope:D12}")),
            new ScopeType("organization"),
            new SecurityModelVersion(2));
    }
}
