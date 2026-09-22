using System.Collections.Frozen;
using IdentityAccess.Application.Routing;
using IdentityAccess.Domain;

namespace IdentityAccess.Infrastructure.ConfigurationRouting
{

    internal readonly record struct RouteKey(string Application, Guid Scope, IdentityDataSet DataSet);
}
