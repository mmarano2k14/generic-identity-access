using IdentityAccess.Domain;

namespace IdentityAccess.Application.Administration
{
    /// <summary>Determines whether the authenticated subject has an active association with a tenant.</summary>
    public interface IAdministrationTenantVisibilityService
    {
        /// <summary>Returns whether the subject has an active membership in the requested tenant.</summary>
        Task<bool> HasActiveMembershipAsync(
            Guid identityScopeId,
            ApplicationKey application,
            SubjectReference subject,
            Guid tenantId,
            CancellationToken cancellationToken);
    }
}
