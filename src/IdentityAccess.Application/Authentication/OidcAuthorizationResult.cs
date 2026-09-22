namespace IdentityAccess.Application.Authentication
{
    /// <summary>Represents authorization-code issuance or a protocol-safe authorization failure.</summary>
    public sealed record OidcAuthorizationResult
    {
        /// <summary>Gets whether authorization succeeded.</summary>
        public bool Succeeded { get; }

        /// <summary>Gets the safe registered redirect URI, when one has been validated.</summary>
        public string? RedirectUri { get; }

        /// <summary>Gets the issued opaque authorization code.</summary>
        public string? Code { get; }

        /// <summary>Gets the caller state to echo.</summary>
        public string? State { get; }

        /// <summary>Gets the stable failure category.</summary>
        public OidcAuthorizationFailureCode? FailureCode { get; }

        /// <summary>Initializes a new authorization result.</summary>
        public OidcAuthorizationResult(
            bool succeeded,
            string? redirectUri = null,
            string? code = null,
            string? state = null,
            OidcAuthorizationFailureCode? failureCode = null)
        {
            Succeeded = succeeded;
            RedirectUri = redirectUri;
            Code = code;
            State = state;
            FailureCode = failureCode;
        }

        /// <summary>Creates a successful authorization-code result.</summary>
        public static OidcAuthorizationResult Success(
            string redirectUri,
            string code,
            string state) =>
            new(true, redirectUri, code, state);

        /// <summary>Creates a rejected authorization request.</summary>
        public static OidcAuthorizationResult Reject(
            OidcAuthorizationFailureCode failureCode,
            string? redirectUri = null,
            string? state = null) =>
            new(false, redirectUri, state: state, failureCode: failureCode);
    }
}
