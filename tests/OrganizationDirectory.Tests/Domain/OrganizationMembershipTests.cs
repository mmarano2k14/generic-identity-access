using OrganizationDirectory.Domain;

namespace OrganizationDirectory.Tests.Domain
{
    public sealed class OrganizationMembershipTests
    {
        [Fact]
        public void Membership_accepts_matching_tenant_membership()
        {
            var now = DateTimeOffset.UtcNow;
            var organization = OrganizationRef(1, 2, 3);
            var tenantMembership = MembershipRef(1, 2, 4);

            var membership = new OrganizationMembership(
                organization,
                tenantMembership,
                OrganizationMembershipStatus.Active,
                0,
                now,
                now);

            Assert.Equal(organization, membership.Organization);
            Assert.Equal(tenantMembership, membership.TenantMembership);
        }

        [Fact]
        public void Membership_rejects_cross_tenant_reference()
        {
            var now = DateTimeOffset.UtcNow;

            Assert.Throws<ArgumentException>(() => new OrganizationMembership(
                OrganizationRef(1, 2, 3),
                MembershipRef(1, 9, 4),
                OrganizationMembershipStatus.Active,
                0,
                now,
                now));
        }

        private static OrganizationReference OrganizationRef(int scope, int tenant, int organization) => new(
            new IdentityScopeId(Guid.Parse($"00000000-0000-0000-0000-{scope:D12}")),
            new TenantId(Guid.Parse($"00000000-0000-0000-0000-{tenant:D12}")),
            new OrganizationId(Guid.Parse($"00000000-0000-0000-0000-{organization:D12}")));

        private static TenantMembershipReference MembershipRef(int scope, int tenant, int membership) => new(
            new IdentityScopeId(Guid.Parse($"00000000-0000-0000-0000-{scope:D12}")),
            new TenantId(Guid.Parse($"00000000-0000-0000-0000-{tenant:D12}")),
            new TenantMembershipId(Guid.Parse($"00000000-0000-0000-0000-{membership:D12}")));
    }
}
