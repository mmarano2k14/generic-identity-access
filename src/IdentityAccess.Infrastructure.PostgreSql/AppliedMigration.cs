namespace IdentityAccess.Infrastructure.PostgreSql
{
    /// <summary>
    /// Represents persisted migration metadata used to verify append-only migration integrity.
    /// </summary>
    internal sealed record AppliedMigration(
        int Version,
        string Name,
        string? Checksum);
}
