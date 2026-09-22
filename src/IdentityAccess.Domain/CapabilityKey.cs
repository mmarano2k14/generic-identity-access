namespace IdentityAccess.Domain
{
    /// <summary>
    /// Concrete capability tuple compatible with the external RBAC contract: resource, feature, action.
    /// Project and authorization namespace belong to the RBAC execution context and are intentionally not part of this key.
    /// Wildcards are not valid in a concrete capability key.
    /// </summary>
    public sealed record CapabilityKey
    {
        private const string InvalidSegmentMessage =
            "Capability segments use 1 to 64 lowercase letters, digits or hyphens, starting with a letter.";

        /// <summary>Gets the resource.</summary>
        public string Resource { get; }

        /// <summary>Gets the feature.</summary>
        public string Feature { get; }

        /// <summary>Gets the action.</summary>
        public string Action { get; }

        /// <summary>Initializes a new instance of <see cref="CapabilityKey"/>.</summary>
        public CapabilityKey(string resource, string feature, string action)
        {
            Resource = Segment(resource, nameof(resource));
            Feature = Segment(feature, nameof(feature));
            Action = Segment(action, nameof(action));
        }

        private static string Segment(string value, string parameterName) =>
            KeySyntax.NormalizeLowercaseSlug(
                value,
                parameterName,
                maximumLength: 64,
                allowUnderscore: false,
                InvalidSegmentMessage);
    }
}
