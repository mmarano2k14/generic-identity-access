using IdentityAccess.Domain;

namespace IdentityAccess.Api.Controllers
{
    /// <summary>Represents one concrete capability in a registered application security model.</summary>
    public sealed record ApplicationSecurityCapabilityResponse(
        string Resource,
        string Feature,
        string Action,
        string DisplayName)
    {
        /// <summary>Creates the response from the registered capability.</summary>
        public static ApplicationSecurityCapabilityResponse From(ApplicationCapability capability) =>
            new(capability.Key.Resource, capability.Key.Feature, capability.Key.Action, capability.DisplayName);
    }
}
