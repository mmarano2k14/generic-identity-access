using IdentityAccess.Domain;

namespace IdentityAccess.Api.Controllers
{
    /// <summary>Represents summary metadata for one registered application security model.</summary>
    public sealed record ApplicationSecurityModelSummaryResponse(
        int SchemaVersion,
        string ApplicationKey,
        int ModelVersion,
        string RbacProject,
        IReadOnlyList<string> RbacNamespaces,
        string ManifestSha256,
        int CapabilityCount)
    {
        /// <summary>Creates the response from the registered model.</summary>
        public static ApplicationSecurityModelSummaryResponse From(RegisteredApplicationSecurityModel model) =>
            new(
                model.Manifest.SchemaVersion,
                model.Reference.Application.Value,
                model.Reference.Version,
                model.Manifest.RbacProject,
                model.Manifest.RbacNamespaces,
                model.ManifestSha256,
                model.Capabilities.Count);
    }
}
