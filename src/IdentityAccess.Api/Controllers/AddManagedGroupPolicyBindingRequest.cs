namespace IdentityAccess.Api.Controllers
{
    /// <summary>Request to bind a tenant group to one published shared managed-policy version.</summary>
    public sealed record AddManagedGroupPolicyBindingRequest(
        Guid PolicyId,
        int? PolicyVersion = null,
        Guid? ResourceScopeId = null,
        bool IncludeDescendants = false);
}
