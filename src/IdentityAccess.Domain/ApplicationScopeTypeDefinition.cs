

namespace IdentityAccess.Domain
{

    /// <summary>
    /// Application-owned declaration of one generic resource-scope type inside a versioned security model.
    /// The identity core does not assign business semantics to the key.
    /// </summary>
    public sealed class ApplicationScopeTypeDefinition
    {
        /// <summary>Gets the model.</summary>
        public ApplicationSecurityModelReference Model { get; }
        /// <summary>Gets the type.</summary>
        public ResourceScopeTypeKey Type { get; }
        /// <summary>Gets the display name.</summary>
        public string DisplayName { get; }
        /// <summary>Gets the parent type.</summary>
        public ResourceScopeTypeKey? ParentType { get; }
        /// <summary>Gets the can attach to tenant.</summary>
        public bool CanAttachToTenant { get; }

        /// <summary>Initializes a new instance of <see cref="ApplicationScopeTypeDefinition"/>.</summary>
        public ApplicationScopeTypeDefinition(
            ApplicationSecurityModelReference model,
            ResourceScopeTypeKey type,
            string displayName,
            ResourceScopeTypeKey? parentType,
            bool canAttachToTenant)
        {
            ArgumentNullException.ThrowIfNull(model);
            ArgumentNullException.ThrowIfNull(type);
            if (parentType == type)
                throw new ArgumentException("A resource scope type cannot be its own parent.", nameof(parentType));
            if (parentType is null && !canAttachToTenant)
                throw new ArgumentException("A root resource scope type must be attachable to the tenant.", nameof(canAttachToTenant));
            if (parentType is not null && canAttachToTenant)
                throw new ArgumentException("Only root resource scope types can attach directly to the tenant.", nameof(canAttachToTenant));

            Model = model;
            Type = type;
            DisplayName = ModelGuard.DisplayName(displayName, nameof(displayName));
            ParentType = parentType;
            CanAttachToTenant = canAttachToTenant;
        }
    }
}
