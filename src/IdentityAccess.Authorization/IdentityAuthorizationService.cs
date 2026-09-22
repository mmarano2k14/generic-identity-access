using IdentityAccess.Application.Authorization;
using IdentityAccess.Application.Routing;
using IdentityAccess.Rbac;

namespace IdentityAccess.Authorization
{
    /// <summary>
    /// Stateless tenant/resource authorization orchestration. It resolves one immutable database
    /// route, projects current grants, validates their provenance, and delegates the final
    /// wildcard-aware decision to the configured RBAC adapter.
    /// </summary>
    public sealed class IdentityAuthorizationService(
        IDatabaseRouteResolver routeResolver,
        IAssignedCapabilityReader assignedCapabilityReader,
        RbacTrnCompiler trnCompiler,
        IRbacAuthorizationAdapter rbacAdapter)
        : IIdentityAuthorizationService
    {
        private readonly CapabilityGrantAuthorizationEvaluator evaluator =
            new(trnCompiler, rbacAdapter);

        /// <inheritdoc />
        public async ValueTask<IdentityAuthorizationResult> AuthorizeAsync(
            IdentityAuthorizationRequest request,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);
            cancellationToken.ThrowIfCancellationRequested();

            ResolvedDatabaseRoute route;

            try
            {
                route = await routeResolver.ResolveAsync(
                    new DatabaseRouteRequest(
                        request.Application,
                        request.Tenant.IdentityScopeId),
                    cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch
            {
                return IdentityAuthorizationResult.Failure(
                    IdentityAuthorizationFailureCode.RouteResolutionFailed);
            }

            IReadOnlyList<AssignedCapabilityGrant> grants;

            try
            {
                grants = await assignedCapabilityReader.ListAsync(
                    route,
                    request.Tenant,
                    request.Subject,
                    request.Application,
                    request.ResourceScope,
                    cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch
            {
                return IdentityAuthorizationResult.Failure(
                    IdentityAuthorizationFailureCode.GrantProjectionFailed);
            }

            if (grants.Any(
                    grant =>
                        grant.Subject != request.Subject ||
                        grant.Tenant != request.Tenant ||
                        grant.Application != request.Application))
            {
                return IdentityAuthorizationResult.Failure(
                    IdentityAuthorizationFailureCode.GrantProvenanceMismatch);
            }

            return await evaluator
                .EvaluateAsync(
                    request.RbacProject,
                    request.RbacNamespace,
                    request.Capability,
                    grants.Select(grant => grant.Pattern),
                    cancellationToken)
                .ConfigureAwait(false);
        }
    }
}
