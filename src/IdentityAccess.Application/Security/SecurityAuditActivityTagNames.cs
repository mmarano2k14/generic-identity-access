namespace IdentityAccess.Application.Security
{
    /// <summary>
    /// Defines activity tag names used to propagate trusted request audit provenance into
    /// PostgreSQL session context. Values must contain identifiers only and never credentials.
    /// </summary>
    public static class SecurityAuditActivityTagNames
    {
        /// <summary>Gets the correlation identifier tag.</summary>
        public const string CorrelationId = "identity_access.audit.correlation_id";

        /// <summary>Gets the authenticated actor identity-scope tag.</summary>
        public const string ActorIdentityScopeId = "identity_access.audit.actor_identity_scope_id";

        /// <summary>Gets the authenticated actor user tag.</summary>
        public const string ActorUserId = "identity_access.audit.actor_user_id";

        /// <summary>Gets the authenticated actor session tag.</summary>
        public const string ActorSessionId = "identity_access.audit.actor_session_id";

        /// <summary>Gets the authenticated client tag.</summary>
        public const string ActorClientId = "identity_access.audit.actor_client_id";

        /// <summary>Gets the authenticated application tag.</summary>
        public const string ActorApplicationKey = "identity_access.audit.actor_application_key";

        /// <summary>Gets the authentication-context tag.</summary>
        public const string AuthenticationContextKey = "identity_access.audit.authentication_context_key";
    }
}
