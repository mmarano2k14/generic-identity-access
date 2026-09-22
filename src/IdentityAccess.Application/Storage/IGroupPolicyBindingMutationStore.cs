using IdentityAccess.Application.Routing;
using IdentityAccess.Domain;

namespace IdentityAccess.Application.Storage
{
    /// <summary>
    /// Performs atomic policy-binding mutations whose validity depends on current persisted
    /// group, policy, and optional resource-scope state.
    /// </summary>
    public interface IGroupPolicyBindingMutationStore
    {
        /// <summary>
        /// Atomically verifies all current binding targets and inserts the binding when they are
        /// active and structurally compatible.
        /// </summary>
        /// <returns>
        /// <see langword="true"/> when the binding was inserted; <see langword="false"/> when a
        /// required current target is missing or inactive.
        /// </returns>
        Task<bool> AddIfActiveAsync(
            ResolvedDatabaseRoute route,
            GroupPolicyBinding binding,
            CancellationToken cancellationToken);
    }
}
