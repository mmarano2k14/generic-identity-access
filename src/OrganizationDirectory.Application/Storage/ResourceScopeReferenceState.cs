using OrganizationDirectory.Domain;

namespace OrganizationDirectory.Application.Storage
{
    /// <summary>Authoritative Identity Access ResourceScope metadata required for Organization linkage.</summary>
    public sealed record ResourceScopeReferenceState(
        ResourceScopeReference Reference,
        bool IsActive);
}
