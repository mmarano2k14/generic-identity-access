using IdentityAccess.Application.Administration;
using IdentityAccess.Application.Authorization;
using IdentityAccess.Application.Storage;
using IdentityAccess.Domain;

namespace IdentityAccess.Tests.Administration
{
    internal sealed class EffectiveAdministrationContextServiceFixture
    {
        public Guid ScopeId { get; } = Guid.NewGuid();
        public ApplicationKey Application { get; } = new("admin-web");
        public SubjectReference Subject { get; }
        public EffectiveAdministrationContextFakeRouteResolver RouteResolver { get; }
        public List<VersionedRecord<TenantMembership>> Memberships { get; } = [];
        public IEffectiveAdministrationContextService Service { get; }

        public EffectiveAdministrationContextServiceFixture(bool withScopeGrant)
        {
            Subject = new SubjectReference(ScopeId, Guid.NewGuid());
            RouteResolver = new EffectiveAdministrationContextFakeRouteResolver();
            var grants = new EffectiveAdministrationContextFakeScopeGrantReader();
            if (withScopeGrant)
            {
                grants.Grants.Add(new AssignedIdentityScopeCapabilityGrant(
                    ScopeId, Subject, Application, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
                    new ApplicationSecurityModelReference(ScopeId, Application, 1),
                    new CapabilityPattern("identity-access", "*", "*")));
            }

            Service = new EffectiveAdministrationContextService(
                RouteResolver, grants, new EffectiveAdministrationContextFakeTenantMembershipStore(Memberships));
        }

        public VersionedRecord<TenantMembership> Membership(MembershipStatus status) =>
            new(new TenantMembership(
                Guid.NewGuid(), new TenantReference(ScopeId, Guid.NewGuid()), Subject, status), 1);
    }
}
