namespace OrganizationDirectory.Domain
{
    /// <summary>Organization lifecycle status.</summary>
    public enum OrganizationStatus
    {
        /// <summary>The organization can participate in normal operations.</summary>
        Active = 1,
        /// <summary>The organization is retained but unavailable for normal operations.</summary>
        Disabled = 2
    }
}
