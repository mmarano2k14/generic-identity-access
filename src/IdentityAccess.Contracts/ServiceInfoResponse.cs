namespace IdentityAccess.Contracts
{
    /// <summary>
    /// Describes the current service version and the server-side capabilities configured on the
    /// running host.
    /// </summary>
    public sealed record ServiceInfoResponse(
        string Service,
        string ApiVersion,
        string ModuleVersion,
        string Stage,
        string StorageProvider,
        bool DatabaseRoutingConfigured,
        bool StorageConfigured,
        bool AuthenticationConfigured,
        bool AuthorizationConfigured);
}
