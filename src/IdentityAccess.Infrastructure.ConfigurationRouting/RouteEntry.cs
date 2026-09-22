using System.Collections.Frozen;
using IdentityAccess.Application.Routing;
using IdentityAccess.Domain;

namespace IdentityAccess.Infrastructure.ConfigurationRouting
{

    internal sealed record RouteEntry(string Destination, long Version, bool Active);
}
