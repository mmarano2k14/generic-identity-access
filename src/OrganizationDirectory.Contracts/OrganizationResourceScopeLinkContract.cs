namespace OrganizationDirectory.Contracts
{
    /// <summary>Transport-neutral application-aware resource-scope linkage.</summary>
    public sealed record OrganizationResourceScopeLinkContract(
        Guid IdentityScopeId,
        Guid TenantId,
        Guid OrganizationId,
        string ApplicationKey,
        Guid ResourceScopeId,
        string ScopeType,
        int ModelVersion,
        int Status,
        long RowVersion,
        DateTimeOffset CreatedAt,
        DateTimeOffset UpdatedAt);
}
