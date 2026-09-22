namespace IdentityAccess.Api.Controllers
{
    /// <summary>Represents one concrete capability evaluation request.</summary>
    public sealed record AuthorizationEvaluationRequest(
        string? Resource,
        string? Feature,
        string? Action);
}
