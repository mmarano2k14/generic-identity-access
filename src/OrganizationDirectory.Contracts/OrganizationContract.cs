namespace OrganizationDirectory.Contracts
{
    /// <summary>Transport-neutral organization representation.</summary>
    public sealed record OrganizationContract(
        Guid IdentityScopeId,
        Guid TenantId,
        Guid OrganizationId,
        string OrganizationKey,
        string DisplayName,
        string OrganizationType,
        Guid? ParentOrganizationId,
        int Status,
        long RowVersion,
        DateTimeOffset CreatedAt,
        DateTimeOffset UpdatedAt);
}
