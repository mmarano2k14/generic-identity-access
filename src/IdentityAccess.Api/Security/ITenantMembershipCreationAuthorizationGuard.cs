namespace IdentityAccess.Api.Security
{
    /// <summary>Restricts direct user-id tenant membership creation to identity-scope administrators.</summary>
    public interface ITenantMembershipCreationAuthorizationGuard
    {
        ValueTask<AdministrationAccessResult> AuthorizeDirectCreateAsync(
            AdministrationRequestContext context,
            CancellationToken cancellationToken);
    }
}
