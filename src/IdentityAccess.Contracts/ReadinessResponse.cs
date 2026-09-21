namespace IdentityAccess.Contracts;

public sealed record ReadinessResponse(bool Ready, string Stage, IReadOnlyList<string> BlockingCapabilities);
