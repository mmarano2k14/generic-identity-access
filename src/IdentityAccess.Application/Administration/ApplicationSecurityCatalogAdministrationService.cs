using IdentityAccess.Application.Routing;
using IdentityAccess.Application.Security;
using IdentityAccess.Application.Storage;
using IdentityAccess.Domain;

namespace IdentityAccess.Application.Administration
{
    /// <summary>Coordinates manifest-backed application security-catalog administration.</summary>
    public sealed class ApplicationSecurityCatalogAdministrationService(
        IDatabaseRouteResolver routeResolver,
        IApplicationSecurityCatalogStore catalogs,
        ApplicationSecurityManifestFingerprint fingerprint,
        ISecurityAuditWriter auditWriter) : IApplicationSecurityCatalogAdministrationService
    {
        /// <summary>Lists registered application security-model versions.</summary>
        public async Task<IReadOnlyList<RegisteredApplicationSecurityModel>> ListAsync(
            Guid identityScopeId,
            ApplicationKey application,
            CancellationToken cancellationToken)
        {
            var route = await ResolveAsync(identityScopeId, application, cancellationToken);
            return await catalogs.ListAsync(route, identityScopeId, application, cancellationToken);
        }

        /// <summary>Gets one registered application security-model version.</summary>
        public async Task<RegisteredApplicationSecurityModel?> GetAsync(
            Guid identityScopeId,
            ApplicationKey application,
            int modelVersion,
            CancellationToken cancellationToken)
        {
            var route = await ResolveAsync(identityScopeId, application, cancellationToken);
            return await catalogs.GetAsync(
                route,
                new ApplicationSecurityModelReference(identityScopeId, application, modelVersion),
                cancellationToken);
        }

        /// <summary>Registers one immutable application-owned manifest version.</summary>
        public async Task<RegisteredApplicationSecurityModel> RegisterAsync(
            Guid identityScopeId,
            ApplicationSecurityManifest manifest,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(manifest);
            var route = await ResolveAsync(identityScopeId, manifest.Application, cancellationToken);
            var registered = new RegisteredApplicationSecurityModel(
                identityScopeId,
                manifest,
                fingerprint.Compute(manifest));
            var stored = await catalogs.RegisterAsync(route, registered, cancellationToken);

            await auditWriter.TryWriteAsync(
                route,
                new SecurityAuditEvent(
                    SecurityAuditEventType.ApplicationSecurityModelRegistered,
                    SecurityAuditOutcome.Succeeded,
                    identityScopeId,
                    application: manifest.Application,
                    targetId: $"{manifest.Application.Value}:v{manifest.ModelVersion}"),
                cancellationToken);

            return stored;
        }

        private ValueTask<ResolvedDatabaseRoute> ResolveAsync(
            Guid identityScopeId,
            ApplicationKey application,
            CancellationToken cancellationToken) =>
            routeResolver.ResolveAsync(new DatabaseRouteRequest(application, identityScopeId), cancellationToken);
    }
}
