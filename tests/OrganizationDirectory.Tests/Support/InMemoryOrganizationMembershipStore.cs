using OrganizationDirectory.Application.Storage;
using OrganizationDirectory.Domain;

namespace OrganizationDirectory.Tests.Support
{
    /// <summary>In-memory organization-membership store for application tests.</summary>
    internal sealed class InMemoryOrganizationMembershipStore :
        IOrganizationMembershipStore
    {
        private readonly Dictionary<
            (OrganizationReference Organization, TenantMembershipReference TenantMembership),
            OrganizationMembership> _memberships = new();

        public Task<OrganizationMembership?> GetAsync(
            OrganizationReference organization,
            TenantMembershipReference tenantMembership,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            _memberships.TryGetValue((organization, tenantMembership), out var membership);
            return Task.FromResult(membership);
        }

        public Task<IReadOnlyList<OrganizationMembership>> ListForOrganizationAsync(
            OrganizationReference organization,
            int offset,
            int limit,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            IReadOnlyList<OrganizationMembership> result = _memberships.Values
                .Where(item => item.Organization == organization)
                .OrderBy(item => item.TenantMembership.TenantMembershipId.Value)
                .Skip(offset)
                .Take(limit)
                .ToArray();

            return Task.FromResult(result);
        }

        public Task<IReadOnlyList<OrganizationMembership>> ListForTenantMembershipAsync(
            TenantMembershipReference tenantMembership,
            int offset,
            int limit,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            IReadOnlyList<OrganizationMembership> result = _memberships.Values
                .Where(item => item.TenantMembership == tenantMembership)
                .OrderBy(item => item.Organization.OrganizationId.Value)
                .Skip(offset)
                .Take(limit)
                .ToArray();

            return Task.FromResult(result);
        }

        public Task<OrganizationMembership> CreateAsync(
            OrganizationMembership membership,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var key = (membership.Organization, membership.TenantMembership);
            if (_memberships.ContainsKey(key))
            {
                throw new OrganizationMembershipAlreadyExistsException(
                    "Organization membership already exists.");
            }

            var persisted = Copy(membership, rowVersion: 1);
            _memberships.Add(key, persisted);
            return Task.FromResult(persisted);
        }

        public Task<OrganizationMembership?> UpdateAsync(
            OrganizationMembership membership,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var key = (membership.Organization, membership.TenantMembership);
            if (!_memberships.TryGetValue(key, out var current))
            {
                return Task.FromResult<OrganizationMembership?>(null);
            }

            if (current.RowVersion != membership.RowVersion)
            {
                throw new OrganizationMembershipConcurrencyException(
                    "Stale organization membership row version.");
            }

            var persisted = Copy(
                membership,
                membership.RowVersion + 1);

            _memberships[key] = persisted;
            return Task.FromResult<OrganizationMembership?>(persisted);
        }

        public Task<bool> DeleteAsync(
            OrganizationReference organization,
            TenantMembershipReference tenantMembership,
            long expectedRowVersion,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var key = (organization, tenantMembership);
            if (!_memberships.TryGetValue(key, out var current))
            {
                return Task.FromResult(false);
            }

            if (current.RowVersion != expectedRowVersion)
            {
                throw new OrganizationMembershipConcurrencyException(
                    "Stale organization membership row version.");
            }

            _memberships.Remove(key);
            return Task.FromResult(true);
        }

        private static OrganizationMembership Copy(
            OrganizationMembership membership,
            long rowVersion) =>
            new(
                membership.Organization,
                membership.TenantMembership,
                membership.Status,
                rowVersion,
                membership.CreatedAt,
                membership.UpdatedAt);
    }
}
