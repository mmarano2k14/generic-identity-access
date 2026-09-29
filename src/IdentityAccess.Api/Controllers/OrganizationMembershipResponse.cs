using OrganizationDirectory.Domain;

namespace IdentityAccess.Api.Controllers
{
    /// <summary>Represents explicit organizational belonging without implying authorization.</summary>
    public sealed class OrganizationMembershipResponse
    {
        /// <summary>Gets the identity scope ID.</summary>
        public Guid IdentityScopeId { get; init; }

        /// <summary>Gets the tenant ID.</summary>
        public Guid TenantId { get; init; }

        /// <summary>Gets the organization ID.</summary>
        public Guid OrganizationId { get; init; }

        /// <summary>Gets the existing Identity Access tenant-membership ID.</summary>
        public Guid TenantMembershipId { get; init; }

        /// <summary>Gets organization-membership lifecycle status.</summary>
        public int Status { get; init; }

        /// <summary>Gets optimistic-concurrency row version.</summary>
        public long RowVersion { get; init; }

        /// <summary>Gets creation time.</summary>
        public DateTimeOffset CreatedAt { get; init; }

        /// <summary>Gets last update time.</summary>
        public DateTimeOffset UpdatedAt { get; init; }

        /// <summary>Maps domain state to an API response.</summary>
        public static OrganizationMembershipResponse From(
            OrganizationMembership membership)
        {
            ArgumentNullException.ThrowIfNull(membership);

            return new OrganizationMembershipResponse
            {
                IdentityScopeId = membership.Organization.IdentityScopeId.Value,
                TenantId = membership.Organization.TenantId.Value,
                OrganizationId = membership.Organization.OrganizationId.Value,
                TenantMembershipId = membership.TenantMembership.TenantMembershipId.Value,
                Status = (int)membership.Status,
                RowVersion = membership.RowVersion,
                CreatedAt = membership.CreatedAt,
                UpdatedAt = membership.UpdatedAt
            };
        }
    }
}
