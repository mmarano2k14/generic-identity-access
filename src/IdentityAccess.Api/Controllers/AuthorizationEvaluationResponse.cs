namespace IdentityAccess.Api.Controllers
{
    /// <summary>Represents the explicit allow/deny result of a capability evaluation.</summary>
    public sealed record AuthorizationEvaluationResponse(bool Allowed);
}
