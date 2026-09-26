using IdentityAccess.Application.Storage;
using IdentityAccess.Domain;

namespace IdentityAccess.Application.Administration
{
    /// <summary>Application-level administration for reusable managed policies.</summary>
    public interface IManagedPolicyAdministrationService
    {
        Task<IReadOnlyList<VersionedRecord<ManagedPolicy>>> ListPoliciesAsync(
            Guid identityScopeId,
            ApplicationKey application,
            string? search,
            int offset,
            int limit,
            CancellationToken cancellationToken);

        Task<VersionedRecord<ManagedPolicy>?> GetPolicyAsync(
            Guid identityScopeId,
            ApplicationKey application,
            Guid policyId,
            CancellationToken cancellationToken);

        Task<VersionedRecord<ManagedPolicy>> CreatePolicyAsync(
            Guid identityScopeId,
            ApplicationKey application,
            Guid policyId,
            ManagedPolicyKey policyKey,
            string displayName,
            PolicyStatus status,
            CancellationToken cancellationToken);

        Task<VersionedRecord<ManagedPolicy>?> UpdatePolicyAsync(
            Guid identityScopeId,
            ApplicationKey application,
            Guid policyId,
            ManagedPolicyKey policyKey,
            string displayName,
            PolicyStatus status,
            long expectedVersion,
            CancellationToken cancellationToken);

        Task<IReadOnlyList<ManagedPolicyVersion>> ListVersionsAsync(
            Guid identityScopeId,
            ApplicationKey application,
            Guid policyId,
            CancellationToken cancellationToken);

        Task<ManagedPolicyVersion?> GetVersionAsync(
            Guid identityScopeId,
            ApplicationKey application,
            Guid policyId,
            int policyVersion,
            CancellationToken cancellationToken);

        Task<ManagedPolicyVersion?> CreateVersionAsync(
            Guid identityScopeId,
            ApplicationKey application,
            Guid policyId,
            int policyVersion,
            int modelVersion,
            CancellationToken cancellationToken);

        Task<ManagedPolicyVersion?> PublishVersionAsync(
            Guid identityScopeId,
            ApplicationKey application,
            Guid policyId,
            int policyVersion,
            bool makeDefault,
            CancellationToken cancellationToken);

        Task<IReadOnlyList<ManagedPolicyStatement>> ListStatementsAsync(
            Guid identityScopeId,
            ApplicationKey application,
            Guid policyId,
            int policyVersion,
            CancellationToken cancellationToken);

        Task<ManagedPolicyStatement?> AddStatementAsync(
            Guid identityScopeId,
            ApplicationKey application,
            Guid policyId,
            int policyVersion,
            Guid statementId,
            CapabilityPattern pattern,
            CancellationToken cancellationToken);

        Task<bool> RemoveStatementAsync(
            Guid identityScopeId,
            ApplicationKey application,
            Guid policyId,
            int policyVersion,
            Guid statementId,
            CancellationToken cancellationToken);
    }
}
