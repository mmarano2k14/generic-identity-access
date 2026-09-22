using IdentityAccess.Application.Authentication;
using Microsoft.AspNetCore.WebUtilities;

namespace IdentityAccess.Api.Oidc
{
    /// <summary>Builds encoded OAuth authorization success and error redirects.</summary>
    internal static class OidcRedirectBuilder
    {
        /// <summary>Builds a successful authorization-code redirect.</summary>
        public static string Success(
            OidcAuthorizationResult result)
        {
            if (!result.Succeeded ||
                string.IsNullOrWhiteSpace(result.RedirectUri) ||
                string.IsNullOrWhiteSpace(result.Code) ||
                string.IsNullOrWhiteSpace(result.State))
            {
                throw new ArgumentException(
                    "A complete successful authorization result is required.",
                    nameof(result));
            }

            return QueryHelpers.AddQueryString(
                result.RedirectUri,
                new Dictionary<string, string?>
                {
                    ["code"] = result.Code,
                    ["state"] = result.State
                });
        }

        /// <summary>Builds a protocol-safe authorization error redirect.</summary>
        public static string Error(
            OidcAuthorizationResult result)
        {
            if (result.Succeeded ||
                string.IsNullOrWhiteSpace(result.RedirectUri) ||
                result.FailureCode is null)
            {
                throw new ArgumentException(
                    "A redirect-safe authorization failure is required.",
                    nameof(result));
            }

            var values =
                new Dictionary<string, string?>
                {
                    ["error"] = ErrorCode(result.FailureCode.Value)
                };

            if (!string.IsNullOrWhiteSpace(result.State))
            {
                values["state"] =
                    result.State;
            }

            return QueryHelpers.AddQueryString(
                result.RedirectUri,
                values);
        }

        private static string ErrorCode(
            OidcAuthorizationFailureCode failureCode) =>
            failureCode switch
            {
                OidcAuthorizationFailureCode.UnsupportedResponseType =>
                    "unsupported_response_type",

                OidcAuthorizationFailureCode.InvalidScope =>
                    "invalid_scope",

                OidcAuthorizationFailureCode.LoginRequired =>
                    "login_required",

                OidcAuthorizationFailureCode.DirectoryUnavailable =>
                    "temporarily_unavailable",

                _ =>
                    "invalid_request"
            };
    }
}
