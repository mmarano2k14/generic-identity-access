using IdentityAccess.Api.Security;
using Microsoft.AspNetCore.Http;

namespace IdentityAccess.Tests.Api
{
    /// <summary>Provides a deterministic administration-authorization result for API tests.</summary>
    public sealed class FixedAdministrationRequestAuthorizer(
        AdministrationAccessResult result)
        : IAdministrationRequestAuthorizer
    {
        /// <inheritdoc />
        public bool CapabilityAuthorizationAvailable => true;

        /// <summary>Gets the last capability resource supplied by the controller.</summary>
        public string? Resource { get; private set; }

        /// <summary>Gets the last capability feature supplied by the controller.</summary>
        public string? Feature { get; private set; }

        /// <summary>Gets the last capability action supplied by the controller.</summary>
        public string? Action { get; private set; }

        /// <inheritdoc />
        public ValueTask<AdministrationAccessResult> AuthorizeAsync(
            HttpContext httpContext,
            string resource,
            string feature,
            string action,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(httpContext);
            cancellationToken.ThrowIfCancellationRequested();

            Resource = resource;
            Feature = feature;
            Action = action;

            return ValueTask.FromResult(result);
        }
    }
}
