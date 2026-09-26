using Npgsql;
using NpgsqlTypes;

namespace IdentityAccess.Infrastructure.PostgreSql.Directory
{
    /// <summary>Adds bounded administration-search parameters without changing storage authority.</summary>
    internal static class PostgreSqlAdministrationSearch
    {
        public static void AddParameters(NpgsqlCommand command, string? search)
        {
            ArgumentNullException.ThrowIfNull(command);

            var pattern = command.Parameters.Add("search_pattern", NpgsqlDbType.Text);
            pattern.Value = search is null ? DBNull.Value : $"{search.ToLowerInvariant()}%";

            var identifier = command.Parameters.Add("search_id", NpgsqlDbType.Uuid);
            identifier.Value = Guid.TryParse(search, out var parsed) ? parsed : DBNull.Value;
        }
    }
}
