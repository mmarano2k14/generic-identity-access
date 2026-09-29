using IdentityAccess.Domain;

namespace IdentityAccess.Api.Security
{
    /// <summary>Prevents delegated tenant administrators from assigning or cloning capability-bearing groups beyond their own authority.</summary>
    public interface ITenantGroupAssignmentDelegationGuard
    {
        ValueTask<AdministrationAccessResult> AuthorizeAssignmentAsync(
            AdministrationRequestContext context,
            Guid tenantId,
            ApplicationKey application,
            Guid groupId,
            CancellationToken cancellationToken);

        /// <summary>Authorizes copying one source group's capability grants into a target tenant.</summary>
        ValueTask<AdministrationAccessResult> AuthorizeGrantCopyAsync(
            AdministrationRequestContext context,
            Guid sourceTenantId,
            Guid targetTenantId,
            ApplicationKey application,
            Guid groupId,
            IReadOnlyDictionary<Guid, Guid> resourceScopeMappings,
            CancellationToken cancellationToken);
    }
}
