using IdentityAccess.Contracts;

namespace IdentityAccess.Application;

/// <summary>Routing activation does not imply live storage or finished authentication and authorization.</summary>
public sealed class FoundationStatus(bool databaseRoutingConfigured = false)
{
    public ServiceInfoResponse Describe() => new(
        "identity-access", "v1", "0.2.0", "foundation", "postgresql",
        StorageConfigured: false, AuthenticationConfigured: false, AuthorizationConfigured: false);

    public ReadinessResponse Readiness()
    {
        var blockers = new List<string>();
        if (!databaseRoutingConfigured) blockers.Add("database-routing");
        blockers.AddRange(["postgresql-persistence", "authentication", "rbac-integration"]);
        return new ReadinessResponse(false, "foundation", blockers.AsReadOnly());
    }
}
