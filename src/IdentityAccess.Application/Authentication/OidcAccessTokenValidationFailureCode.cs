namespace IdentityAccess.Application.Authentication
{
    /// <summary>Defines stable internal failure categories for OIDC access-token validation.</summary>
    public enum OidcAccessTokenValidationFailureCode
    {
        /// <summary>The JWT wire representation or JSON structure is malformed.</summary>
        MalformedToken = 1,

        /// <summary>The JWT does not use the required RS256/JWT header contract.</summary>
        UnsupportedHeader = 2,

        /// <summary>The JWT references a signing-key identifier not present in the process-pinned key ring.</summary>
        UnknownSigningKey = 3,

        /// <summary>The JWT signature is not valid for the selected signing key.</summary>
        SignatureInvalid = 4,

        /// <summary>The issuer claim does not match the configured canonical issuer.</summary>
        IssuerMismatch = 5,

        /// <summary>The audience claim does not match the configured access-token audience.</summary>
        AudienceMismatch = 6,

        /// <summary>The access token is expired.</summary>
        Expired = 7,

        /// <summary>The access-token issuance or expiry timestamps violate the provider contract.</summary>
        LifetimeInvalid = 8,

        /// <summary>Required access-token claims are missing, malformed, duplicated, or inconsistent.</summary>
        ClaimsInvalid = 9,

        /// <summary>The token client is not a current registered OIDC client or no longer matches its application/scope binding.</summary>
        ClientBindingInvalid = 10
    }
}
