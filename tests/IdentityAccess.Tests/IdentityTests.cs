using IdentityAccess.Domain;

namespace IdentityAccess.Tests
{

    public sealed class IdentityTests
    {
        [Fact]
        public void Subject_identity_includes_the_identity_scope()
        {
            var userId = Guid.NewGuid();
            Assert.NotEqual(new SubjectReference(Guid.NewGuid(), userId),
                new SubjectReference(Guid.NewGuid(), userId));
        }

        [Fact]
        public void Equivalent_subject_references_compare_equal()
        {
            var scopeId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            Assert.Equal(new SubjectReference(scopeId, userId), new SubjectReference(scopeId, userId));
        }

        [Fact]
        public void Subject_scope_cannot_be_empty() =>
            Assert.Throws<ArgumentException>(() => new SubjectReference(Guid.Empty, Guid.NewGuid()));

        [Fact]
        public void User_identifier_cannot_be_empty() =>
            Assert.Throws<ArgumentException>(() => new SubjectReference(Guid.NewGuid(), Guid.Empty));

        [Fact]
        public void Tenant_scope_cannot_be_empty() =>
            Assert.Throws<ArgumentException>(() => new TenantReference(Guid.Empty, Guid.NewGuid()));

        [Fact]
        public void Tenant_identifier_cannot_be_empty() =>
            Assert.Throws<ArgumentException>(() => new TenantReference(Guid.NewGuid(), Guid.Empty));

        [Theory]
        [InlineData("app-a")]
        [InlineData("app-b")]
        [InlineData("a")]
        [InlineData("application-2")]
        public void Application_keys_preserve_their_exact_value(string value) =>
            Assert.Equal(value, new ApplicationKey(value).Value);

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData("APP-A")]
        [InlineData(" app-a")]
        [InlineData("app-a ")]
        [InlineData("1app-a")]
        [InlineData("../app-a")]
        [InlineData("app-a\n")]
        public void Invalid_application_keys_are_rejected(string? value) =>
            Assert.ThrowsAny<ArgumentException>(() => new ApplicationKey(value!));

        [Fact]
        public void Application_keys_have_a_bounded_length() =>
            Assert.Throws<ArgumentException>(() => new ApplicationKey(new string('a', 65)));

        [Fact]
        public void Group_identity_includes_the_application()
        {
            var tenant = new TenantReference(Guid.NewGuid(), Guid.NewGuid());
            var groupId = Guid.NewGuid();
            Assert.NotEqual(new GroupReference(tenant, new("app-a"), groupId),
                new GroupReference(tenant, new("app-b"), groupId));
        }

        [Fact]
        public void Group_identity_includes_the_tenant()
        {
            var scopeId = Guid.NewGuid();
            var groupId = Guid.NewGuid();
            var app = new ApplicationKey("app-a");
            Assert.NotEqual(new GroupReference(new(scopeId, Guid.NewGuid()), app, groupId),
                new GroupReference(new(scopeId, Guid.NewGuid()), app, groupId));
        }

        [Fact]
        public void Group_identifier_cannot_be_empty() =>
            Assert.Throws<ArgumentException>(() => new GroupReference(
                new(Guid.NewGuid(), Guid.NewGuid()), new("app-a"), Guid.Empty));

        [Fact]
        public void Display_name_does_not_define_user_identity()
        {
            var subject = new SubjectReference(Guid.NewGuid(), Guid.NewGuid());
            var first = new User(subject, "Initial name");
            var renamed = new User(subject, "Different name");
            Assert.Equal(first.Subject, renamed.Subject);
            Assert.NotEqual(first.DisplayName, renamed.DisplayName);
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData("line\nfeed")]
        public void Invalid_display_names_are_rejected(string name) =>
            Assert.Throws<ArgumentException>(() => new User(new(Guid.NewGuid(), Guid.NewGuid()), name));

        [Fact]
        public void Long_display_names_are_rejected() =>
            Assert.Throws<ArgumentException>(() => new User(new(Guid.NewGuid(), Guid.NewGuid()), new string('x', 201)));

        [Fact]
        public void Invalid_account_status_is_rejected() =>
            Assert.Throws<ArgumentOutOfRangeException>(() => new User(
                new(Guid.NewGuid(), Guid.NewGuid()), "User", (UserStatus)99));
    }
}
