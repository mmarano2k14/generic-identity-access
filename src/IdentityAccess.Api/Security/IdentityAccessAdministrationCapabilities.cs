namespace IdentityAccess.Api.Security
{
    /// <summary>
    /// Defines the stable capability segments used by the Identity Access administration API.
    /// </summary>
    /// <remarks>
    /// These constants describe authorization metadata only. They do not grant a permission,
    /// authenticate a caller, or evaluate a wildcard.
    /// </remarks>
    public static class IdentityAccessAdministrationCapabilities
    {
        /// <summary>Gets the administration capability resource.</summary>
        public const string Resource = "identity-access";

        /// <summary>Gets the user administration feature.</summary>
        public const string Users = "user";

        /// <summary>Gets the tenant administration feature.</summary>
        public const string Tenants = "tenant";

        /// <summary>Gets the tenant-membership administration feature.</summary>
        public const string TenantMemberships = "tenant-membership";

        /// <summary>Gets the group administration feature.</summary>
        public const string Groups = "group";

        /// <summary>Gets the group-membership administration feature.</summary>
        public const string GroupMemberships = "group-membership";

        /// <summary>Gets the credential administration feature.</summary>
        public const string Credentials = "credential";

        /// <summary>Gets the policy administration feature.</summary>
        public const string Policies = "policy";

        /// <summary>Gets the application security-model administration feature.</summary>
        public const string SecurityModels = "security-model";

        /// <summary>Gets the policy-statement administration feature.</summary>
        public const string PolicyStatements = "policy-statement";

        /// <summary>Gets the policy-binding administration feature.</summary>
        public const string PolicyBindings = "policy-binding";

        /// <summary>Gets the scope-type administration feature.</summary>
        public const string ScopeTypes = "scope-type";

        /// <summary>Gets the resource-scope administration feature.</summary>
        public const string ResourceScopes = "resource-scope";

        /// <summary>Gets the session administration feature.</summary>
        public const string Sessions = "session";

        /// <summary>Gets the read-only security audit administration feature.</summary>
        public const string SecurityAudit = "security-audit";

        /// <summary>Gets the generic MFA policy administration feature.</summary>
        public const string MfaPolicies = "mfa-policy";

        /// <summary>Gets the generic user-authenticator administration feature.</summary>
        public const string MfaAuthenticators = "mfa-authenticator";

        /// <summary>Gets the identity-scope authority-group administration feature.</summary>
        public const string IdentityScopeAuthorityGroups = "scope-authority-group";

        /// <summary>Gets the identity-scope authority-membership administration feature.</summary>
        public const string IdentityScopeAuthorityMemberships = "scope-authority-membership";

        /// <summary>Gets the identity-scope authority-policy administration feature.</summary>
        public const string IdentityScopeAuthorityPolicies = "scope-authority-policy";

        /// <summary>Gets the identity-scope authority-statement administration feature.</summary>
        public const string IdentityScopeAuthorityStatements = "scope-authority-statement";

        /// <summary>Gets the identity-scope authority-binding administration feature.</summary>
        public const string IdentityScopeAuthorityBindings = "scope-authority-binding";

        /// <summary>Gets the read action.</summary>
        public const string Read = "read";

        /// <summary>Gets the write action.</summary>
        public const string Write = "write";
    }
}
