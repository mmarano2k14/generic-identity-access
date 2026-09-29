using OrganizationDirectory.Domain;

namespace OrganizationDirectory.Tests.Domain
{
    public sealed class OrganizationTests
    {
        [Fact]
        public void Organization_rejects_self_parenting()
        {
            var reference = Reference(1, 2, 3);
            var now = DateTimeOffset.UtcNow;

            var error = Assert.Throws<ArgumentException>(() => new Organization(
                reference,
                new OrganizationKey("urban-flower"),
                "Urban Flower",
                new OrganizationType("business"),
                reference,
                OrganizationStatus.Active,
                0,
                now,
                now));

            Assert.Contains("cannot parent itself", error.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void Organization_rejects_cross_tenant_parent()
        {
            var reference = Reference(1, 2, 3);
            var parent = Reference(1, 9, 4);
            var now = DateTimeOffset.UtcNow;

            Assert.Throws<ArgumentException>(() => new Organization(
                reference,
                new OrganizationKey("urban-flower"),
                "Urban Flower",
                new OrganizationType("business"),
                parent,
                OrganizationStatus.Active,
                0,
                now,
                now));
        }

        [Fact]
        public void Organization_rejects_negative_row_version()
        {
            var now = DateTimeOffset.UtcNow;

            Assert.Throws<ArgumentOutOfRangeException>(() => new Organization(
                Reference(1, 2, 3),
                new OrganizationKey("urban-flower"),
                "Urban Flower",
                new OrganizationType("business"),
                null,
                OrganizationStatus.Active,
                -1,
                now,
                now));
        }

        private static OrganizationReference Reference(int scope, int tenant, int organization) => new(
            new IdentityScopeId(Guid.Parse($"00000000-0000-0000-0000-{scope:D12}")),
            new TenantId(Guid.Parse($"00000000-0000-0000-0000-{tenant:D12}")),
            new OrganizationId(Guid.Parse($"00000000-0000-0000-0000-{organization:D12}")));
    }
}
