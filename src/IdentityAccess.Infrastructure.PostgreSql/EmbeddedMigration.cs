namespace IdentityAccess.Infrastructure.PostgreSql
{
    /// <summary>
    /// Represents one embedded append-only PostgreSQL migration and its canonical checksum.
    /// </summary>
    internal sealed record EmbeddedMigration(
        int Version,
        string Name,
        string Sql,
        string Checksum);
}
