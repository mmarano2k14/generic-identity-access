

namespace IdentityAccess.Domain
{

    /// <summary>
    /// Generic application resource hierarchy node. Examples are supplied by consuming applications;
    /// the identity core itself does not define organization, business, department or project semantics.
    /// </summary>
    public sealed class ResourceScope
    {
        /// <summary>Gets the reference.</summary>
        public ResourceScopeReference Reference { get; }
        /// <summary>Gets the model version.</summary>
        public int ModelVersion { get; }
        /// <summary>Gets the type.</summary>
        public ResourceScopeTypeKey Type { get; }
        /// <summary>Gets the external resource identifier.</summary>
        public string ExternalResourceId { get; }
        /// <summary>Gets the display name.</summary>
        public string DisplayName { get; }
        /// <summary>Gets the parent resource scope identifier.</summary>
        public Guid? ParentResourceScopeId { get; }
        /// <summary>Gets the status.</summary>
        public ResourceScopeStatus Status { get; }

        /// <summary>Initializes a new instance of <see cref="ResourceScope"/>.</summary>
        public ResourceScope(
            ResourceScopeReference reference,
            int modelVersion,
            ResourceScopeTypeKey type,
            string externalResourceId,
            string displayName,
            Guid? parentResourceScopeId,
            ResourceScopeStatus status = ResourceScopeStatus.Active)
        {
            ArgumentNullException.ThrowIfNull(reference);
            ArgumentNullException.ThrowIfNull(type);
            if (modelVersion <= 0) throw new ArgumentOutOfRangeException(nameof(modelVersion));
            ArgumentException.ThrowIfNullOrWhiteSpace(externalResourceId);
            var normalizedExternalId = externalResourceId.Trim();
            if (normalizedExternalId.Length > 200 || normalizedExternalId.Any(char.IsControl))
                throw new ArgumentException("External resource ids must contain 1 to 200 printable characters.", nameof(externalResourceId));
            if (parentResourceScopeId == Guid.Empty)
                throw new ArgumentException("A parent resource scope id cannot be empty.", nameof(parentResourceScopeId));
            if (parentResourceScopeId == reference.ResourceScopeId)
                throw new ArgumentException("A resource scope cannot be its own parent.", nameof(parentResourceScopeId));

            Reference = reference;
            ModelVersion = modelVersion;
            Type = type;
            ExternalResourceId = normalizedExternalId;
            DisplayName = ModelGuard.DisplayName(displayName, nameof(displayName));
            ParentResourceScopeId = parentResourceScopeId;
            Status = ModelGuard.DefinedEnum(status, nameof(status));
        }
    }
}
