using IdentityAccess.Application.Authorization;
using IdentityAccess.Application.Routing;
using IdentityAccess.Rbac;

namespace IdentityAccess.Authorization
{
    /// <summary>
    /// Resolves one immutable identity database route, projects current scope-administration
    /// grants, validates their provenance, and delegates the final decision to the external RBAC
    /// adapter.
    /// </summary>
    public sealed class IdentityScopeAuthorizationService(
        IDatabaseRouteResolver routeResolver,
        IIdentityScopeAssignedCapabilityReader assignedCapabilityReader,
        RbacTrnCompiler trnCompiler,
        IRbacAuthorizationAdapter rbacAdapter)
        : IIdentityScopeAuthorizationService
    {
        private readonly CapabilityGrantAuthorizationEvaluator evaluator =
            new(trnCompiler, rbacAdapter);

        /// <inheritdoc />
        public async ValueTask<IdentityAuthorizationResult> AuthorizeAsync(
            IdentityScopeAuthorizationRequest request,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);
            cancellationToken.ThrowIfCancellationRequested();

            ResolvedDatabaseRoute route;

            try
            {
                route = await routeResolver
                    .ResolveAsync(
                        new DatabaseRouteRequest(
                            request.Application,
                            request.IdentityScopeId),
                        cancellationToken)
                    .ConfigureAwait(false);
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

            IReadOnlyList<AssignedIdentityScopeCapabilityGrant> grants;

            try
            {
                grants = await assignedCapabilityReader
                    .ListAsync(
                        route,
                        request.IdentityScopeId,
                        request.Subject,
                        request.Application,
                        cancellationToken)
                    .ConfigureAwait(false);
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
                        grant.IdentityScopeId != request.IdentityScopeId ||
                        grant.Subject != request.Subject ||
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
