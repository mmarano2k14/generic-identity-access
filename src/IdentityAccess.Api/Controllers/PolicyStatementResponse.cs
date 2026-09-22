using IdentityAccess.Application.Administration;
using IdentityAccess.Application.Storage;
using IdentityAccess.Domain;
using Microsoft.AspNetCore.Mvc;

namespace IdentityAccess.Api.Controllers
{

    /// <summary>Represents the response payload for policy statement.</summary>
    public sealed record PolicyStatementResponse(Guid StatementId, int ModelVersion, string Resource, string Feature,
        string Action)
    {
        /// <summary>Creates the response from the supplied domain or persistence record.</summary>
        public static PolicyStatementResponse From(PolicyStatement statement) =>
            new(statement.StatementId, statement.Model.Version, statement.Pattern.Resource,
                statement.Pattern.Feature, statement.Pattern.Action);
    }
}
