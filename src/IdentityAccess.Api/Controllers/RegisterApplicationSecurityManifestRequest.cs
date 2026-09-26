namespace IdentityAccess.Api.Controllers
{
    /// <summary>
    /// Represents the project-owned JSON manifest registered as one immutable application security-model version.
    /// </summary>
    public sealed record RegisterApplicationSecurityManifestRequest(
        int SchemaVersion,
        string ApplicationKey,
        int ModelVersion,
        ApplicationSecurityRbacRequest Rbac,
        IReadOnlyList<ApplicationSecurityResourceRequest> Resources);
}
