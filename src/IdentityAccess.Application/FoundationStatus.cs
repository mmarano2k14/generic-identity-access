using IdentityAccess.Contracts;

namespace IdentityAccess.Application;

/// <summary>Explicit bootstrap status. No configured value can falsely mark unfinished security as ready.</summary>
public sealed class FoundationStatus
{
    public ServiceInfoResponse Describe() => new(
        "identity-access", "v1", "0.1.0", "foundation", "postgresql",
        StorageConfigured: false, AuthenticationConfigured: false, AuthorizationConfigured: false);

    public ReadinessResponse Readiness() => new(false, "foundation",
        Array.AsReadOnly(new[]
        {
            "database-routing",
            "postgresql-persistence",
            "authentication",
            "rbac-integration"
        }));
}
