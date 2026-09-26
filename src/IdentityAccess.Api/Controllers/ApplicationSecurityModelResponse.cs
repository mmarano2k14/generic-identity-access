using IdentityAccess.Domain;

namespace IdentityAccess.Api.Controllers
{
    /// <summary>Represents one complete registered application security model.</summary>
    public sealed record ApplicationSecurityModelResponse(
        int SchemaVersion,
        string ApplicationKey,
        int ModelVersion,
        string RbacProject,
        IReadOnlyList<string> RbacNamespaces,
        string ManifestSha256,
        IReadOnlyList<ApplicationSecurityCapabilityResponse> Capabilities)
    {
        /// <summary>Creates the response from the registered model.</summary>
        public static ApplicationSecurityModelResponse From(RegisteredApplicationSecurityModel model) =>
            new(
                model.Manifest.SchemaVersion,
                model.Reference.Application.Value,
                model.Reference.Version,
                model.Manifest.RbacProject,
                model.Manifest.RbacNamespaces,
                model.ManifestSha256,
                model.Capabilities.Select(ApplicationSecurityCapabilityResponse.From).ToArray());
    }
}
