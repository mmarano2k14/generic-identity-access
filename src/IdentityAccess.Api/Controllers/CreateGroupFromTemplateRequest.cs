namespace IdentityAccess.Api.Controllers
{
    /// <summary>Creates a normal tenant group by cloning a reusable real group definition.</summary>
    public sealed record CreateGroupFromTemplateRequest(
        Guid SourceTenantId,
        Guid SourceGroupId,
        Guid GroupId,
        IReadOnlyList<CreateGroupFromTemplateResourceScopeMappingRequest>? ResourceScopeMappings = null);
}
