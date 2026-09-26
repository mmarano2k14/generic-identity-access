namespace IdentityAccess.Api.Controllers
{
    /// <summary>Represents the RBAC execution-context declaration in an application security manifest.</summary>
    public sealed record ApplicationSecurityRbacRequest(string Project, IReadOnlyList<string> Namespaces);
}
