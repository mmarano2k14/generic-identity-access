using IdentityAccess.Domain;

namespace IdentityAccess.Rbac
{
    /// <summary>
    /// Neutral request contract owned by Identity Access. It contains no type from an external RBAC package.
    /// Granted TRNs are candidate grants; the adapter establishes the external execution context.
    /// </summary>
    public sealed record RbacAuthorizationRequest
    {
        /// <summary>Gets the canonical RBAC project.</summary>
        public string Project { get; }

        /// <summary>Gets the canonical RBAC namespace.</summary>
        public string Namespace { get; }

        /// <summary>Gets the requested concrete capability.</summary>
        public CapabilityKey Capability { get; }

        /// <summary>Gets the candidate granted TRNs.</summary>
        public IReadOnlyList<string> GrantedTrns { get; }

        /// <summary>Initializes a new instance of <see cref="RbacAuthorizationRequest"/>.</summary>
        public RbacAuthorizationRequest(
            string project,
            string @namespace,
            CapabilityKey capability,
            IEnumerable<string> grantedTrns)
        {
            ArgumentNullException.ThrowIfNull(capability);
            ArgumentNullException.ThrowIfNull(grantedTrns);

            Project = new RbacContextKey(project).Value;
            Namespace = new RbacContextKey(@namespace).Value;
            Capability = capability;
            GrantedTrns = grantedTrns.Where(x => !string.IsNullOrWhiteSpace(x)).ToArray();
        }
    }
}
