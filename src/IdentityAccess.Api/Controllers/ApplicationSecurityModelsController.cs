using IdentityAccess.Api.Features;
using IdentityAccess.Api.Http;
using IdentityAccess.Api.Security;
using IdentityAccess.Application.Administration;
using IdentityAccess.Domain;
using Microsoft.AspNetCore.Mvc;

namespace IdentityAccess.Api.Controllers
{
    /// <summary>Exposes manifest-backed application security-model registration and discovery.</summary>
    [ApiController]
    [Route("api/v1/identity-scopes/{identityScopeId:guid}/applications/{applicationKey}/security-models")]
    [Produces("application/json")]
    public sealed class ApplicationSecurityModelsController(
        OptionalFeature<IApplicationSecurityCatalogAdministrationService> feature) : ControllerBase
    {
        /// <summary>Lists registered manifest-backed security-model versions.</summary>
        [HttpGet]
        [RequireAdministrationCapability(
            IdentityAccessAdministrationCapabilities.Resource,
            IdentityAccessAdministrationCapabilities.SecurityModels,
            IdentityAccessAdministrationCapabilities.Read)]
        public async Task<ActionResult<IReadOnlyList<ApplicationSecurityModelSummaryResponse>>> List(
            Guid identityScopeId,
            string applicationKey,
            CancellationToken cancellationToken)
        {
            if (!feature.TryGet(out var service)) return ApiProblems.SecurityCatalogAdministrationUnavailable();
            var application = new ApplicationKey(applicationKey);
            var models = await service.ListAsync(identityScopeId, application, cancellationToken);
            return Ok(models.Select(ApplicationSecurityModelSummaryResponse.From).ToArray());
        }

        /// <summary>Gets one complete manifest-backed security-model version.</summary>
        [HttpGet("{modelVersion:int}")]
        [RequireAdministrationCapability(
            IdentityAccessAdministrationCapabilities.Resource,
            IdentityAccessAdministrationCapabilities.SecurityModels,
            IdentityAccessAdministrationCapabilities.Read)]
        public async Task<ActionResult<ApplicationSecurityModelResponse>> Get(
            Guid identityScopeId,
            string applicationKey,
            int modelVersion,
            CancellationToken cancellationToken)
        {
            if (!feature.TryGet(out var service)) return ApiProblems.SecurityCatalogAdministrationUnavailable();
            var model = await service.GetAsync(
                identityScopeId,
                new ApplicationKey(applicationKey),
                modelVersion,
                cancellationToken);
            return model is null ? NotFound() : Ok(ApplicationSecurityModelResponse.From(model));
        }

        /// <summary>
        /// Registers an immutable project-owned JSON manifest. Re-registering identical normalized
        /// semantics is idempotent; reusing the model version for different semantics is a conflict.
        /// </summary>
        [HttpPut("{modelVersion:int}")]
        [RequireAdministrationCapability(
            IdentityAccessAdministrationCapabilities.Resource,
            IdentityAccessAdministrationCapabilities.SecurityModels,
            IdentityAccessAdministrationCapabilities.Write)]
        public async Task<ActionResult<ApplicationSecurityModelResponse>> Register(
            Guid identityScopeId,
            string applicationKey,
            int modelVersion,
            [FromBody] RegisterApplicationSecurityManifestRequest request,
            CancellationToken cancellationToken)
        {
            if (!feature.TryGet(out var service)) return ApiProblems.SecurityCatalogAdministrationUnavailable();

            var application = new ApplicationKey(applicationKey);
            var manifestApplication = new ApplicationKey(request.ApplicationKey);
            if (application != manifestApplication || modelVersion != request.ModelVersion)
            {
                return BadRequest();
            }

            ArgumentNullException.ThrowIfNull(request.Rbac);
            ArgumentNullException.ThrowIfNull(request.Resources);

            var capabilities = request.Resources.SelectMany(resource =>
            {
                ArgumentNullException.ThrowIfNull(resource);
                ArgumentNullException.ThrowIfNull(resource.Features);
                return resource.Features.SelectMany(feature =>
                {
                    ArgumentNullException.ThrowIfNull(feature);
                    ArgumentNullException.ThrowIfNull(feature.Actions);
                    return feature.Actions.Select(action =>
                    {
                        ArgumentNullException.ThrowIfNull(action);
                        return new ApplicationSecurityManifestCapability(
                            new CapabilityKey(resource.Name, feature.Name, action.Name),
                            action.DisplayName);
                    });
                });
            }).ToArray();

            var manifest = new ApplicationSecurityManifest(
                request.SchemaVersion,
                application,
                modelVersion,
                request.Rbac.Project,
                request.Rbac.Namespaces,
                capabilities);

            var registered = await service.RegisterAsync(identityScopeId, manifest, cancellationToken);
            return Ok(ApplicationSecurityModelResponse.From(registered));
        }
    }
}
