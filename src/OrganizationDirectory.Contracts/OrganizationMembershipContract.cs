namespace OrganizationDirectory.Contracts
{
    /// <summary>Transport-neutral organization-membership representation.</summary>
    public sealed record OrganizationMembershipContract(
        Guid IdentityScopeId,
        Guid TenantId,
        Guid OrganizationId,
        Guid TenantMembershipId,
        int Status,
        long RowVersion,
        DateTimeOffset CreatedAt,
        DateTimeOffset UpdatedAt);
}
