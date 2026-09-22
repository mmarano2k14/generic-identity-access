namespace IdentityAccess.Api.Security
{
    /// <summary>Stores the trusted administration context for the lifetime of one HTTP request.</summary>
    internal sealed record AdministrationRequestContextFeature(
        AdministrationRequestContext Context);
}
