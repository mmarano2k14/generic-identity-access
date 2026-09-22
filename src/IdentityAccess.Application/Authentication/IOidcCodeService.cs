namespace IdentityAccess.Application.Authentication
{
    /// <summary>Provides cryptographic authorization-code and PKCE S256 primitives.</summary>
    public interface IOidcCodeService
    {
        /// <summary>Issues a cryptographically random opaque authorization code.</summary>
        IssuedOidcAuthorizationCode Issue();

        /// <summary>Derives the SHA-256 storage hash for an opaque authorization code.</summary>
        byte[] Hash(string code);

        /// <summary>Computes the RFC 7636 S256 code challenge for a validated verifier.</summary>
        string ComputeS256Challenge(string codeVerifier);
    }
}
