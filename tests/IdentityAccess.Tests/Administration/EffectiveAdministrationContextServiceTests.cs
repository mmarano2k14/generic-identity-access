using IdentityAccess.Application.Administration;
using IdentityAccess.Domain;

namespace IdentityAccess.Tests.Administration
{
    public sealed class EffectiveAdministrationContextServiceTests
    {
        [Fact]
        public async Task Active_scope_authority_selects_scope_wide_visibility_and_keeps_active_memberships()
        {
            var fixture = new EffectiveAdministrationContextServiceFixture(withScopeGrant: true);
            fixture.Memberships.Add(fixture.Membership(MembershipStatus.Active));
            fixture.Memberships.Add(fixture.Membership(MembershipStatus.Suspended));

            var result = await fixture.Service.ResolveAsync(
                fixture.ScopeId, fixture.Application, fixture.Subject, CancellationToken.None);

            Assert.Equal(AdministrationTenantVisibilityMode.ScopeWide, result.TenantVisibility);
            Assert.Single(result.ActiveTenantMemberships);
            Assert.Equal(fixture.Subject, result.Subject);
            Assert.Equal(fixture.Application, result.Application);
        }

        [Fact]
        public async Task Subject_without_scope_authority_is_limited_to_active_memberships()
        {
            var fixture = new EffectiveAdministrationContextServiceFixture(withScopeGrant: false);
            var membership = fixture.Membership(MembershipStatus.Active);
            fixture.Memberships.Add(membership);

            var result = await fixture.Service.ResolveAsync(
                fixture.ScopeId, fixture.Application, fixture.Subject, CancellationToken.None);

            Assert.Equal(AdministrationTenantVisibilityMode.MembershipLimited, result.TenantVisibility);
            var visible = Assert.Single(result.ActiveTenantMemberships);
            Assert.Equal(membership.Value.MembershipId, visible.MembershipId);
            Assert.Equal(membership.Value.Tenant, visible.Tenant);
        }

        [Fact]
        public async Task Subject_from_another_identity_scope_is_rejected_before_storage_access()
        {
            var fixture = new EffectiveAdministrationContextServiceFixture(withScopeGrant: false);
            var foreignSubject = new SubjectReference(Guid.NewGuid(), Guid.NewGuid());

            await Assert.ThrowsAsync<ArgumentException>(() => fixture.Service.ResolveAsync(
                fixture.ScopeId, fixture.Application, foreignSubject, CancellationToken.None));

            Assert.Equal(0, fixture.RouteResolver.CallCount);
        }
    }
}
