namespace IdentityAccess.Api.Controllers
{
    /// <summary>Maps one source template resource scope to a concrete resource scope in the target tenant.</summary>
    public sealed record CreateGroupFromTemplateResourceScopeMappingRequest(
        Guid SourceResourceScopeId,
        Guid TargetResourceScopeId);
}
