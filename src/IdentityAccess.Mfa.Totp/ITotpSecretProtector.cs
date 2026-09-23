namespace IdentityAccess.Mfa.Totp
{
    /// <summary>Protects provider-owned TOTP secret material before durable persistence.</summary>
    internal interface ITotpSecretProtector
    {
        byte[] Protect(ReadOnlySpan<byte> secret);
        byte[] Unprotect(byte[] protectedSecret);
    }
}
