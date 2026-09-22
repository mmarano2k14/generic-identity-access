using System.Collections;
using System.Reflection;
using IdentityAccess.Rbac;

namespace IdentityAccess.Rbac.MultiplexedAdapter
{
    /// <summary>
    /// Anti-corruption adapter around an external Multiplexed RBAC binary distribution. The
    /// external reflection contract is validated and process-pinned once per adapter instance.
    /// Wildcard evaluation remains delegated to the real external authorization engine.
    /// </summary>
    public sealed class MultiplexedRbacAuthorizationAdapter :
        IRbacAuthorizationAdapter,
        IMultiplexedRbacCompatibilityProbe
    {
        private readonly Lazy<MultiplexedRbacBindingResult> binding;

        /// <summary>Initializes a new external RBAC adapter.</summary>
        public MultiplexedRbacAuthorizationAdapter(
            MultiplexedRbacAdapterOptions options)
        {
            ArgumentNullException.ThrowIfNull(options);

            binding =
                new Lazy<MultiplexedRbacBindingResult>(
                    () =>
                        MultiplexedRbacBindingLoader.Load(
                            options),
                    System.Threading.LazyThreadSafetyMode.ExecutionAndPublication);
        }

        /// <inheritdoc />
        public MultiplexedRbacCompatibilityReport ProbeCompatibility() =>
            binding.Value.Report;

        /// <inheritdoc />
        public ValueTask<RbacAuthorizationResult> AuthorizeAsync(
            RbacAuthorizationRequest request,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);
            cancellationToken.ThrowIfCancellationRequested();

            var bindingResult =
                binding.Value;

            if (!bindingResult.Report.IsCompatible ||
                bindingResult.Binding is null)
            {
                return ValueTask.FromResult(
                    RbacAuthorizationResult.Failure(
                        bindingResult.Report.FailureCode ??
                            RbacAuthorizationFailureCode.ExternalContractMismatch,
                        bindingResult.Report.DiagnosticDetail));
            }

            try
            {
                var allowed = Evaluate(
                    bindingResult.Binding,
                    request,
                    cancellationToken);

                return ValueTask.FromResult(
                    allowed
                        ? RbacAuthorizationResult.Allow()
                        : RbacAuthorizationResult.Deny());
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (TargetInvocationException error)
                when (error.InnerException is OperationCanceledException canceled)
            {
                throw canceled;
            }
            catch (Exception error)
            {
                return ValueTask.FromResult(
                    RbacAuthorizationResult.Failure(
                        RbacAuthorizationFailureCode.ExternalInvocationFailed,
                        error.GetType().Name));
            }
        }

        private static bool Evaluate(
            MultiplexedRbacBinding binding,
            RbacAuthorizationRequest request,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var namespaceEntry =
                binding.NamespaceEntryConstructor.Invoke(
                    null);

            binding.NamespaceEntryNameProperty.SetValue(
                namespaceEntry,
                request.Namespace);

            binding.NamespaceEntryTrnsProperty.SetValue(
                namespaceEntry,
                CreateStringCollection(
                    binding,
                    FilterContextTrns(request)));

            var namespaceList =
                (IList)(
                    Activator.CreateInstance(
                        binding.NamespaceListType)
                    ?? throw new InvalidOperationException(
                        "Unable to create external namespace list."));

            namespaceList.Add(
                namespaceEntry);

            var context =
                binding.ContextConstructor.Invoke(
                    null);

            binding.ContextKeyProperty.SetValue(
                context,
                $"identity-access:{Guid.NewGuid():N}");

            binding.ProjectProperty.SetValue(
                context,
                request.Project);

            binding.UserIdProperty.SetValue(
                context,
                "identity-access-subject");

            binding.TenantIdProperty.SetValue(
                context,
                "identity-access-tenant");

            binding.TenantGroupIdProperty.SetValue(
                context,
                "identity-access-adapter");

            binding.CurrentNamespaceProperty.SetValue(
                context,
                request.Namespace);

            binding.NamespacesProperty.SetValue(
                context,
                namespaceList);

            binding.InFlightCountProperty.SetValue(
                context,
                1);

            binding.TtlSecondsProperty.SetValue(
                context,
                60);

            var accessor =
                binding.AccessorConstructor.Invoke(
                    null);

            var contextSet = false;

            try
            {
                binding.AccessorSetMethod.Invoke(
                    accessor,
                    [context]);

                contextSet = true;

                var scope =
                    binding.ScopeConstructor.Invoke(
                        null);

                var options =
                    binding.OptionsConstructor.Invoke(
                        null);

                binding.OptionsProjectProperty.SetValue(
                    options,
                    request.Project);

                var wrapper =
                    binding.OptionsWrapperConstructor.Invoke(
                        [options]);

                var builder =
                    binding.BuilderConstructor.Invoke(
                        [wrapper]);

                var engine =
                    binding.EngineConstructor.Invoke(
                        [
                            builder,
                            scope,
                            accessor
                        ]);

                cancellationToken.ThrowIfCancellationRequested();

                return (bool)(
                    binding.IsAllowedMethod.Invoke(
                        engine,
                        [
                            request.Capability.Resource,
                            request.Capability.Feature,
                            request.Capability.Action
                        ])
                    ?? false);
            }
            finally
            {
                if (contextSet)
                {
                    binding.AccessorClearMethod.Invoke(
                        accessor,
                        null);
                }
            }
        }

        private static IReadOnlyList<string> FilterContextTrns(
            RbacAuthorizationRequest request)
        {
            var grants =
                new HashSet<string>(
                    StringComparer.Ordinal);

            foreach (var raw in request.GrantedTrns)
            {
                var normalized =
                    raw.Trim()
                        .ToLowerInvariant();

                var parts =
                    normalized.Split(
                        ':',
                        StringSplitOptions.None);

                if (parts.Length != 6 ||
                    !parts[0].Equals(
                        "trn",
                        StringComparison.Ordinal) ||
                    !parts[1].Equals(
                        request.Project,
                        StringComparison.Ordinal) ||
                    !parts[2].Equals(
                        request.Namespace,
                        StringComparison.Ordinal))
                {
                    continue;
                }

                grants.Add(
                    normalized);
            }

            return grants.ToArray();
        }

        private static object CreateStringCollection(
            MultiplexedRbacBinding binding,
            IEnumerable<string> values)
        {
            var materialized =
                values.ToArray();

            if (binding.NamespaceTrnsAcceptsHashSet)
            {
                return new HashSet<string>(
                    materialized,
                    StringComparer.Ordinal);
            }

            if (binding.NamespaceTrnsAcceptsList)
            {
                return materialized.ToList();
            }

            if (binding.NamespaceTrnsEnumerableConstructor is not null)
            {
                return binding.NamespaceTrnsEnumerableConstructor.Invoke(
                    [materialized]);
            }

            throw new InvalidOperationException(
                "Validated external string collection contract became unavailable.");
        }
    }
}
