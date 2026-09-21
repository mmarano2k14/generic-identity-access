namespace IdentityAccess.Contracts;

public sealed record ServiceInfoResponse(string Service, string ApiVersion, string ModuleVersion, string Stage, string StorageProvider, bool StorageConfigured, bool AuthenticationConfigured, bool AuthorizationConfigured);
