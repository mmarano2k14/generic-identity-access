using OrganizationDirectory.Application.Storage;
using OrganizationDirectory.Domain;

namespace OrganizationDirectory.Tests.Support
{
    /// <summary>In-memory organization store used by application-service tests.</summary>
    internal sealed class InMemoryOrganizationStore : IOrganizationStore
    {
        private readonly Dictionary<OrganizationReference, Organization> _organizations = new();

        public Task<Organization?> GetAsync(
            OrganizationReference reference,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            _organizations.TryGetValue(reference, out var organization);
            return Task.FromResult(organization);
        }

        public Task<Organization?> FindByKeyAsync(
            IdentityScopeId identityScopeId,
            TenantId tenantId,
            OrganizationKey key,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var organization = _organizations.Values.SingleOrDefault(item =>
                item.Reference.IdentityScopeId == identityScopeId &&
                item.Reference.TenantId == tenantId &&
                item.Key == key);

            return Task.FromResult(organization);
        }

        public Task<IReadOnlyList<Organization>> ListAsync(
            IdentityScopeId identityScopeId,
            TenantId tenantId,
            int offset,
            int limit,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            IReadOnlyList<Organization> organizations = _organizations.Values
                .Where(item =>
                    item.Reference.IdentityScopeId == identityScopeId &&
                    item.Reference.TenantId == tenantId)
                .OrderBy(item => item.Key.Value, StringComparer.Ordinal)
                .ThenBy(item => item.Reference.OrganizationId.Value)
                .Skip(offset)
                .Take(limit)
                .ToArray();

            return Task.FromResult(organizations);
        }

        public Task<Organization> CreateAsync(
            Organization organization,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (_organizations.ContainsKey(organization.Reference))
                throw new OrganizationIdentityConflictException("Duplicate organization identity.");

            if (_organizations.Values.Any(item =>
                    item.Reference.IdentityScopeId == organization.Reference.IdentityScopeId &&
                    item.Reference.TenantId == organization.Reference.TenantId &&
                    item.Key == organization.Key))
            {
                throw new OrganizationKeyConflictException("Duplicate organization key.");
            }

            var persisted = Copy(organization, rowVersion: 1);
            _organizations.Add(persisted.Reference, persisted);
            return Task.FromResult(persisted);
        }

        public Task<Organization?> UpdateAsync(
            Organization organization,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!_organizations.TryGetValue(organization.Reference, out var current))
                return Task.FromResult<Organization?>(null);

            if (current.RowVersion != organization.RowVersion)
                throw new OrganizationConcurrencyException("Stale organization row version.");

            var persisted = Copy(organization, organization.RowVersion + 1);
            _organizations[organization.Reference] = persisted;
            return Task.FromResult<Organization?>(persisted);
        }

        public Task<bool> DeleteAsync(
            OrganizationReference reference,
            long expectedRowVersion,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!_organizations.TryGetValue(reference, out var current))
                return Task.FromResult(false);

            if (current.RowVersion != expectedRowVersion)
                throw new OrganizationConcurrencyException("Stale organization row version.");

            if (_organizations.Values.Any(item => item.Parent == reference))
                throw new OrganizationHierarchyConflictException("Organization has children.");

            _organizations.Remove(reference);
            return Task.FromResult(true);
        }

        private static Organization Copy(Organization source, long rowVersion) =>
            new(
                source.Reference,
                source.Key,
                source.DisplayName,
                source.Type,
                source.Parent,
                source.Status,
                rowVersion,
                source.CreatedAt,
                source.UpdatedAt);
    }
}
