using IdentityAccess.Application.Storage;
using IdentityAccess.Domain;

namespace IdentityAccess.Application.Administration
{
    /// <summary>Administers identity-scope authorization groups, policies, and assignments.</summary>
    public interface IIdentityScopeAuthorityAdministrationService
    {
        /// <summary>Gets a scope-administration group.</summary>
        Task<VersionedRecord<IdentityScopeAdministrationGroup>?> GetGroupAsync(
            Guid identityScopeId, ApplicationKey application, Guid groupId, CancellationToken cancellationToken);

        /// <summary>Creates a scope-administration group.</summary>
        Task<VersionedRecord<IdentityScopeAdministrationGroup>> CreateGroupAsync(
            Guid identityScopeId, ApplicationKey application, Guid groupId, string displayName,
            GroupStatus status, CancellationToken cancellationToken);

        /// <summary>Updates a scope-administration group.</summary>
        Task<VersionedRecord<IdentityScopeAdministrationGroup>> UpdateGroupAsync(
            Guid identityScopeId, ApplicationKey application, Guid groupId, string displayName,
            GroupStatus status, long expectedVersion, CancellationToken cancellationToken);

        /// <summary>Lists group members.</summary>
        Task<IReadOnlyList<IdentityScopeAdministrationGroupMembership>> ListMembersAsync(
            Guid identityScopeId, ApplicationKey application, Guid groupId, CancellationToken cancellationToken);

        /// <summary>Adds an active user to an active administration group.</summary>
        Task<IdentityScopeAdministrationGroupMembership?> AddMemberAsync(
            Guid identityScopeId, ApplicationKey application, Guid groupId, Guid userId,
            CancellationToken cancellationToken);

        /// <summary>Removes a group member.</summary>
        Task<bool> RemoveMemberAsync(
            Guid identityScopeId, ApplicationKey application, Guid groupId, Guid userId,
            CancellationToken cancellationToken);

        /// <summary>Gets a scope-administration policy.</summary>
        Task<VersionedRecord<IdentityScopeAdministrationPolicy>?> GetPolicyAsync(
            Guid identityScopeId, ApplicationKey application, Guid policyId, CancellationToken cancellationToken);

        /// <summary>Creates a scope-administration policy.</summary>
        Task<VersionedRecord<IdentityScopeAdministrationPolicy>> CreatePolicyAsync(
            Guid identityScopeId, ApplicationKey application, Guid policyId, string displayName,
            PolicyStatus status, CancellationToken cancellationToken);

        /// <summary>Updates a scope-administration policy.</summary>
        Task<VersionedRecord<IdentityScopeAdministrationPolicy>> UpdatePolicyAsync(
            Guid identityScopeId, ApplicationKey application, Guid policyId, string displayName,
            PolicyStatus status, long expectedVersion, CancellationToken cancellationToken);

        /// <summary>Lists policy statements.</summary>
        Task<IReadOnlyList<IdentityScopeAdministrationPolicyStatement>> ListStatementsAsync(
            Guid identityScopeId, ApplicationKey application, Guid policyId, CancellationToken cancellationToken);

        /// <summary>Adds a policy statement.</summary>
        Task<IdentityScopeAdministrationPolicyStatement> AddStatementAsync(
            Guid identityScopeId, ApplicationKey application, Guid policyId, Guid statementId,
            int modelVersion, CapabilityPattern pattern, CancellationToken cancellationToken);

        /// <summary>Removes a policy statement.</summary>
        Task<bool> RemoveStatementAsync(
            Guid identityScopeId, ApplicationKey application, Guid policyId, Guid statementId,
            CancellationToken cancellationToken);

        /// <summary>Lists group-policy bindings.</summary>
        Task<IReadOnlyList<IdentityScopeAdministrationGroupPolicyBinding>> ListBindingsAsync(
            Guid identityScopeId, ApplicationKey application, Guid groupId, CancellationToken cancellationToken);

        /// <summary>Adds a group-policy binding when both sides are active.</summary>
        Task<IdentityScopeAdministrationGroupPolicyBinding?> AddBindingAsync(
            Guid identityScopeId, ApplicationKey application, Guid groupId, Guid policyId,
            CancellationToken cancellationToken);

        /// <summary>Removes a group-policy binding.</summary>
        Task<bool> RemoveBindingAsync(
            Guid identityScopeId, ApplicationKey application, Guid groupId, Guid policyId,
            CancellationToken cancellationToken);
    }
}
