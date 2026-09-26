namespace IdentityAccess.Api.Controllers
{
    /// <summary>Represents one application security-manifest feature and its actions.</summary>
    public sealed record ApplicationSecurityFeatureRequest(
        string Name,
        IReadOnlyList<ApplicationSecurityActionRequest> Actions);
}
