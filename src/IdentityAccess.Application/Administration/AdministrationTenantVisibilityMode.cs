namespace IdentityAccess.Application.Administration
{
    /// <summary>Defines how tenant visibility is derived for one authenticated administrator.</summary>
    public enum AdministrationTenantVisibilityMode
    {
        /// <summary>Tenant visibility is limited to active memberships of the authenticated subject.</summary>
        MembershipLimited = 1,

        /// <summary>The subject has active identity-scope authority; individual operations remain RBAC-authorized.</summary>
        ScopeWide = 2
    }
}
