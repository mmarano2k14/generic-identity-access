namespace IdentityAccess.Application.Security
{
    /// <summary>Defines security-relevant events recorded by the audit subsystem.</summary>
    public enum SecurityAuditEventType
    {
        /// <summary>A user was created.</summary>
        UserCreated = 1,
        /// <summary>A user was updated.</summary>
        UserUpdated = 2,
        /// <summary>A tenant was created.</summary>
        TenantCreated = 3,
        /// <summary>A tenant was updated.</summary>
        TenantUpdated = 4,
        /// <summary>A tenant membership was created.</summary>
        TenantMembershipCreated = 5,
        /// <summary>A tenant membership was updated.</summary>
        TenantMembershipUpdated = 6,
        /// <summary>A group was created.</summary>
        GroupCreated = 7,
        /// <summary>A group was updated.</summary>
        GroupUpdated = 8,
        /// <summary>A group membership was added.</summary>
        GroupMemberAdded = 9,
        /// <summary>A group membership was removed.</summary>
        GroupMemberRemoved = 10,
        /// <summary>A permission policy was created.</summary>
        PolicyCreated = 11,
        /// <summary>A permission policy was updated.</summary>
        PolicyUpdated = 12,
        /// <summary>A policy statement was added.</summary>
        PolicyStatementAdded = 13,
        /// <summary>A policy statement was removed.</summary>
        PolicyStatementRemoved = 14,
        /// <summary>A policy binding was added.</summary>
        PolicyBindingAdded = 15,
        /// <summary>A policy binding was removed.</summary>
        PolicyBindingRemoved = 16,
        /// <summary>An application scope type was added.</summary>
        ScopeTypeAdded = 17,
        /// <summary>A resource scope was created.</summary>
        ResourceScopeCreated = 18,
        /// <summary>A resource scope was updated.</summary>
        ResourceScopeUpdated = 19,
        /// <summary>A password credential was created.</summary>
        PasswordCredentialCreated = 20,
        /// <summary>A password was changed.</summary>
        PasswordChanged = 21,
        /// <summary>A local password login succeeded.</summary>
        PasswordLoginSucceeded = 22,
        /// <summary>A local password login failed after directory resolution.</summary>
        PasswordLoginFailed = 23,
        /// <summary>A local authentication session was revoked.</summary>
        SessionRevoked = 24,
        /// <summary>A local authentication session revocation failed after directory resolution.</summary>
        SessionRevocationFailed = 25,
        /// <summary>All active sessions for a user were administratively revoked.</summary>
        UserSessionsRevoked = 26,
        /// <summary>All active sessions for a registered client were administratively revoked.</summary>
        ClientSessionsRevoked = 27,
        /// <summary>An identity-scope authority group was created.</summary>
        IdentityScopeAuthorityGroupCreated = 28,
        /// <summary>An identity-scope authority group was updated.</summary>
        IdentityScopeAuthorityGroupUpdated = 29,
        /// <summary>A user was added to an identity-scope authority group.</summary>
        IdentityScopeAuthorityMemberAdded = 30,
        /// <summary>A user was removed from an identity-scope authority group.</summary>
        IdentityScopeAuthorityMemberRemoved = 31,
        /// <summary>An identity-scope authority policy was created.</summary>
        IdentityScopeAuthorityPolicyCreated = 32,
        /// <summary>An identity-scope authority policy was updated.</summary>
        IdentityScopeAuthorityPolicyUpdated = 33,
        /// <summary>An identity-scope authority policy statement was added.</summary>
        IdentityScopeAuthorityStatementAdded = 34,
        /// <summary>An identity-scope authority policy statement was removed.</summary>
        IdentityScopeAuthorityStatementRemoved = 35,
        /// <summary>An identity-scope authority group-policy binding was added.</summary>
        IdentityScopeAuthorityBindingAdded = 36,
        /// <summary>An identity-scope authority group-policy binding was removed.</summary>
        IdentityScopeAuthorityBindingRemoved = 37,
        /// <summary>An OpenID Connect authorization code was issued.</summary>
        OidcAuthorizationCodeIssued = 38,
        /// <summary>An OpenID Connect authorization code was redeemed successfully.</summary>
        OidcAuthorizationCodeRedeemed = 39,
        /// <summary>An OAuth refresh-token family was created.</summary>
        OidcRefreshTokenFamilyCreated = 40,
        /// <summary>An OAuth refresh token was rotated successfully.</summary>
        OidcRefreshTokenRotated = 41,
        /// <summary>A consumed OAuth refresh token was replayed.</summary>
        OidcRefreshTokenReuseDetected = 42,
        /// <summary>An OAuth refresh-token family was revoked after replay detection.</summary>
        OidcRefreshTokenFamilyRevoked = 43,
        /// <summary>An MFA policy was created.</summary>
        MfaPolicyCreated = 44,
        /// <summary>An MFA policy was updated.</summary>
        MfaPolicyUpdated = 45,
        /// <summary>A user authenticator was revoked.</summary>
        UserAuthenticatorRevoked = 46
    }
}
