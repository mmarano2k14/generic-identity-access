namespace IdentityAccess.Domain
{
    /// <summary>Managed-policy version pinned to one application security-model version.</summary>
    public sealed class ManagedPolicyVersion
    {
        /// <summary>Gets the managed policy version reference.</summary>
        public ManagedPolicyVersionReference Reference { get; }
        /// <summary>Gets the application security model used by this policy version.</summary>
        public ApplicationSecurityModelReference Model { get; }
        /// <summary>Gets the publication timestamp, or null while the version remains editable.</summary>
        public DateTimeOffset? PublishedAt { get; }
        /// <summary>Gets whether this immutable policy version has been published.</summary>
        public bool IsPublished => PublishedAt.HasValue;

        /// <summary>Initializes a new instance of <see cref="ManagedPolicyVersion"/>.</summary>
        public ManagedPolicyVersion(
            ManagedPolicyVersionReference reference,
            ApplicationSecurityModelReference model,
            DateTimeOffset? publishedAt = null)
        {
            ArgumentNullException.ThrowIfNull(reference);
            ArgumentNullException.ThrowIfNull(model);
            if (reference.Policy.IdentityScopeId != model.IdentityScopeId)
                throw new ArgumentException("Managed policy and security model must belong to the same identity scope.", nameof(model));
            if (reference.Policy.Application != model.Application)
                throw new ArgumentException("Managed policy and security model must belong to the same application.", nameof(model));
            if (publishedAt == default(DateTimeOffset))
                throw new ArgumentOutOfRangeException(nameof(publishedAt));
            Reference = reference;
            Model = model;
            PublishedAt = publishedAt;
        }
    }
}
