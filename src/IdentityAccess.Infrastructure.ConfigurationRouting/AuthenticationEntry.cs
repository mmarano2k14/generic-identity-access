using System.Collections.Frozen;
using IdentityAccess.Application.Routing;
using IdentityAccess.Domain;

namespace IdentityAccess.Infrastructure.ConfigurationRouting
{

    internal sealed record AuthenticationEntry(string Application, Guid Scope, bool Active);
}
