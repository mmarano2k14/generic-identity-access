using OrganizationDirectory.Application.Storage;
using OrganizationDirectory.Domain;

namespace OrganizationDirectory.Tests.Support
{
    /// <summary>In-memory authoritative ResourceScope reader for application tests.</summary>
    internal sealed class InMemoryResourceScopeReferenceReader :
        IResourceScopeReferenceReader
    {
        private readonly Dictionary<
            (IdentityScopeId Scope, TenantId Tenant, ApplicationKey Application, ResourceScopeId ResourceScope),
            ResourceScopeReferenceState> _scopes = new();

        public void Add(
            ResourceScopeReference reference,
            bool isActive = true)
        {
            ArgumentNullException.ThrowIfNull(reference);

            _scopes[
                (
                    reference.IdentityScopeId,
                    reference.TenantId,
                    reference.ApplicationKey,
                    reference.ResourceScopeId
                )] = new ResourceScopeReferenceState(reference, isActive);
        }

        public Task<ResourceScopeReferenceState?> GetAsync(
            IdentityScopeId identityScopeId,
            TenantId tenantId,
            ApplicationKey application,
            ResourceScopeId resourceScopeId,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            _scopes.TryGetValue(
                (
                    identityScopeId,
                    tenantId,
                    application,
                    resourceScopeId
                ),
                out var state);

            return Task.FromResult(state);
        }
    }
}
