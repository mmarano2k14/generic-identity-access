namespace IdentityAccess.Api.Controllers
{
    /// <summary>Requests addition of one user to a scope-administration group.</summary>
    public sealed record AddIdentityScopeAdministrationMemberRequest(Guid UserId);
}
