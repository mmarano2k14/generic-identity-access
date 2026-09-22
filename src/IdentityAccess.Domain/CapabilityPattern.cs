namespace IdentityAccess.Domain
{
    /// <summary>
    /// Persistable RBAC grant pattern. Wildcards are whole-segment only and are restricted to
    /// the six forms validated against the external RBAC engine:
    /// r:f:a, r:f:*, r:*:a, r:*:*, *:*:a, *:*:*.
    /// This type validates representation only; it never evaluates authorization.
    /// </summary>
    public sealed record CapabilityPattern
    {
        private const string InvalidSegmentMessage =
            "Capability pattern segments use '*' or 1 to 64 lowercase letters, digits or hyphens, starting with a letter.";

        /// <summary>Gets the resource.</summary>
        public string Resource { get; }

        /// <summary>Gets the feature.</summary>
        public string Feature { get; }

        /// <summary>Gets the action.</summary>
        public string Action { get; }

        /// <summary>Gets a value indicating whether the pattern contains no wildcard segments.</summary>
        public bool IsConcrete => Resource != "*" && Feature != "*" && Action != "*";

        /// <summary>Initializes a new instance of <see cref="CapabilityPattern"/>.</summary>
        public CapabilityPattern(string resource, string feature, string action)
        {
            Resource = Segment(resource, nameof(resource));
            Feature = Segment(feature, nameof(feature));
            Action = Segment(action, nameof(action));

            if (!IsSupportedShape(Resource, Feature, Action))
            {
                throw new ArgumentException("Unsupported capability wildcard shape.");
            }
        }

        /// <summary>Initializes a new instance of <see cref="CapabilityPattern"/> from a concrete capability.</summary>
        public CapabilityPattern(CapabilityKey capability)
            : this(
                capability?.Resource ?? throw new ArgumentNullException(nameof(capability)),
                capability.Feature,
                capability.Action)
        {
        }

        /// <summary>Returns the equivalent concrete capability when the pattern contains no wildcard segments.</summary>
        public CapabilityKey ToConcreteCapability()
        {
            if (!IsConcrete)
            {
                throw new InvalidOperationException(
                    "A wildcard capability pattern cannot be converted to a concrete capability key.");
            }

            return new CapabilityKey(Resource, Feature, Action);
        }

        /// <summary>Determines whether the pattern uses one of the supported concrete or wildcard shapes.</summary>
        public static bool IsSupportedShape(string resource, string feature, string action) =>
            resource != "*" || feature == "*";

        private static string Segment(string value, string parameterName) =>
            KeySyntax.NormalizeLowercaseSlugOrWildcard(
                value,
                parameterName,
                maximumLength: 64,
                InvalidSegmentMessage);
    }
}
