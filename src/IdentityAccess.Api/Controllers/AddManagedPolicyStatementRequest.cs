namespace IdentityAccess.Api.Controllers
{
    /// <summary>Adds one capability statement to an unpublished managed-policy version.</summary>
    public sealed record AddManagedPolicyStatementRequest(
        Guid StatementId,
        string Resource,
        string Feature,
        string Action);
}
