namespace IdentityAccess.Application.Authentication
{
    /// <summary>Contains a newly issued opaque authorization code and its SHA-256 storage hash.</summary>
    /// <param name="Value">The raw opaque authorization code returned once to the client.</param>
    /// <param name="Hash">The SHA-256 authorization-code hash safe for persistence.</param>
    public sealed record IssuedOidcAuthorizationCode(
        string Value,
        byte[] Hash);
}
