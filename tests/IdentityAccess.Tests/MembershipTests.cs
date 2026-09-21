using IdentityAccess.Domain;

namespace IdentityAccess.Tests;

public sealed class MembershipTests
{
    [Fact]
    public void A_user_may_belong_to_two_tenants_in_the_same_identity_scope()
    {
        var scopeId = Guid.NewGuid();
        var subject = new SubjectReference(scopeId, Guid.NewGuid());
        var first = new TenantMembership(Guid.NewGuid(), new(scopeId, Guid.NewGuid()), subject);
        var second = new TenantMembership(Guid.NewGuid(), new(scopeId, Guid.NewGuid()), subject);
        Assert.Equal(first.Subject, second.Subject);
        Assert.NotEqual(first.Tenant, second.Tenant);
    }

    [Fact]
    public void Membership_cannot_cross_identity_scopes() =>
        Assert.Throws<ArgumentException>(() => new TenantMembership(Guid.NewGuid(),
            new(Guid.NewGuid(), Guid.NewGuid()), new(Guid.NewGuid(), Guid.NewGuid())));

    [Fact]
    public void Membership_identifier_cannot_be_empty()
    {
        var scopeId = Guid.NewGuid();
        Assert.Throws<ArgumentException>(() => new TenantMembership(Guid.Empty,
            new(scopeId, Guid.NewGuid()), new(scopeId, Guid.NewGuid())));
    }

    [Fact]
    public void Active_membership_can_join_an_active_group_in_the_same_tenant()
    {
        var tenant = new TenantReference(Guid.NewGuid(), Guid.NewGuid());
        var membership = new TenantMembership(Guid.NewGuid(), tenant, new(tenant.IdentityScopeId, Guid.NewGuid()));
        var group = new UserGroup(new(tenant, new("magellan"), Guid.NewGuid()), "Support");
        var result = GroupMembership.Create(group, membership);
        Assert.Equal(group.Reference, result.Group);
        Assert.Equal(membership.MembershipId, result.TenantMembershipId);
        Assert.Equal(membership.Subject, result.Subject);
    }

    [Fact]
    public void Group_membership_cannot_cross_tenants()
    {
        var scopeId = Guid.NewGuid();
        var membership = new TenantMembership(Guid.NewGuid(), new(scopeId, Guid.NewGuid()), new(scopeId, Guid.NewGuid()));
        var group = new UserGroup(new(new(scopeId, Guid.NewGuid()), new("magellan"), Guid.NewGuid()), "Support");
        Assert.Throws<ArgumentException>(() => GroupMembership.Create(group, membership));
    }

    [Fact]
    public void Matching_tenant_identifiers_in_different_scopes_do_not_match()
    {
        var tenantId = Guid.NewGuid();
        var firstScope = Guid.NewGuid();
        var membership = new TenantMembership(Guid.NewGuid(), new(firstScope, tenantId), new(firstScope, Guid.NewGuid()));
        var group = new UserGroup(new(new(Guid.NewGuid(), tenantId), new("magellan"), Guid.NewGuid()), "Support");
        Assert.Throws<ArgumentException>(() => GroupMembership.Create(group, membership));
    }

    [Fact]
    public void Suspended_tenant_membership_cannot_join_a_group()
    {
        var tenant = new TenantReference(Guid.NewGuid(), Guid.NewGuid());
        var membership = new TenantMembership(Guid.NewGuid(), tenant, new(tenant.IdentityScopeId, Guid.NewGuid()), MembershipStatus.Suspended);
        var group = new UserGroup(new(tenant, new("magellan"), Guid.NewGuid()), "Support");
        Assert.Throws<InvalidOperationException>(() => GroupMembership.Create(group, membership));
    }

    [Fact]
    public void Suspended_group_does_not_accept_new_members()
    {
        var tenant = new TenantReference(Guid.NewGuid(), Guid.NewGuid());
        var membership = new TenantMembership(Guid.NewGuid(), tenant, new(tenant.IdentityScopeId, Guid.NewGuid()));
        var group = new UserGroup(new(tenant, new("magellan"), Guid.NewGuid()), "Support", GroupStatus.Suspended);
        Assert.Throws<InvalidOperationException>(() => GroupMembership.Create(group, membership));
    }

    [Fact]
    public void Suspending_one_membership_does_not_suspend_the_account()
    {
        var scopeId = Guid.NewGuid();
        var user = new User(new(scopeId, Guid.NewGuid()), "User");
        var suspended = new TenantMembership(Guid.NewGuid(), new(scopeId, Guid.NewGuid()), user.Subject, MembershipStatus.Suspended);
        var active = new TenantMembership(Guid.NewGuid(), new(scopeId, Guid.NewGuid()), user.Subject);
        Assert.Equal(UserStatus.Active, user.Status);
        Assert.Equal(MembershipStatus.Suspended, suspended.Status);
        Assert.Equal(MembershipStatus.Active, active.Status);
    }

    [Fact]
    public void Unknown_membership_status_is_rejected()
    {
        var scopeId = Guid.NewGuid();
        Assert.Throws<ArgumentOutOfRangeException>(() => new TenantMembership(Guid.NewGuid(),
            new(scopeId, Guid.NewGuid()), new(scopeId, Guid.NewGuid()), (MembershipStatus)0));
    }
}
