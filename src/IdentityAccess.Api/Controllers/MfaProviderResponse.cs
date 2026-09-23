using IdentityAccess.Application.Authentication.Mfa;

namespace IdentityAccess.Api.Controllers
{
    /// <summary>Public provider metadata returned by the generic MFA administration API.</summary>
    public sealed record MfaProviderResponse(
        string Key,
        string DisplayName,
        IReadOnlyList<string> Capabilities)
    {
        /// <summary>Creates an HTTP response from provider metadata.</summary>
        public static MfaProviderResponse From(AuthenticationFactorProviderDescriptor descriptor)
        {
            ArgumentNullException.ThrowIfNull(descriptor);
            var capabilities = new List<string>();
            if (descriptor.Capabilities.HasFlag(AuthenticationFactorProviderCapabilities.Enrollment))
                capabilities.Add("enrollment");
            if (descriptor.Capabilities.HasFlag(AuthenticationFactorProviderCapabilities.Verification))
                capabilities.Add("verification");
            if (descriptor.Capabilities.HasFlag(AuthenticationFactorProviderCapabilities.Recovery))
                capabilities.Add("recovery");
            return new MfaProviderResponse(descriptor.Key.Value, descriptor.DisplayName, capabilities);
        }
    }
}
