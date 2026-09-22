

namespace IdentityAccess.Domain
{

    /// <summary>An immutable application capability-model version within one identity scope.</summary>
    public sealed record ApplicationSecurityModelReference
    {
        /// <summary>Gets the identity scope identifier.</summary>
        public Guid IdentityScopeId { get; }
        /// <summary>Gets the application.</summary>
        public ApplicationKey Application { get; }
        /// <summary>Gets the version.</summary>
        public int Version { get; }

        /// <summary>Initializes a new instance of <see cref="ApplicationSecurityModelReference"/>.</summary>
        public ApplicationSecurityModelReference(Guid identityScopeId, ApplicationKey application, int version)
        {
            IdentityScopeId = ModelGuard.Identifier(identityScopeId, nameof(identityScopeId));
            ArgumentNullException.ThrowIfNull(application);
            if (version <= 0) throw new ArgumentOutOfRangeException(nameof(version));
            Application = application;
            Version = version;
        }
    }
}
