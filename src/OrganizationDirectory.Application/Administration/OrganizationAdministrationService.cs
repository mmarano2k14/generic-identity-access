using OrganizationDirectory.Application.Storage;
using OrganizationDirectory.Domain;

namespace OrganizationDirectory.Application.Administration
{
    /// <summary>Coordinates organization lifecycle and hierarchy operations over durable storage.</summary>
    public sealed class OrganizationAdministrationService(
        IOrganizationStore store,
        IOrganizationClock clock) : IOrganizationAdministrationService
    {
        private const int TreePageSize = 500;
        private const int MaximumTreeOrganizations = 5000;

        /// <inheritdoc />
        public Task<IReadOnlyList<Organization>> ListAsync(
            IdentityScopeId identityScopeId,
            TenantId tenantId,
            int offset,
            int limit,
            CancellationToken cancellationToken) =>
            store.ListAsync(identityScopeId, tenantId, offset, limit, cancellationToken);

        /// <inheritdoc />
        public Task<Organization?> GetAsync(
            OrganizationReference reference,
            CancellationToken cancellationToken) =>
            store.GetAsync(reference, cancellationToken);

        /// <inheritdoc />
        public async Task<IReadOnlyList<Organization>> ListChildrenAsync(
            OrganizationReference parent,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(parent);

            var organizations = await LoadTenantOrganizationsAsync(
                    parent.IdentityScopeId,
                    parent.TenantId,
                    cancellationToken)
                .ConfigureAwait(false);

            return organizations
                .Where(item =>
                    item.Parent is not null &&
                    item.Parent.OrganizationId == parent.OrganizationId)
                .OrderBy(item => item.Key.Value, StringComparer.Ordinal)
                .ThenBy(item => item.Reference.OrganizationId.Value)
                .ToArray();
        }

        /// <inheritdoc />
        public async Task<IReadOnlyList<OrganizationTreeNode>> GetTreeAsync(
            IdentityScopeId identityScopeId,
            TenantId tenantId,
            CancellationToken cancellationToken)
        {
            var organizations = await LoadTenantOrganizationsAsync(
                    identityScopeId,
                    tenantId,
                    cancellationToken)
                .ConfigureAwait(false);

            var roots = organizations
                .Where(item => item.Parent is null)
                .OrderBy(item => item.Key.Value, StringComparer.Ordinal)
                .ThenBy(item => item.Reference.OrganizationId.Value)
                .ToArray();

            var byParent = organizations
                .Where(item => item.Parent is not null)
                .GroupBy(item => item.Parent!.OrganizationId)
                .ToDictionary(
                    group => group.Key,
                    group => group
                        .OrderBy(item => item.Key.Value, StringComparer.Ordinal)
                        .ThenBy(item => item.Reference.OrganizationId.Value)
                        .ToArray());

            var activePath = new HashSet<OrganizationId>();

            OrganizationTreeNode Build(Organization organization)
            {
                if (!activePath.Add(organization.Reference.OrganizationId))
                {
                    throw new OrganizationHierarchyConflictException(
                        "Organization hierarchy cycle detected while materializing the tree.");
                }

                try
                {
                    var children = byParent.TryGetValue(
                            organization.Reference.OrganizationId,
                            out var directChildren)
                        ? directChildren.Select(Build).ToArray()
                        : Array.Empty<OrganizationTreeNode>();

                    return new OrganizationTreeNode(organization, children);
                }
                finally
                {
                    activePath.Remove(organization.Reference.OrganizationId);
                }
            }

            return roots.Select(Build).ToArray();
        }

        /// <inheritdoc />
        public async Task<Organization> CreateAsync(
            OrganizationReference reference,
            OrganizationKey key,
            string displayName,
            OrganizationType type,
            OrganizationId? parentOrganizationId,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(reference);
            ArgumentNullException.ThrowIfNull(key);
            ArgumentNullException.ThrowIfNull(type);

            var parent = await ResolveAndValidateParentAsync(
                    reference,
                    parentOrganizationId,
                    cancellationToken)
                .ConfigureAwait(false);

            var now = clock.UtcNow;

            var organization = new Organization(
                reference,
                key,
                displayName,
                type,
                parent?.Reference,
                OrganizationStatus.Active,
                rowVersion: 0,
                createdAt: now,
                updatedAt: now);

            return await store.CreateAsync(organization, cancellationToken)
                .ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<Organization?> UpdateAsync(
            OrganizationReference reference,
            string displayName,
            OrganizationType type,
            OrganizationId? parentOrganizationId,
            long expectedRowVersion,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(reference);
            ArgumentNullException.ThrowIfNull(type);
            if (expectedRowVersion <= 0) throw new ArgumentOutOfRangeException(nameof(expectedRowVersion));

            var current = await store.GetAsync(reference, cancellationToken)
                .ConfigureAwait(false);

            if (current is null)
            {
                return null;
            }

            var parent = await ResolveAndValidateParentAsync(
                    reference,
                    parentOrganizationId,
                    cancellationToken)
                .ConfigureAwait(false);

            if (parent is not null)
            {
                await EnsureParentIsNotDescendantAsync(
                        reference,
                        parent,
                        cancellationToken)
                    .ConfigureAwait(false);
            }

            var updated = new Organization(
                current.Reference,
                current.Key,
                displayName,
                type,
                parent?.Reference,
                current.Status,
                expectedRowVersion,
                current.CreatedAt,
                clock.UtcNow);

            return await store.UpdateAsync(updated, cancellationToken)
                .ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<Organization?> SetStatusAsync(
            OrganizationReference reference,
            OrganizationStatus status,
            long expectedRowVersion,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(reference);
            if (!Enum.IsDefined(status)) throw new ArgumentOutOfRangeException(nameof(status));
            if (expectedRowVersion <= 0) throw new ArgumentOutOfRangeException(nameof(expectedRowVersion));

            var current = await store.GetAsync(reference, cancellationToken)
                .ConfigureAwait(false);

            if (current is null)
            {
                return null;
            }

            var updated = new Organization(
                current.Reference,
                current.Key,
                current.DisplayName,
                current.Type,
                current.Parent,
                status,
                expectedRowVersion,
                current.CreatedAt,
                clock.UtcNow);

            return await store.UpdateAsync(updated, cancellationToken)
                .ConfigureAwait(false);
        }

        private async Task<Organization?> ResolveAndValidateParentAsync(
            OrganizationReference child,
            OrganizationId? parentOrganizationId,
            CancellationToken cancellationToken)
        {
            if (parentOrganizationId is null)
            {
                return null;
            }

            if (parentOrganizationId == child.OrganizationId)
            {
                throw new OrganizationHierarchyConflictException(
                    "Organization cannot parent itself.");
            }

            var parentReference = new OrganizationReference(
                child.IdentityScopeId,
                child.TenantId,
                parentOrganizationId.Value);

            var parent = await store.GetAsync(parentReference, cancellationToken)
                .ConfigureAwait(false);

            if (parent is null)
            {
                throw new OrganizationParentNotFoundException(
                    $"Parent organization '{parentOrganizationId}' was not found in the tenant.");
            }

            return parent;
        }

        private async Task EnsureParentIsNotDescendantAsync(
            OrganizationReference organization,
            Organization proposedParent,
            CancellationToken cancellationToken)
        {
            var visited = new HashSet<OrganizationId>();
            Organization? current = proposedParent;

            while (current is not null)
            {
                if (!visited.Add(current.Reference.OrganizationId))
                {
                    throw new OrganizationHierarchyConflictException(
                        "Organization hierarchy cycle detected.");
                }

                if (current.Reference.OrganizationId == organization.OrganizationId)
                {
                    throw new OrganizationHierarchyConflictException(
                        "Organization cannot be moved below one of its descendants.");
                }

                if (current.Parent is null)
                {
                    return;
                }

                current = await store.GetAsync(current.Parent, cancellationToken)
                    .ConfigureAwait(false);

                if (current is null)
                {
                    throw new OrganizationHierarchyConflictException(
                        "Organization hierarchy contains a missing parent reference.");
                }
            }
        }

        private async Task<IReadOnlyList<Organization>> LoadTenantOrganizationsAsync(
            IdentityScopeId identityScopeId,
            TenantId tenantId,
            CancellationToken cancellationToken)
        {
            var organizations = new List<Organization>();

            for (var offset = 0; offset < MaximumTreeOrganizations; offset += TreePageSize)
            {
                var page = await store.ListAsync(
                        identityScopeId,
                        tenantId,
                        offset,
                        TreePageSize,
                        cancellationToken)
                    .ConfigureAwait(false);

                organizations.AddRange(page);

                if (page.Count < TreePageSize)
                {
                    return organizations;
                }
            }

            throw new InvalidOperationException(
                $"Organization tree exceeds the supported limit of {MaximumTreeOrganizations} organizations.");
        }
    }
}
