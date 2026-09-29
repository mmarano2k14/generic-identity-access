using OrganizationDirectory.Domain;

namespace IdentityAccess.Api.Controllers
{
    /// <summary>Represents the application-aware ResourceScope linked to one Organization.</summary>
    public sealed class OrganizationResourceScopeLinkResponse
    {
        /// <summary>Gets the Organization ID.</summary>
        public Guid OrganizationId { get; init; }

        /// <summary>Gets the linked application's key.</summary>
        public string ApplicationKey { get; init; } = string.Empty;

        /// <summary>Gets the linked Identity Access ResourceScope ID.</summary>
        public Guid ResourceScopeId { get; init; }

        /// <summary>Gets the authoritative ResourceScope type.</summary>
        public string ScopeType { get; init; } = string.Empty;

        /// <summary>Gets the authoritative security-model version.</summary>
        public int ModelVersion { get; init; }

        /// <summary>Gets link lifecycle status.</summary>
        public int Status { get; init; }

        /// <summary>Gets optimistic-concurrency row version.</summary>
        public long RowVersion { get; init; }

        /// <summary>Gets creation time.</summary>
        public DateTimeOffset CreatedAt { get; init; }

        /// <summary>Gets last update time.</summary>
        public DateTimeOffset UpdatedAt { get; init; }

        /// <summary>Maps domain state to an administration response.</summary>
        public static OrganizationResourceScopeLinkResponse From(
            OrganizationResourceScopeLink link)
        {
            ArgumentNullException.ThrowIfNull(link);

            return new OrganizationResourceScopeLinkResponse
            {
                OrganizationId = link.Organization.OrganizationId.Value,
                ApplicationKey = link.ResourceScope.ApplicationKey.Value,
                ResourceScopeId = link.ResourceScope.ResourceScopeId.Value,
                ScopeType = link.ResourceScope.ScopeType.Value,
                ModelVersion = link.ResourceScope.ModelVersion.Value,
                Status = (int)link.Status,
                RowVersion = link.RowVersion,
                CreatedAt = link.CreatedAt,
                UpdatedAt = link.UpdatedAt
            };
        }
    }
}
