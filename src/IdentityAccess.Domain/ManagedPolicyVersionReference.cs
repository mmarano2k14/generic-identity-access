namespace IdentityAccess.Domain
{
    /// <summary>Reference to one version of a reusable managed policy.</summary>
    public sealed record ManagedPolicyVersionReference
    {
        /// <summary>Gets the managed policy.</summary>
        public ManagedPolicyReference Policy { get; }
        /// <summary>Gets the positive policy version.</summary>
        public int Version { get; }

        /// <summary>Initializes a new instance of <see cref="ManagedPolicyVersionReference"/>.</summary>
        public ManagedPolicyVersionReference(ManagedPolicyReference policy, int version)
        {
            ArgumentNullException.ThrowIfNull(policy);
            if (version <= 0) throw new ArgumentOutOfRangeException(nameof(version));
            Policy = policy;
            Version = version;
        }
    }
}
