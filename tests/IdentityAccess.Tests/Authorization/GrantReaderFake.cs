using IdentityAccess.Application.Authorization;
using IdentityAccess.Application.Routing;
using IdentityAccess.Authorization;
using IdentityAccess.Domain;
using IdentityAccess.Rbac;

namespace IdentityAccess.Tests.Authorization
{

    internal sealed class GrantReaderFake : IAssignedCapabilityReader
    {
        private readonly IReadOnlyList<AssignedCapabilityGrant> _grants;
        private readonly Exception? _exception;
        public int CallCount { get; private set; }
        public ResolvedDatabaseRoute? LastRoute { get; private set; }
        public ResourceScopeReference? LastResourceScope { get; private set; }

        public GrantReaderFake(IReadOnlyList<AssignedCapabilityGrant> grants) => _grants = grants;
        public GrantReaderFake(Exception exception)
        {
            _exception = exception;
            _grants = [];
        }

        public Task<IReadOnlyList<AssignedCapabilityGrant>> ListAsync(
            ResolvedDatabaseRoute route,
            TenantReference tenant,
            SubjectReference subject,
            ApplicationKey application,
            ResourceScopeReference? resourceScope,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            CallCount++;
            LastRoute = route;
            LastResourceScope = resourceScope;
            if (_exception is not null) throw _exception;
            return Task.FromResult(_grants);
        }
    }
}
