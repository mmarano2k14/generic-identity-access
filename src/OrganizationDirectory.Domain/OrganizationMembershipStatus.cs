namespace OrganizationDirectory.Domain
{
    /// <summary>Explicit organization-membership lifecycle status.</summary>
    public enum OrganizationMembershipStatus
    {
        /// <summary>The tenant member belongs to the organization.</summary>
        Active = 1,
        /// <summary>The organizational relationship is retained but inactive.</summary>
        Suspended = 2
    }
}
