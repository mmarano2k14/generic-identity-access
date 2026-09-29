using OrganizationDirectory.Domain;

namespace IdentityAccess.Api.Controllers
{
    /// <summary>Represents organization state exposed by the administration API.</summary>
    public sealed class OrganizationResponse
    {
        /// <summary>Gets the identity scope ID.</summary>
        public Guid IdentityScopeId { get; init; }

        /// <summary>Gets the tenant ID.</summary>
        public Guid TenantId { get; init; }

        /// <summary>Gets the organization ID.</summary>
        public Guid OrganizationId { get; init; }

        /// <summary>Gets the stable tenant-local organization key.</summary>
        public string OrganizationKey { get; init; } = string.Empty;

        /// <summary>Gets the display name.</summary>
        public string DisplayName { get; init; } = string.Empty;

        /// <summary>Gets the application-defined organization type.</summary>
        public string OrganizationType { get; init; } = string.Empty;

        /// <summary>Gets the optional parent organization ID.</summary>
        public Guid? ParentOrganizationId { get; init; }

        /// <summary>Gets the lifecycle status.</summary>
        public int Status { get; init; }

        /// <summary>Gets the optimistic-concurrency row version.</summary>
        public long RowVersion { get; init; }

        /// <summary>Gets creation time.</summary>
        public DateTimeOffset CreatedAt { get; init; }

        /// <summary>Gets last update time.</summary>
        public DateTimeOffset UpdatedAt { get; init; }

        /// <summary>Maps domain organization state to the API response.</summary>
        public static OrganizationResponse From(Organization organization)
        {
            ArgumentNullException.ThrowIfNull(organization);

            return new OrganizationResponse
            {
                IdentityScopeId = organization.Reference.IdentityScopeId.Value,
                TenantId = organization.Reference.TenantId.Value,
                OrganizationId = organization.Reference.OrganizationId.Value,
                OrganizationKey = organization.Key.Value,
                DisplayName = organization.DisplayName,
                OrganizationType = organization.Type.Value,
                ParentOrganizationId = organization.Parent?.OrganizationId.Value,
                Status = (int)organization.Status,
                RowVersion = organization.RowVersion,
                CreatedAt = organization.CreatedAt,
                UpdatedAt = organization.UpdatedAt
            };
        }
    }
}
