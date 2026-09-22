namespace IdentityAccess.Application.Authentication
{
    /// <summary>
    /// Contains one opaque refresh token for protocol delivery and its SHA-256 persistence hash.
    /// The raw value must never be persisted.
    /// </summary>
    /// <param name="Value">The raw opaque refresh token returned to the public client.</param>
    /// <param name="Hash">The SHA-256 token hash safe for durable persistence.</param>
    public sealed record IssuedOidcRefreshToken(
        string Value,
        byte[] Hash);
}
