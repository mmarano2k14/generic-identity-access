using OrganizationDirectory.Application.Administration;
using OrganizationDirectory.Application.Storage;
using OrganizationDirectory.Domain;
using OrganizationDirectory.Tests.Support;

namespace OrganizationDirectory.Tests.Application
{
    /// <summary>Verifies explicit organization membership remains separate from authorization.</summary>
    public sealed class OrganizationMembershipAdministrationServiceTests
    {
        private static readonly IdentityScopeId Scope =
            new(Guid.Parse("71000000-0000-0000-0000-000000000001"));

        private static readonly TenantId Tenant =
            new(Guid.Parse("72000000-0000-0000-0000-000000000001"));

        [Fact]
        public async Task Active_tenant_member_can_join_suspend_reactivate_and_leave_active_organization()
        {
            var fixture = await CreateFixtureAsync();

            var created = await fixture.Service.AddAsync(
                fixture.Organization.Reference,
                fixture.TenantMembership,
                CancellationToken.None);

            Assert.Equal(OrganizationMembershipStatus.Active, created.Status);
            Assert.Equal(1, created.RowVersion);

            fixture.Clock.UtcNow = fixture.Clock.UtcNow.AddMinutes(1);

            var suspended = await fixture.Service.SetStatusAsync(
                fixture.Organization.Reference,
                fixture.TenantMembership,
                OrganizationMembershipStatus.Suspended,
                created.RowVersion,
                CancellationToken.None);

            Assert.NotNull(suspended);
            Assert.Equal(OrganizationMembershipStatus.Suspended, suspended.Status);
            Assert.Equal(2, suspended.RowVersion);

            fixture.Clock.UtcNow = fixture.Clock.UtcNow.AddMinutes(1);

            var reactivated = await fixture.Service.SetStatusAsync(
                fixture.Organization.Reference,
                fixture.TenantMembership,
                OrganizationMembershipStatus.Active,
                suspended.RowVersion,
                CancellationToken.None);

            Assert.NotNull(reactivated);
            Assert.Equal(OrganizationMembershipStatus.Active, reactivated.Status);
            Assert.Equal(3, reactivated.RowVersion);

            Assert.True(await fixture.Service.RemoveAsync(
                fixture.Organization.Reference,
                fixture.TenantMembership,
                reactivated.RowVersion,
                CancellationToken.None));

            Assert.Null(await fixture.Service.GetAsync(
                fixture.Organization.Reference,
                fixture.TenantMembership,
                CancellationToken.None));
        }

        [Fact]
        public async Task Missing_tenant_membership_is_rejected()
        {
            var fixture = await CreateFixtureAsync(
                registerTenantMembership: false);

            await Assert.ThrowsAsync<TenantMembershipReferenceNotFoundException>(() =>
                fixture.Service.AddAsync(
                    fixture.Organization.Reference,
                    fixture.TenantMembership,
                    CancellationToken.None));
        }

        [Fact]
        public async Task Inactive_tenant_membership_is_rejected_for_add()
        {
            var fixture = await CreateFixtureAsync(
                tenantMembershipActive: false);

            await Assert.ThrowsAsync<TenantMembershipInactiveException>(() =>
                fixture.Service.AddAsync(
                    fixture.Organization.Reference,
                    fixture.TenantMembership,
                    CancellationToken.None));
        }

        [Fact]
        public async Task Disabled_organization_is_rejected_for_add()
        {
            var fixture = await CreateFixtureAsync(
                organizationStatus: OrganizationStatus.Disabled);

            await Assert.ThrowsAsync<OrganizationMembershipOrganizationInactiveException>(() =>
                fixture.Service.AddAsync(
                    fixture.Organization.Reference,
                    fixture.TenantMembership,
                    CancellationToken.None));
        }

        [Fact]
        public async Task Member_centric_read_returns_only_explicit_organization_memberships()
        {
            var fixture = await CreateFixtureAsync();

            var secondReference = new OrganizationReference(
                Scope,
                Tenant,
                new OrganizationId(
                    Guid.Parse("73000000-0000-0000-0000-000000000002")));

            await fixture.OrganizationStore.CreateAsync(
                new Organization(
                    secondReference,
                    new OrganizationKey("second"),
                    "Second",
                    new OrganizationType("business"),
                    null,
                    OrganizationStatus.Active,
                    0,
                    fixture.Clock.UtcNow,
                    fixture.Clock.UtcNow),
                CancellationToken.None);

            await fixture.Service.AddAsync(
                fixture.Organization.Reference,
                fixture.TenantMembership,
                CancellationToken.None);

            await fixture.Service.AddAsync(
                secondReference,
                fixture.TenantMembership,
                CancellationToken.None);

            var memberships = await fixture.Service.ListForTenantMembershipAsync(
                fixture.TenantMembership,
                0,
                100,
                CancellationToken.None);

            Assert.Equal(2, memberships.Count);
            Assert.All(
                memberships,
                item => Assert.Equal(
                    fixture.TenantMembership,
                    item.TenantMembership));
        }

        [Fact]
        public async Task Duplicate_relation_does_not_create_a_second_membership()
        {
            var fixture = await CreateFixtureAsync();

            await fixture.Service.AddAsync(
                fixture.Organization.Reference,
                fixture.TenantMembership,
                CancellationToken.None);

            await Assert.ThrowsAsync<OrganizationMembershipAlreadyExistsException>(() =>
                fixture.Service.AddAsync(
                    fixture.Organization.Reference,
                    fixture.TenantMembership,
                    CancellationToken.None));
        }

        private static async Task<OrganizationMembershipServiceFixture> CreateFixtureAsync(
            bool registerTenantMembership = true,
            bool tenantMembershipActive = true,
            OrganizationStatus organizationStatus = OrganizationStatus.Active)
        {
            var organizationStore = new InMemoryOrganizationStore();
            var membershipStore = new InMemoryOrganizationMembershipStore();
            var tenantMembershipReader = new InMemoryTenantMembershipReferenceReader();
            var clock = new TestOrganizationClock(
                new DateTimeOffset(
                    2026,
                    9,
                    29,
                    9,
                    0,
                    0,
                    TimeSpan.Zero));

            var organization = await organizationStore.CreateAsync(
                new Organization(
                    new OrganizationReference(
                        Scope,
                        Tenant,
                        new OrganizationId(
                            Guid.Parse("73000000-0000-0000-0000-000000000001"))),
                    new OrganizationKey("urban-flower"),
                    "Urban Flower",
                    new OrganizationType("business"),
                    null,
                    organizationStatus,
                    0,
                    clock.UtcNow,
                    clock.UtcNow),
                CancellationToken.None);

            var tenantMembership = new TenantMembershipReference(
                Scope,
                Tenant,
                new TenantMembershipId(
                    Guid.Parse("74000000-0000-0000-0000-000000000001")));

            if (registerTenantMembership)
            {
                tenantMembershipReader.Add(
                    tenantMembership,
                    tenantMembershipActive);
            }

            return new OrganizationMembershipServiceFixture(
                new OrganizationMembershipAdministrationService(
                    organizationStore,
                    membershipStore,
                    tenantMembershipReader,
                    clock),
                organizationStore,
                organization,
                tenantMembership,
                clock);
        }
    }
}
