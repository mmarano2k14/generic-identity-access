using IdentityAccess.Domain;

namespace IdentityAccess.Application.Administration
{
    /// <summary>Administration surface for versioned application-owned security manifests.</summary>
    public interface IApplicationSecurityCatalogAdministrationService
    {
        /// <summary>Lists registered application security-model versions.</summary>
        Task<IReadOnlyList<RegisteredApplicationSecurityModel>> ListAsync(
            Guid identityScopeId,
            ApplicationKey application,
            CancellationToken cancellationToken);

        /// <summary>Gets one registered application security-model version.</summary>
        Task<RegisteredApplicationSecurityModel?> GetAsync(
            Guid identityScopeId,
            ApplicationKey application,
            int modelVersion,
            CancellationToken cancellationToken);

        /// <summary>Registers one immutable application-owned manifest version.</summary>
        Task<RegisteredApplicationSecurityModel> RegisterAsync(
            Guid identityScopeId,
            ApplicationSecurityManifest manifest,
            CancellationToken cancellationToken);
    }
}
