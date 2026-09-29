using IdentityAccess.Application.Storage;

namespace IdentityAccess.Api.Controllers
{
    /// <summary>Describes one source resource scope that must be remapped before cloning a reusable group.</summary>
    public sealed record GroupTemplateResourceScopeRequirementResponse(
        Guid SourceResourceScopeId,
        int ModelVersion,
        string ScopeType,
        string DisplayName)
    {
        internal static GroupTemplateResourceScopeRequirementResponse From(GroupTemplateResourceScopeRequirement value) =>
            new(value.SourceResourceScopeId, value.ModelVersion, value.ScopeType, value.DisplayName);
    }
}
