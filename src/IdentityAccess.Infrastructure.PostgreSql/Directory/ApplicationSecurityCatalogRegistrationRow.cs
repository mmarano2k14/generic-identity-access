namespace IdentityAccess.Infrastructure.PostgreSql.Directory
{
    /// <summary>Internal projection of manifest registration metadata loaded with the capability catalog.</summary>
    internal sealed record ApplicationSecurityCatalogRegistrationRow(
        int SchemaVersion,
        string RbacProject,
        string ManifestSha256);
}
