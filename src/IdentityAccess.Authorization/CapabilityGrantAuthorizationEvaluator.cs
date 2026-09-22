using IdentityAccess.Domain;
using IdentityAccess.Rbac;

namespace IdentityAccess.Authorization
{
    /// <summary>
    /// Materializes projected grant patterns and delegates wildcard-aware authorization to the
    /// external RBAC adapter.
    /// </summary>
    internal sealed class CapabilityGrantAuthorizationEvaluator(
        RbacTrnCompiler trnCompiler,
        IRbacAuthorizationAdapter rbacAdapter)
    {
        /// <summary>Evaluates one concrete capability against projected patterns.</summary>
        public async ValueTask<IdentityAuthorizationResult> EvaluateAsync(
            string rbacProject,
            string rbacNamespace,
            CapabilityKey capability,
            IEnumerable<CapabilityPattern> patterns,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(capability);
            ArgumentNullException.ThrowIfNull(patterns);

            string[] grantedTrns;

            try
            {
                grantedTrns = patterns
                    .Select(
                        pattern => trnCompiler.Compile(
                            rbacProject,
                            rbacNamespace,
                            pattern))
                    .Distinct(StringComparer.Ordinal)
                    .ToArray();
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch
            {
                return IdentityAuthorizationResult.Failure(
                    IdentityAuthorizationFailureCode.GrantMaterializationFailed);
            }

            RbacAuthorizationResult rbacResult;

            try
            {
                rbacResult = await rbacAdapter
                    .AuthorizeAsync(
                        new RbacAuthorizationRequest(
                            rbacProject,
                            rbacNamespace,
                            capability,
                            grantedTrns),
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
                    IdentityAuthorizationFailureCode.RbacAdapterInvocationFailed);
            }

            return rbacResult.Decision switch
            {
                RbacAuthorizationDecision.Allowed =>
                    IdentityAuthorizationResult.Allow(),

                RbacAuthorizationDecision.Denied =>
                    IdentityAuthorizationResult.Deny(),

                RbacAuthorizationDecision.TechnicalFailure =>
                    IdentityAuthorizationResult.Failure(
                        IdentityAuthorizationFailureCode.RbacTechnicalFailure,
                        rbacResult.FailureCode),

                _ =>
                    IdentityAuthorizationResult.Failure(
                        IdentityAuthorizationFailureCode.UnknownRbacDecision)
            };
        }
    }
}
