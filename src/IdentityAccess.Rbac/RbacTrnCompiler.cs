using IdentityAccess.Domain;

namespace IdentityAccess.Rbac
{
    /// <summary>
    /// Materializes the external TRN wire format. It does not evaluate authorization or wildcard semantics.
    /// </summary>
    public sealed class RbacTrnCompiler
    {
        /// <summary>Compiles a concrete capability into its canonical TRN representation.</summary>
        public string Compile(string project, string @namespace, CapabilityKey capability)
        {
            ArgumentNullException.ThrowIfNull(capability);
            return Compile(project, @namespace, new CapabilityPattern(capability));
        }

        /// <summary>Compiles a capability pattern into its canonical TRN representation.</summary>
        public string Compile(string project, string @namespace, CapabilityPattern pattern)
        {
            ArgumentNullException.ThrowIfNull(pattern);

            var projectKey = new RbacContextKey(project);
            var namespaceKey = new RbacContextKey(@namespace);

            return $"trn:{projectKey.Value}:{namespaceKey.Value}:{pattern.Resource}:{pattern.Feature}:{pattern.Action}";
        }
    }
}
