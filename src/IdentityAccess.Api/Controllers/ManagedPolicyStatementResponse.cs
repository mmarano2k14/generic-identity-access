using IdentityAccess.Domain;

namespace IdentityAccess.Api.Controllers
{
    /// <summary>One capability statement belonging to a managed-policy version.</summary>
    public sealed record ManagedPolicyStatementResponse(
        Guid StatementId,
        int PolicyVersion,
        int ModelVersion,
        string Resource,
        string Feature,
        string Action)
    {
        public static ManagedPolicyStatementResponse From(ManagedPolicyStatement statement) =>
            new(
                statement.StatementId,
                statement.PolicyVersion.Version,
                statement.Model.Version,
                statement.Pattern.Resource,
                statement.Pattern.Feature,
                statement.Pattern.Action);
    }
}
