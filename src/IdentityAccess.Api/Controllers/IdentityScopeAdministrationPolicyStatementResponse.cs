using IdentityAccess.Domain;

namespace IdentityAccess.Api.Controllers
{
    /// <summary>Represents an identity-scope administration policy statement.</summary>
    public sealed record IdentityScopeAdministrationPolicyStatementResponse(
        Guid StatementId,
        int ModelVersion,
        string Resource,
        string Feature,
        string Action)
    {
        /// <summary>Maps a policy statement.</summary>
        public static IdentityScopeAdministrationPolicyStatementResponse From(
            IdentityScopeAdministrationPolicyStatement statement) =>
            new(
                statement.StatementId,
                statement.Model.Version,
                statement.Pattern.Resource,
                statement.Pattern.Feature,
                statement.Pattern.Action);
    }
}
