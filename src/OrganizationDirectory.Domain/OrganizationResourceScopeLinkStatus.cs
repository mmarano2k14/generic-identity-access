namespace OrganizationDirectory.Domain
{
    /// <summary>Organization-to-resource-scope link lifecycle status.</summary>
    public enum OrganizationResourceScopeLinkStatus
    {
        /// <summary>The external resource-scope mapping is active.</summary>
        Active = 1,
        /// <summary>The mapping is retained but inactive.</summary>
        Disabled = 2
    }
}
