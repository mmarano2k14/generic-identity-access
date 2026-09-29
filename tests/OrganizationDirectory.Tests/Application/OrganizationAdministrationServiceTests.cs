using OrganizationDirectory.Application.Administration;
using OrganizationDirectory.Application.Storage;
using OrganizationDirectory.Domain;
using OrganizationDirectory.Tests.Support;

namespace OrganizationDirectory.Tests.Application
{
    /// <summary>Verifies organization lifecycle and hierarchy application behavior.</summary>
    public sealed class OrganizationAdministrationServiceTests
    {
        private static readonly IdentityScopeId Scope = new(Guid.Parse("61000000-0000-0000-0000-000000000001"));
        private static readonly TenantId Tenant = new(Guid.Parse("62000000-0000-0000-0000-000000000001"));

        [Fact]
        public async Task Create_tree_update_and_lifecycle_are_deterministic()
        {
            var store = new InMemoryOrganizationStore();
            var clock = new TestOrganizationClock(new DateTimeOffset(2026, 9, 29, 8, 0, 0, TimeSpan.Zero));
            var service = new OrganizationAdministrationService(store, clock);

            var root = await service.CreateAsync(
                Reference("63000000-0000-0000-0000-000000000001"),
                new OrganizationKey("urban-group"),
                "Urban Group",
                new OrganizationType("organization"),
                null,
                CancellationToken.None);

            var child = await service.CreateAsync(
                Reference("63000000-0000-0000-0000-000000000002"),
                new OrganizationKey("urban-flower"),
                "Urban Flower",
                new OrganizationType("business"),
                root.Reference.OrganizationId,
                CancellationToken.None);

            var tree = await service.GetTreeAsync(Scope, Tenant, CancellationToken.None);

            var rootNode = Assert.Single(tree);
            Assert.Equal(root.Reference.OrganizationId, rootNode.Organization.Reference.OrganizationId);
            Assert.Equal(child.Reference.OrganizationId, Assert.Single(rootNode.Children).Organization.Reference.OrganizationId);

            clock.UtcNow = clock.UtcNow.AddMinutes(1);

            var updated = await service.UpdateAsync(
                child.Reference,
                "Urban Flower Thailand",
                new OrganizationType("business"),
                root.Reference.OrganizationId,
                child.RowVersion,
                CancellationToken.None);

            Assert.NotNull(updated);
            Assert.Equal("urban-flower", updated.Key.Value);
            Assert.Equal(2, updated.RowVersion);

            clock.UtcNow = clock.UtcNow.AddMinutes(1);

            var disabled = await service.SetStatusAsync(
                child.Reference,
                OrganizationStatus.Disabled,
                updated.RowVersion,
                CancellationToken.None);

            Assert.NotNull(disabled);
            Assert.Equal(OrganizationStatus.Disabled, disabled.Status);
            Assert.Equal(3, disabled.RowVersion);
        }

        [Fact]
        public async Task Create_rejects_missing_parent()
        {
            var service = new OrganizationAdministrationService(
                new InMemoryOrganizationStore(),
                new TestOrganizationClock(DateTimeOffset.UtcNow));

            await Assert.ThrowsAsync<OrganizationParentNotFoundException>(() =>
                service.CreateAsync(
                    Reference("63000000-0000-0000-0000-000000000010"),
                    new OrganizationKey("child"),
                    "Child",
                    new OrganizationType("business"),
                    new OrganizationId(Guid.Parse("63000000-0000-0000-0000-000000000099")),
                    CancellationToken.None));
        }

        [Fact]
        public async Task Update_rejects_moving_parent_below_its_descendant()
        {
            var store = new InMemoryOrganizationStore();
            var clock = new TestOrganizationClock(DateTimeOffset.UtcNow);
            var service = new OrganizationAdministrationService(store, clock);

            var root = await service.CreateAsync(
                Reference("63000000-0000-0000-0000-000000000020"),
                new OrganizationKey("root"),
                "Root",
                new OrganizationType("organization"),
                null,
                CancellationToken.None);

            var child = await service.CreateAsync(
                Reference("63000000-0000-0000-0000-000000000021"),
                new OrganizationKey("child"),
                "Child",
                new OrganizationType("business"),
                root.Reference.OrganizationId,
                CancellationToken.None);

            await Assert.ThrowsAsync<OrganizationHierarchyConflictException>(() =>
                service.UpdateAsync(
                    root.Reference,
                    root.DisplayName,
                    root.Type,
                    child.Reference.OrganizationId,
                    root.RowVersion,
                    CancellationToken.None));
        }

        [Fact]
        public async Task Direct_children_are_tenant_local_and_deterministic()
        {
            var store = new InMemoryOrganizationStore();
            var service = new OrganizationAdministrationService(
                store,
                new TestOrganizationClock(DateTimeOffset.UtcNow));

            var root = await service.CreateAsync(
                Reference("63000000-0000-0000-0000-000000000030"),
                new OrganizationKey("root"),
                "Root",
                new OrganizationType("organization"),
                null,
                CancellationToken.None);

            await service.CreateAsync(
                Reference("63000000-0000-0000-0000-000000000031"),
                new OrganizationKey("z-child"),
                "Z",
                new OrganizationType("business"),
                root.Reference.OrganizationId,
                CancellationToken.None);

            await service.CreateAsync(
                Reference("63000000-0000-0000-0000-000000000032"),
                new OrganizationKey("a-child"),
                "A",
                new OrganizationType("business"),
                root.Reference.OrganizationId,
                CancellationToken.None);

            var children = await service.ListChildrenAsync(root.Reference, CancellationToken.None);

            Assert.Equal(new[] { "a-child", "z-child" }, children.Select(item => item.Key.Value));
        }

        private static OrganizationReference Reference(string organizationId) =>
            new(Scope, Tenant, new OrganizationId(Guid.Parse(organizationId)));
    }
}
