using System.Collections.Concurrent;
using System.Data.Common;
using IdentityAccess.Application.Routing;
using IdentityAccess.Application.Storage;
using Npgsql;

namespace IdentityAccess.Infrastructure.PostgreSql
{

    internal sealed record DataSourceRegistration(string SecretReference, NpgsqlDataSource DataSource);
}
