namespace IdentityAccess.Application.Storage
{
    /// <summary>Describes one source resource scope that must be explicitly remapped when cloning a reusable group.</summary>
    public sealed record GroupTemplateResourceScopeRequirement(
        Guid SourceResourceScopeId,
        int ModelVersion,
        string ScopeType,
        string DisplayName);
}
