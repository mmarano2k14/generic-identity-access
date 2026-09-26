using IdentityAccess.Application.Authorization;
using IdentityAccess.Application.Routing;
using IdentityAccess.Authorization;
using IdentityAccess.Domain;
using IdentityAccess.Rbac;

namespace IdentityAccess.Tests.Authorization
{

    internal sealed class ConcurrentGrantReaderFake : IAssignedCapabilityReader
    {
        public System.Collections.Concurrent.ConcurrentBag<Guid> SeenScopes { get; } = [];

        public Task<IReadOnlyList<AssignedCapabilityGrant>> ListAsync(
            ResolvedDatabaseRoute route,
            TenantReference tenant,
            SubjectReference subject,
            ApplicationKey application,
            ResourceScopeReference? resourceScope,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Assert.Equal(tenant.IdentityScopeId, route.Request.IdentityScopeId);
            SeenScopes.Add(tenant.IdentityScopeId);
            IReadOnlyList<AssignedCapabilityGrant> result =
            [
                new AssignedCapabilityGrant(
                    subject,
                    tenant,
                    application,
                    new GroupReference(tenant, application, Guid.NewGuid()),
                    new ManagedPolicyVersionReference(new ManagedPolicyReference(tenant.IdentityScopeId, application, Guid.NewGuid()), 1),
                    Guid.NewGuid(),
                    new ApplicationSecurityModelReference(tenant.IdentityScopeId, application, 1),
                    new CapabilityKey("billing", "invoice", "read"))
            ];
            return Task.FromResult(result);
        }
    }
}
