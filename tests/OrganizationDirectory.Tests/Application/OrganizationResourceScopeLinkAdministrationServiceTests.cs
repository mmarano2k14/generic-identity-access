using OrganizationDirectory.Application.Administration;
using OrganizationDirectory.Application.Storage;
using OrganizationDirectory.Domain;
using OrganizationDirectory.Tests.Support;

namespace OrganizationDirectory.Tests.Application
{
    /// <summary>Verifies Organization ResourceScope linkage uses authoritative Identity Access scope state.</summary>
    public sealed class OrganizationResourceScopeLinkAdministrationServiceTests
    {
        private static readonly IdentityScopeId Scope =
            new(Guid.Parse("91000000-0000-0000-0000-000000000001"));

        private static readonly TenantId Tenant =
            new(Guid.Parse("92000000-0000-0000-0000-000000000001"));

        private static readonly ApplicationKey Application =
            new("consumer-app");

        [Fact]
        public async Task Active_organization_can_link_relink_and_remove_active_resource_scope()
        {
            var organizationStore = new InMemoryOrganizationStore();
            var linkStore = new InMemoryOrganizationResourceScopeLinkStore();
            var scopeReader = new InMemoryResourceScopeReferenceReader();
            var clock = new TestOrganizationClock(
                new DateTimeOffset(2026, 9, 29, 10, 0, 0, TimeSpan.Zero));

            var organization = await CreateOrganizationAsync(
                organizationStore,
                clock,
                OrganizationStatus.Active);

            var firstScope = ResourceScope(
                "93000000-0000-0000-0000-000000000001",
                "organization",
                2);

            var secondScope = ResourceScope(
                "93000000-0000-0000-0000-000000000002",
                "organization",
                2);

            scopeReader.Add(firstScope);
            scopeReader.Add(secondScope);

            var service = new OrganizationResourceScopeLinkAdministrationService(
                organizationStore,
                linkStore,
                scopeReader,
                clock);

            var created = await service.LinkAsync(
                organization.Reference,
                Application,
                firstScope.ResourceScopeId,
                CancellationToken.None);

            Assert.Equal(firstScope.ResourceScopeId, created.ResourceScope.ResourceScopeId);
            Assert.Equal(1, created.RowVersion);

            clock.UtcNow = clock.UtcNow.AddMinutes(1);

            var updated = await service.RelinkAsync(
                organization.Reference,
                Application,
                secondScope.ResourceScopeId,
                created.RowVersion,
                CancellationToken.None);

            Assert.NotNull(updated);
            Assert.Equal(secondScope.ResourceScopeId, updated.ResourceScope.ResourceScopeId);
            Assert.Equal(2, updated.RowVersion);

            Assert.True(await service.RemoveAsync(
                organization.Reference,
                Application,
                updated.RowVersion,
                CancellationToken.None));
        }

        [Fact]
        public async Task Missing_resource_scope_is_rejected()
        {
            var organizationStore = new InMemoryOrganizationStore();
            var service = await CreateServiceAsync(
                organizationStore,
                new InMemoryOrganizationResourceScopeLinkStore(),
                new InMemoryResourceScopeReferenceReader(),
                OrganizationStatus.Active);

            await Assert.ThrowsAsync<OrganizationResourceScopeReferenceNotFoundException>(() =>
                service.Service.LinkAsync(
                    service.Organization.Reference,
                    Application,
                    new ResourceScopeId(
                        Guid.Parse("93000000-0000-0000-0000-000000000010")),
                    CancellationToken.None));
        }

        [Fact]
        public async Task Inactive_resource_scope_is_rejected()
        {
            var organizationStore = new InMemoryOrganizationStore();
            var linkStore = new InMemoryOrganizationResourceScopeLinkStore();
            var scopeReader = new InMemoryResourceScopeReferenceReader();

            var fixture = await CreateServiceAsync(
                organizationStore,
                linkStore,
                scopeReader,
                OrganizationStatus.Active);

            var scope = ResourceScope(
                "93000000-0000-0000-0000-000000000020",
                "organization",
                2);

            scopeReader.Add(scope, isActive: false);

            await Assert.ThrowsAsync<OrganizationResourceScopeInactiveException>(() =>
                fixture.Service.LinkAsync(
                    fixture.Organization.Reference,
                    Application,
                    scope.ResourceScopeId,
                    CancellationToken.None));
        }

        [Fact]
        public async Task Disabled_organization_is_rejected_for_link_mutation()
        {
            var organizationStore = new InMemoryOrganizationStore();
            var scopeReader = new InMemoryResourceScopeReferenceReader();

            var fixture = await CreateServiceAsync(
                organizationStore,
                new InMemoryOrganizationResourceScopeLinkStore(),
                scopeReader,
                OrganizationStatus.Disabled);

            var scope = ResourceScope(
                "93000000-0000-0000-0000-000000000030",
                "organization",
                2);

            scopeReader.Add(scope);

            await Assert.ThrowsAsync<OrganizationResourceScopeOrganizationInactiveException>(() =>
                fixture.Service.LinkAsync(
                    fixture.Organization.Reference,
                    Application,
                    scope.ResourceScopeId,
                    CancellationToken.None));
        }

        [Fact]
        public async Task Same_resource_scope_cannot_be_linked_to_two_organizations()
        {
            var organizationStore = new InMemoryOrganizationStore();
            var linkStore = new InMemoryOrganizationResourceScopeLinkStore();
            var scopeReader = new InMemoryResourceScopeReferenceReader();
            var clock = new TestOrganizationClock(DateTimeOffset.UtcNow);

            var first = await CreateOrganizationAsync(
                organizationStore,
                clock,
                OrganizationStatus.Active,
                "urban-flower",
                "94000000-0000-0000-0000-000000000001");

            var second = await CreateOrganizationAsync(
                organizationStore,
                clock,
                OrganizationStatus.Active,
                "urban-cafe",
                "94000000-0000-0000-0000-000000000002");

            var scope = ResourceScope(
                "93000000-0000-0000-0000-000000000040",
                "organization",
                2);

            scopeReader.Add(scope);

            var service = new OrganizationResourceScopeLinkAdministrationService(
                organizationStore,
                linkStore,
                scopeReader,
                clock);

            await service.LinkAsync(
                first.Reference,
                Application,
                scope.ResourceScopeId,
                CancellationToken.None);

            await Assert.ThrowsAsync<OrganizationResourceScopeAlreadyLinkedException>(() =>
                service.LinkAsync(
                    second.Reference,
                    Application,
                    scope.ResourceScopeId,
                    CancellationToken.None));
        }

        private static async Task<(
            OrganizationResourceScopeLinkAdministrationService Service,
            Organization Organization)> CreateServiceAsync(
            InMemoryOrganizationStore organizationStore,
            InMemoryOrganizationResourceScopeLinkStore linkStore,
            InMemoryResourceScopeReferenceReader scopeReader,
            OrganizationStatus status)
        {
            var clock = new TestOrganizationClock(DateTimeOffset.UtcNow);

            var organization = await CreateOrganizationAsync(
                organizationStore,
                clock,
                status);

            return (
                new OrganizationResourceScopeLinkAdministrationService(
                    organizationStore,
                    linkStore,
                    scopeReader,
                    clock),
                organization);
        }

        private static Task<Organization> CreateOrganizationAsync(
            InMemoryOrganizationStore store,
            TestOrganizationClock clock,
            OrganizationStatus status,
            string key = "urban-flower",
            string organizationId = "94000000-0000-0000-0000-000000000010") =>
            store.CreateAsync(
                new Organization(
                    new OrganizationReference(
                        Scope,
                        Tenant,
                        new OrganizationId(Guid.Parse(organizationId))),
                    new OrganizationKey(key),
                    key,
                    new OrganizationType("business"),
                    null,
                    status,
                    0,
                    clock.UtcNow,
                    clock.UtcNow),
                CancellationToken.None);

        private static ResourceScopeReference ResourceScope(
            string id,
            string type,
            int version) =>
            new(
                Scope,
                Tenant,
                Application,
                new ResourceScopeId(Guid.Parse(id)),
                new ScopeType(type),
                new SecurityModelVersion(version));
    }
}
