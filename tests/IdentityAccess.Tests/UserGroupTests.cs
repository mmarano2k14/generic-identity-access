using IdentityAccess.Domain;

namespace IdentityAccess.Tests
{
    public sealed class UserGroupTests
    {
        [Fact]
        public void Normal_group_is_not_reusable_by_default()
        {
            var group = new UserGroup(Reference(), "Operators");

            Assert.False(group.IsTemplate);
        }

        [Fact]
        public void Reusable_marker_is_part_of_the_real_group_state()
        {
            var group = new UserGroup(Reference(), "Finance Administrators", GroupStatus.Active, true);

            Assert.True(group.IsTemplate);
            Assert.Equal("Finance Administrators", group.DisplayName);
        }

        private static GroupReference Reference() =>
            new(
                new TenantReference(
                    Guid.Parse("b1000000-0000-0000-0000-000000000001"),
                    Guid.Parse("b1000000-0000-0000-0000-000000000002")),
                new ApplicationKey("admin-web"),
                Guid.Parse("b1000000-0000-0000-0000-000000000003"));
    }
}
