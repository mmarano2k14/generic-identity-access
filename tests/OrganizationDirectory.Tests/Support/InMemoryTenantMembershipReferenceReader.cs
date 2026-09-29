using OrganizationDirectory.Application.Storage;
using OrganizationDirectory.Domain;

namespace OrganizationDirectory.Tests.Support
{
    /// <summary>In-memory Identity Access tenant-membership reference reader for application tests.</summary>
    internal sealed class InMemoryTenantMembershipReferenceReader :
        ITenantMembershipReferenceReader
    {
        private readonly Dictionary<TenantMembershipReference, bool> _memberships = new();

        public void Add(
            TenantMembershipReference reference,
            bool isActive = true)
        {
            ArgumentNullException.ThrowIfNull(reference);
            _memberships[reference] = isActive;
        }

        public Task<TenantMembershipReferenceState?> GetAsync(
            TenantMembershipReference reference,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            return Task.FromResult(
                _memberships.TryGetValue(reference, out var isActive)
                    ? new TenantMembershipReferenceState(reference, isActive)
                    : null);
        }
    }
}
