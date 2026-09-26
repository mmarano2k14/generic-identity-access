using Microsoft.AspNetCore.Mvc;

namespace IdentityAccess.Api.Http
{
    /// <summary>
    /// Creates standardized RFC 7807 problem responses for the Identity Access HTTP API.
    /// </summary>
    internal static class ApiProblems
    {
        /// <summary>Creates a standardized problem-details payload.</summary>
        public static ProblemDetails Details(int status, string title, string? detail = null) =>
            new()
            {
                Status = status,
                Title = title,
                Detail = detail
            };

        /// <summary>Creates a standardized object result for the supplied HTTP status.</summary>
        public static ObjectResult Result(int status, string title, string? detail = null) =>
            new(Details(status, title, detail))
            {
                StatusCode = status
            };

        /// <summary>Creates a bad-request response.</summary>
        public static ObjectResult BadRequest(string title, string? detail = null) =>
            Result(StatusCodes.Status400BadRequest, title, detail);

        /// <summary>Creates an unauthorized response.</summary>
        public static ObjectResult Unauthorized(string title, string? detail = null) =>
            Result(StatusCodes.Status401Unauthorized, title, detail);

        /// <summary>Creates a forbidden response.</summary>
        public static ObjectResult Forbidden(string title, string? detail = null) =>
            Result(StatusCodes.Status403Forbidden, title, detail);

        /// <summary>Creates a not-found response.</summary>
        public static ObjectResult NotFound(string title, string? detail = null) =>
            Result(StatusCodes.Status404NotFound, title, detail);

        /// <summary>Creates the directory-administration unavailable response.</summary>
        public static ObjectResult DirectoryAdministrationUnavailable() =>
            Result(
                StatusCodes.Status503ServiceUnavailable,
                "Directory administration unavailable",
                "Routing and persistence must be configured before directory administration can execute.");

        /// <summary>Creates the policy-administration unavailable response.</summary>
        public static ObjectResult PolicyAdministrationUnavailable() =>
            Result(
                StatusCodes.Status503ServiceUnavailable,
                "Policy administration unavailable",
                "Routing and PostgreSQL persistence must be configured before policy administration can execute.");

        /// <summary>Creates the application security-catalog administration unavailable response.</summary>
        public static ObjectResult SecurityCatalogAdministrationUnavailable() =>
            Result(
                StatusCodes.Status503ServiceUnavailable,
                "Application security catalog unavailable",
                "Routing and PostgreSQL persistence must be configured before application security manifests can be registered or queried.");

        /// <summary>Creates the managed-policy administration unavailable response.</summary>
        public static ObjectResult ManagedPolicyAdministrationUnavailable() =>
            Result(
                StatusCodes.Status503ServiceUnavailable,
                "Managed policy administration unavailable",
                "Routing and managed-policy persistence must be configured before the shared policy catalog can be administered.");

        /// <summary>Creates the managed-policy-binding administration unavailable response.</summary>
        public static ObjectResult ManagedPolicyBindingAdministrationUnavailable() =>
            Result(
                StatusCodes.Status503ServiceUnavailable,
                "Managed policy binding administration unavailable",
                "Routing, shared managed-policy persistence, and managed binding persistence must be configured before bindings can be administered.");

        /// <summary>Creates the resource-scope administration unavailable response.</summary>
        public static ObjectResult ResourceScopeAdministrationUnavailable() =>
            Result(
                StatusCodes.Status503ServiceUnavailable,
                "Resource scope administration unavailable",
                "Routing and PostgreSQL persistence must be configured before resource scopes can be administered.");

        /// <summary>Creates the identity-scope authority administration unavailable response.</summary>
        public static ObjectResult IdentityScopeAuthorityAdministrationUnavailable() =>
            Result(
                StatusCodes.Status503ServiceUnavailable,
                "Identity-scope authority administration unavailable",
                "Routing, PostgreSQL persistence, and scope-authority stores must be configured before authority administration can execute.");

        /// <summary>Creates the security-audit administration unavailable response.</summary>
        public static ObjectResult SecurityAuditUnavailable() =>
            Result(
                StatusCodes.Status503ServiceUnavailable,
                "Security audit unavailable",
                "Security audit persistence must be configured before audit records can be queried.");

        /// <summary>Creates the credential-administration unavailable response.</summary>
        public static ObjectResult CredentialAdministrationUnavailable() =>
            Result(
                StatusCodes.Status503ServiceUnavailable,
                "Authentication unavailable",
                "Credential administration is not configured on this host.");

        /// <summary>Creates the local-authentication unavailable response.</summary>
        public static ObjectResult AuthenticationUnavailable() =>
            Result(
                StatusCodes.Status503ServiceUnavailable,
                "Authentication unavailable",
                "Local authentication is not configured on this host.");

        /// <summary>Creates the MFA-administration unavailable response.</summary>
        public static ObjectResult MfaAdministrationUnavailable() =>
            Result(
                StatusCodes.Status503ServiceUnavailable,
                "MFA administration unavailable",
                "Generic MFA policy and authenticator stores are not configured on this host.");

        /// <summary>Creates the administration-authorization unavailable response.</summary>
        public static ObjectResult AdministrationAuthorizationUnavailable() =>
            Result(
                StatusCodes.Status503ServiceUnavailable,
                "Administration authorization unavailable",
                "Administrative operations remain fail-closed until a trusted administration authorizer is configured.");
    }
}
