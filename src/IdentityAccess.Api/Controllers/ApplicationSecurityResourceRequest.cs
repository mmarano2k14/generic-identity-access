namespace IdentityAccess.Api.Controllers
{
    /// <summary>Represents one application security-manifest resource and its features.</summary>
    public sealed record ApplicationSecurityResourceRequest(
        string Name,
        IReadOnlyList<ApplicationSecurityFeatureRequest> Features);
}
