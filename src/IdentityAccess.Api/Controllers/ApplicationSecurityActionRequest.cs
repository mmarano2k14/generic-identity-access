namespace IdentityAccess.Api.Controllers
{
    /// <summary>Represents one application security-manifest action.</summary>
    public sealed record ApplicationSecurityActionRequest(string Name, string DisplayName);
}
