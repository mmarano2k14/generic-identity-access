using IdentityAccess.Rbac;

namespace IdentityAccess.Rbac.MultiplexedAdapter
{
    /// <summary>
    /// Describes whether the configured external RBAC distribution satisfies the adapter contract
    /// together with non-secret version and SHA-256 identity information for the pinned binaries.
    /// </summary>
    public sealed record MultiplexedRbacCompatibilityReport
    {
        /// <summary>Gets whether the external distribution is compatible.</summary>
        public bool IsCompatible { get; }

        /// <summary>Gets the typed technical failure when compatibility validation fails.</summary>
        public RbacAuthorizationFailureCode? FailureCode { get; }

        /// <summary>Gets a safe internal diagnostic category/detail.</summary>
        public string? DiagnosticDetail { get; }

        /// <summary>Gets the external core assembly version.</summary>
        public string? CoreAssemblyVersion { get; }

        /// <summary>Gets the external core assembly SHA-256 fingerprint.</summary>
        public string? CoreAssemblySha256 { get; }

        /// <summary>Gets the external abstractions assembly version.</summary>
        public string? AbstractionsAssemblyVersion { get; }

        /// <summary>Gets the external abstractions assembly SHA-256 fingerprint.</summary>
        public string? AbstractionsAssemblySha256 { get; }

        /// <summary>Initializes an external RBAC compatibility report.</summary>
        public MultiplexedRbacCompatibilityReport(
            bool isCompatible,
            RbacAuthorizationFailureCode? failureCode = null,
            string? diagnosticDetail = null,
            string? coreAssemblyVersion = null,
            string? coreAssemblySha256 = null,
            string? abstractionsAssemblyVersion = null,
            string? abstractionsAssemblySha256 = null)
        {
            IsCompatible = isCompatible;
            FailureCode = failureCode;
            DiagnosticDetail = diagnosticDetail;
            CoreAssemblyVersion = coreAssemblyVersion;
            CoreAssemblySha256 = coreAssemblySha256;
            AbstractionsAssemblyVersion = abstractionsAssemblyVersion;
            AbstractionsAssemblySha256 = abstractionsAssemblySha256;
        }
    }
}
