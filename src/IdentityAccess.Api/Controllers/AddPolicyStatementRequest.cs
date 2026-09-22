using IdentityAccess.Application.Administration;
using IdentityAccess.Application.Storage;
using IdentityAccess.Domain;
using Microsoft.AspNetCore.Mvc;

namespace IdentityAccess.Api.Controllers
{

    /// <summary>Represents the request payload for add policy statement.</summary>
    public sealed record AddPolicyStatementRequest(Guid StatementId, int ModelVersion, string Resource, string Feature,
        string Action);
}
