

namespace IdentityAccess.Application.Authentication
{

    /// <summary>Represents issued session token.</summary>
    public sealed record IssuedSessionToken(string Value, byte[] Hash);
}
