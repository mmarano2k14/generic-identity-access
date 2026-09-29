using OrganizationDirectory.Application.Storage;
using OrganizationDirectory.Domain;

namespace OrganizationDirectory.Application.Administration
{
    /// <summary>Coordinates explicit organization membership while keeping authorization separate.</summary>
    public sealed class OrganizationMembershipAdministrationService(
        IOrganizationStore organizationStore,
        IOrganizationMembershipStore membershipStore,
        ITenantMembershipReferenceReader tenantMembershipReader,
        IOrganizationClock clock) : IOrganizationMembershipAdministrationService
    {
        /// <inheritdoc />
        public async Task<IReadOnlyList<OrganizationMembership>> ListForOrganizationAsync(
            OrganizationReference organization,
            int offset,
            int limit,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(organization);

            await RequireOrganizationAsync(organization, cancellationToken)
                .ConfigureAwait(false);

            return await membershipStore.ListForOrganizationAsync(
                    organization,
                    offset,
                    limit,
                    cancellationToken)
                .ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<IReadOnlyList<OrganizationMembership>> ListForTenantMembershipAsync(
            TenantMembershipReference tenantMembership,
            int offset,
            int limit,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(tenantMembership);

            await RequireTenantMembershipAsync(
                    tenantMembership,
                    requireActive: false,
                    cancellationToken)
                .ConfigureAwait(false);

            return await membershipStore.ListForTenantMembershipAsync(
                    tenantMembership,
                    offset,
                    limit,
                    cancellationToken)
                .ConfigureAwait(false);
        }

        /// <inheritdoc />
        public Task<OrganizationMembership?> GetAsync(
            OrganizationReference organization,
            TenantMembershipReference tenantMembership,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(organization);
            ArgumentNullException.ThrowIfNull(tenantMembership);
            EnsureSameBoundary(organization, tenantMembership);

            return membershipStore.GetAsync(
                organization,
                tenantMembership,
                cancellationToken);
        }

        /// <inheritdoc />
        public async Task<OrganizationMembership> AddAsync(
            OrganizationReference organization,
            TenantMembershipReference tenantMembership,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(organization);
            ArgumentNullException.ThrowIfNull(tenantMembership);
            EnsureSameBoundary(organization, tenantMembership);

            var organizationState = await RequireOrganizationAsync(
                    organization,
                    cancellationToken)
                .ConfigureAwait(false);

            if (organizationState.Status != OrganizationStatus.Active)
            {
                throw new OrganizationMembershipOrganizationInactiveException(
                    "Tenant members can only be added to active organizations.");
            }

            await RequireTenantMembershipAsync(
                    tenantMembership,
                    requireActive: true,
                    cancellationToken)
                .ConfigureAwait(false);

            var now = clock.UtcNow;
            var membership = new OrganizationMembership(
                organization,
                tenantMembership,
                OrganizationMembershipStatus.Active,
                rowVersion: 0,
                createdAt: now,
                updatedAt: now);

            return await membershipStore.CreateAsync(membership, cancellationToken)
                .ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<OrganizationMembership?> SetStatusAsync(
            OrganizationReference organization,
            TenantMembershipReference tenantMembership,
            OrganizationMembershipStatus status,
            long expectedRowVersion,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(organization);
            ArgumentNullException.ThrowIfNull(tenantMembership);
            EnsureSameBoundary(organization, tenantMembership);

            if (!Enum.IsDefined(status))
                throw new ArgumentOutOfRangeException(nameof(status));
            if (expectedRowVersion <= 0)
                throw new ArgumentOutOfRangeException(nameof(expectedRowVersion));

            var current = await membershipStore.GetAsync(
                    organization,
                    tenantMembership,
                    cancellationToken)
                .ConfigureAwait(false);

            if (current is null)
            {
                return null;
            }

            if (status == OrganizationMembershipStatus.Active)
            {
                var organizationState = await RequireOrganizationAsync(
                        organization,
                        cancellationToken)
                    .ConfigureAwait(false);

                if (organizationState.Status != OrganizationStatus.Active)
                {
                    throw new OrganizationMembershipOrganizationInactiveException(
                        "Organization membership cannot be activated while the organization is inactive.");
                }

                await RequireTenantMembershipAsync(
                        tenantMembership,
                        requireActive: true,
                        cancellationToken)
                    .ConfigureAwait(false);
            }

            var updated = new OrganizationMembership(
                current.Organization,
                current.TenantMembership,
                status,
                expectedRowVersion,
                current.CreatedAt,
                clock.UtcNow);

            return await membershipStore.UpdateAsync(updated, cancellationToken)
                .ConfigureAwait(false);
        }

        /// <inheritdoc />
        public Task<bool> RemoveAsync(
            OrganizationReference organization,
            TenantMembershipReference tenantMembership,
            long expectedRowVersion,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(organization);
            ArgumentNullException.ThrowIfNull(tenantMembership);
            EnsureSameBoundary(organization, tenantMembership);

            if (expectedRowVersion <= 0)
                throw new ArgumentOutOfRangeException(nameof(expectedRowVersion));

            return membershipStore.DeleteAsync(
                organization,
                tenantMembership,
                expectedRowVersion,
                cancellationToken);
        }

        private async Task<Organization> RequireOrganizationAsync(
            OrganizationReference reference,
            CancellationToken cancellationToken)
        {
            var organization = await organizationStore.GetAsync(reference, cancellationToken)
                .ConfigureAwait(false);

            return organization ??
                throw new OrganizationMembershipReferenceNotFoundException(
                    $"Organization '{reference.OrganizationId}' was not found in the tenant.");
        }

        private async Task<TenantMembershipReferenceState> RequireTenantMembershipAsync(
            TenantMembershipReference reference,
            bool requireActive,
            CancellationToken cancellationToken)
        {
            var state = await tenantMembershipReader.GetAsync(reference, cancellationToken)
                .ConfigureAwait(false);

            if (state is null)
            {
                throw new TenantMembershipReferenceNotFoundException(
                    $"Tenant membership '{reference.TenantMembershipId}' was not found in Identity Access.");
            }

            if (requireActive && !state.IsActive)
            {
                throw new TenantMembershipInactiveException(
                    $"Tenant membership '{reference.TenantMembershipId}' is inactive.");
            }

            return state;
        }

        private static void EnsureSameBoundary(
            OrganizationReference organization,
            TenantMembershipReference tenantMembership)
        {
            if (organization.IdentityScopeId != tenantMembership.IdentityScopeId ||
                organization.TenantId != tenantMembership.TenantId)
            {
                throw new ArgumentException(
                    "Organization membership cannot cross identity-scope or tenant boundaries.",
                    nameof(tenantMembership));
            }
        }
    }
}
