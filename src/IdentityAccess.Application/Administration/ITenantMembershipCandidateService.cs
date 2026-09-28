using IdentityAccess.Domain;

namespace IdentityAccess.Application.Administration
{
    /// <summary>Resolves one exact login identifier for a tenant-membership workflow.</summary>
    public interface ITenantMembershipCandidateService
    {
        Task<TenantMembershipCandidate?> FindByLoginAsync(Guid identityScopeId, ApplicationKey application,
            Guid tenantId, string loginIdentifier, CancellationToken cancellationToken);
    }
}
