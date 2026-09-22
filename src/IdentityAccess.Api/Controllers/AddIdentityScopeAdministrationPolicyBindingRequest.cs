namespace IdentityAccess.Api.Controllers
{
    /// <summary>Requests an identity-scope administration group-policy binding.</summary>
    public sealed record AddIdentityScopeAdministrationPolicyBindingRequest(Guid PolicyId);
}
