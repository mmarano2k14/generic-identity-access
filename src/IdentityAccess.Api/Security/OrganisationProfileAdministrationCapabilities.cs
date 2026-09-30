namespace IdentityAccess.Api.Security
{
    /// <summary>
    /// Defines the stable external administration capability segments for OrganisationProfile.
    /// </summary>
    /// <remarks>
    /// These constants are authorization metadata only. They do not grant permissions and do not
    /// evaluate RBAC policy.
    /// </remarks>
    public static class OrganisationProfileAdministrationCapabilities
    {
        /// <summary>Gets the established Identity Access administration authorization resource.</summary>
        public const string Resource = IdentityAccessAdministrationCapabilities.Resource;

        /// <summary>Gets the mutable profile-definition feature.</summary>
        public const string Profiles = "organisation-profile";

        /// <summary>Gets the reusable template-definition/version feature.</summary>
        public const string Templates = "organisation-profile-template";

        /// <summary>Gets the Organization-specific domain-override feature.</summary>
        public const string DomainOverrides = "organisation-profile-domain-override";

        /// <summary>Gets the immutable effective semantic-version feature.</summary>
        public const string EffectiveVersions = "organisation-profile-effective-version";

        /// <summary>Gets the read action.</summary>
        public const string Read = "read";

        /// <summary>Gets the write action.</summary>
        public const string Write = "write";
    }
}
