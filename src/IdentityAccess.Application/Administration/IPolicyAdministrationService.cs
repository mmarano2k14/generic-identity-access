using IdentityAccess.Application.Storage;
using IdentityAccess.Domain;

namespace IdentityAccess.Application.Administration
{

    /// <summary>Defines the contract for policy administration service.</summary>
    public interface IPolicyAdministrationService
    {
        /// <summary>Gets the requested permission policy.</summary>
        Task<VersionedRecord<PermissionPolicy>?> GetPolicyAsync(Guid identityScopeId, Guid tenantId,
            ApplicationKey application, Guid policyId, CancellationToken cancellationToken);
        /// <summary>Creates a permission policy.</summary>
        Task<VersionedRecord<PermissionPolicy>> CreatePolicyAsync(Guid identityScopeId, Guid tenantId,
            ApplicationKey application, Guid policyId, string displayName, PolicyStatus status,
            CancellationToken cancellationToken);
        /// <summary>Updates a permission policy using optimistic concurrency.</summary>
        Task<VersionedRecord<PermissionPolicy>> UpdatePolicyAsync(Guid identityScopeId, Guid tenantId,
            ApplicationKey application, Guid policyId, string displayName, PolicyStatus status, long expectedVersion,
            CancellationToken cancellationToken);
        /// <summary>Lists statements belonging to the requested permission policy.</summary>
        Task<IReadOnlyList<PolicyStatement>> ListStatementsAsync(Guid identityScopeId, Guid tenantId,
            ApplicationKey application, Guid policyId, CancellationToken cancellationToken);
        /// <summary>Adds a capability statement to the requested permission policy.</summary>
        Task<PolicyStatement> AddStatementAsync(Guid identityScopeId, Guid tenantId, ApplicationKey application,
            Guid policyId, Guid statementId, int modelVersion, CapabilityPattern pattern,
            CancellationToken cancellationToken);
        /// <summary>Removes a capability statement from the requested permission policy.</summary>
        Task<bool> RemoveStatementAsync(Guid identityScopeId, Guid tenantId, ApplicationKey application,
            Guid policyId, Guid statementId, CancellationToken cancellationToken);
        /// <summary>Lists policy bindings for the requested user group.</summary>
        Task<IReadOnlyList<GroupPolicyBinding>> ListBindingsAsync(Guid identityScopeId, Guid tenantId,
            ApplicationKey application, Guid groupId, CancellationToken cancellationToken);
        /// <summary>Adds a policy binding to the requested user group.</summary>
        Task<GroupPolicyBinding?> AddBindingAsync(Guid identityScopeId, Guid tenantId, ApplicationKey application,
            Guid groupId, Guid policyId, CancellationToken cancellationToken);
        /// <summary>Adds a policy binding to the requested user group.</summary>
        Task<GroupPolicyBinding?> AddBindingAsync(Guid identityScopeId, Guid tenantId, ApplicationKey application,
            Guid groupId, Guid policyId, Guid? resourceScopeId, bool includeDescendants,
            CancellationToken cancellationToken);
        /// <summary>Removes a policy binding from the requested user group.</summary>
        Task<bool> RemoveBindingAsync(Guid identityScopeId, Guid tenantId, ApplicationKey application,
            Guid groupId, Guid policyId, CancellationToken cancellationToken);
        /// <summary>Removes a policy binding from the requested user group.</summary>
        Task<bool> RemoveBindingAsync(Guid identityScopeId, Guid tenantId, ApplicationKey application,
            Guid groupId, Guid policyId, Guid? resourceScopeId, CancellationToken cancellationToken);
    }
}
