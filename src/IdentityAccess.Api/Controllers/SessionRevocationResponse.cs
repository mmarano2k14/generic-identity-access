namespace IdentityAccess.Api.Controllers
{
    /// <summary>Returns the number of active sessions revoked by an administration operation.</summary>
    public sealed record SessionRevocationResponse(int RevokedCount);
}
