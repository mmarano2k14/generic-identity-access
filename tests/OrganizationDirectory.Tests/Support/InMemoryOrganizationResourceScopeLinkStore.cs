using OrganizationDirectory.Application.Storage;
using OrganizationDirectory.Domain;

namespace OrganizationDirectory.Tests.Support
{
    /// <summary>In-memory Organization ResourceScope-link store for application tests.</summary>
    internal sealed class InMemoryOrganizationResourceScopeLinkStore :
        IOrganizationResourceScopeLinkStore
    {
        private readonly Dictionary<
            (OrganizationReference Organization, ApplicationKey Application),
            OrganizationResourceScopeLink> _links = new();

        public Task<OrganizationResourceScopeLink?> GetAsync(
            OrganizationReference organization,
            ApplicationKey application,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            _links.TryGetValue((organization, application), out var link);
            return Task.FromResult(link);
        }

        public Task<IReadOnlyList<OrganizationResourceScopeLink>> ListAsync(
            OrganizationReference organization,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            IReadOnlyList<OrganizationResourceScopeLink> result = _links.Values
                .Where(item => item.Organization == organization)
                .OrderBy(item => item.ResourceScope.ApplicationKey.Value, StringComparer.Ordinal)
                .ToArray();

            return Task.FromResult(result);
        }

        public Task<OrganizationResourceScopeLink> CreateAsync(
            OrganizationResourceScopeLink link,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var key = (link.Organization, link.ResourceScope.ApplicationKey);

            if (_links.ContainsKey(key))
            {
                throw new OrganizationResourceScopeLinkAlreadyExistsException(
                    "Organization already has a ResourceScope link for the application.");
            }

            if (_links.Values.Any(item =>
                    item.ResourceScope.IdentityScopeId == link.ResourceScope.IdentityScopeId &&
                    item.ResourceScope.TenantId == link.ResourceScope.TenantId &&
                    item.ResourceScope.ApplicationKey == link.ResourceScope.ApplicationKey &&
                    item.ResourceScope.ResourceScopeId == link.ResourceScope.ResourceScopeId))
            {
                throw new OrganizationResourceScopeAlreadyLinkedException(
                    "ResourceScope already belongs to another Organization.");
            }

            var persisted = Copy(link, rowVersion: 1);
            _links.Add(key, persisted);
            return Task.FromResult(persisted);
        }

        public Task<OrganizationResourceScopeLink?> UpdateAsync(
            OrganizationResourceScopeLink link,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var key = (link.Organization, link.ResourceScope.ApplicationKey);

            if (!_links.TryGetValue(key, out var current))
            {
                return Task.FromResult<OrganizationResourceScopeLink?>(null);
            }

            if (current.RowVersion != link.RowVersion)
            {
                throw new OrganizationResourceScopeLinkConcurrencyException(
                    "Stale Organization ResourceScope-link row version.");
            }

            if (_links.Values.Any(item =>
                    item.Organization != link.Organization &&
                    item.ResourceScope.IdentityScopeId == link.ResourceScope.IdentityScopeId &&
                    item.ResourceScope.TenantId == link.ResourceScope.TenantId &&
                    item.ResourceScope.ApplicationKey == link.ResourceScope.ApplicationKey &&
                    item.ResourceScope.ResourceScopeId == link.ResourceScope.ResourceScopeId))
            {
                throw new OrganizationResourceScopeAlreadyLinkedException(
                    "ResourceScope already belongs to another Organization.");
            }

            var persisted = Copy(link, link.RowVersion + 1);
            _links[key] = persisted;
            return Task.FromResult<OrganizationResourceScopeLink?>(persisted);
        }

        public Task<bool> DeleteAsync(
            OrganizationReference organization,
            ApplicationKey application,
            long expectedRowVersion,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var key = (organization, application);

            if (!_links.TryGetValue(key, out var current))
            {
                return Task.FromResult(false);
            }

            if (current.RowVersion != expectedRowVersion)
            {
                throw new OrganizationResourceScopeLinkConcurrencyException(
                    "Stale Organization ResourceScope-link row version.");
            }

            _links.Remove(key);
            return Task.FromResult(true);
        }

        private static OrganizationResourceScopeLink Copy(
            OrganizationResourceScopeLink link,
            long rowVersion) =>
            new(
                link.Organization,
                link.ResourceScope,
                link.Status,
                rowVersion,
                link.CreatedAt,
                link.UpdatedAt);
    }
}
