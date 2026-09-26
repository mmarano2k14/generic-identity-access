using IdentityAccess.Application.Storage;
using IdentityAccess.Domain;

namespace IdentityAccess.Application.Administration
{
    /// <summary>Tenant-scoped administration for bindings to shared managed-policy versions.</summary>
    public interface IManagedPolicyBindingAdministrationService
    {
        Task<IReadOnlyList<VersionedRecord<ManagedPolicy>>> ListAvailablePoliciesAsync(
            Guid identityScopeId,
            Guid tenantId,
            ApplicationKey application,
            string? search,
            int offset,
            int limit,
            CancellationToken cancellationToken);

        Task<IReadOnlyList<ManagedGroupPolicyBinding>> ListBindingsAsync(
            Guid identityScopeId,
            Guid tenantId,
            ApplicationKey application,
            Guid groupId,
            CancellationToken cancellationToken);

        Task<ManagedGroupPolicyBinding?> AddBindingAsync(
            Guid identityScopeId,
            Guid tenantId,
            ApplicationKey application,
            Guid groupId,
            Guid policyId,
            int? policyVersion,
            Guid? resourceScopeId,
            bool includeDescendants,
            CancellationToken cancellationToken);

        Task<bool> RemoveBindingAsync(
            Guid identityScopeId,
            Guid tenantId,
            ApplicationKey application,
            Guid groupId,
            Guid policyId,
            int policyVersion,
            Guid? resourceScopeId,
            CancellationToken cancellationToken);
    }
}
