using IdentityAccess.Api.Features;
using IdentityAccess.Api.Http;
using IdentityAccess.Api.Security;
using IdentityAccess.Application.Security;
using Microsoft.AspNetCore.Mvc;
using OrganisationProfile.Application.Storage;
using OrganisationProfile.Application.Templates;
using OrganisationProfile.Domain;

namespace IdentityAccess.Api.Controllers
{
    /// <summary>Administers reusable OrganisationProfile template definitions.</summary>
    [ApiController]
    [Route("api/v1/identity-scopes/{identityScopeId:guid}/applications/{applicationKey}/organisation-profile-templates")]
    [Produces("application/json")]
    public sealed class OrganisationProfileTemplatesController(
        OptionalFeature<IOrganisationProfileTemplateStore> storeFeature,
        OptionalFeature<OrganisationProfileTemplateDefinitionService> definitionFeature,
        IOrganisationProfileSecurityAuditWriter auditWriter) : ControllerBase
    {
        /// <summary>Lists reusable template definitions.</summary>
        [HttpGet]
        [RequireAdministrationCapability(
            OrganisationProfileAdministrationCapabilities.Resource,
            OrganisationProfileAdministrationCapabilities.Templates,
            OrganisationProfileAdministrationCapabilities.Read)]
        public async Task<ActionResult<IReadOnlyList<OrganisationProfileTemplateResponse>>> List(
            Guid identityScopeId,
            string applicationKey,
            [FromQuery] int? offset,
            [FromQuery] int? limit,
            CancellationToken cancellationToken)
        {
            _ = identityScopeId;
            _ = applicationKey;

            var resolvedOffset = offset ?? 0;
            var resolvedLimit = limit ?? 100;

            if (resolvedOffset < 0 ||
                resolvedLimit is < 1 or > 500)
            {
                return BadRequest();
            }

            if (!storeFeature.TryGet(out var store))
            {
                return ApiProblems.OrganisationProfileAdministrationUnavailable();
            }

            var templates = await store.ListAsync(
                resolvedOffset,
                resolvedLimit,
                cancellationToken);

            return Ok(
                templates
                    .Select(OrganisationProfileTemplateResponse.From)
                    .ToArray());
        }

        /// <summary>Gets one reusable template definition.</summary>
        [HttpGet("{templateKey}")]
        [RequireAdministrationCapability(
            OrganisationProfileAdministrationCapabilities.Resource,
            OrganisationProfileAdministrationCapabilities.Templates,
            OrganisationProfileAdministrationCapabilities.Read)]
        public async Task<ActionResult<OrganisationProfileTemplateResponse>> Get(
            Guid identityScopeId,
            string applicationKey,
            string templateKey,
            CancellationToken cancellationToken)
        {
            _ = identityScopeId;
            _ = applicationKey;

            if (!storeFeature.TryGet(out var store))
            {
                return ApiProblems.OrganisationProfileAdministrationUnavailable();
            }

            var template = await store.GetAsync(
                new OrganisationProfileTemplateKey(templateKey),
                cancellationToken);

            return template is null
                ? NotFound()
                : Ok(
                    OrganisationProfileTemplateResponse.From(
                        template));
        }

        /// <summary>Creates one active reusable template definition.</summary>
        [HttpPost]
        [RequireAdministrationCapability(
            OrganisationProfileAdministrationCapabilities.Resource,
            OrganisationProfileAdministrationCapabilities.Templates,
            OrganisationProfileAdministrationCapabilities.Write)]
        [ProducesResponseType<OrganisationProfileTemplateResponse>(
            StatusCodes.Status201Created)]
        public async Task<ActionResult<OrganisationProfileTemplateResponse>> Create(
            Guid identityScopeId,
            string applicationKey,
            [FromBody] CreateOrganisationProfileTemplateRequest request,
            CancellationToken cancellationToken)
        {
            if (!definitionFeature.TryGet(out var service))
            {
                return ApiProblems.OrganisationProfileAdministrationUnavailable();
            }

            var key =
                new OrganisationProfileTemplateKey(
                    request.TemplateKey);

            var created = await service.CreateAsync(
                key,
                request.DisplayName,
                cancellationToken);

            await auditWriter.TryWriteAsync(
                identityScopeId,
                tenantId: null,
                applicationKey,
                SecurityAuditEventType.OrganisationProfileTemplateCreated,
                key.Value,
                cancellationToken);

            return CreatedAtAction(
                nameof(Get),
                new
                {
                    identityScopeId,
                    applicationKey,
                    templateKey = key.Value
                },
                OrganisationProfileTemplateResponse.From(
                    created));
        }

        /// <summary>Updates human-readable template metadata.</summary>
        [HttpPut("{templateKey}")]
        [RequireAdministrationCapability(
            OrganisationProfileAdministrationCapabilities.Resource,
            OrganisationProfileAdministrationCapabilities.Templates,
            OrganisationProfileAdministrationCapabilities.Write)]
        public async Task<ActionResult<OrganisationProfileTemplateResponse>> Update(
            Guid identityScopeId,
            string applicationKey,
            string templateKey,
            [FromBody] UpdateOrganisationProfileTemplateRequest request,
            CancellationToken cancellationToken)
        {
            if (!definitionFeature.TryGet(out var service))
            {
                return ApiProblems.OrganisationProfileAdministrationUnavailable();
            }

            var key =
                new OrganisationProfileTemplateKey(
                    templateKey);

            var updated = await service.RenameAsync(
                key,
                request.DisplayName,
                request.ExpectedRowVersion,
                cancellationToken);

            if (updated is null)
            {
                return NotFound();
            }

            await auditWriter.TryWriteAsync(
                identityScopeId,
                tenantId: null,
                applicationKey,
                SecurityAuditEventType.OrganisationProfileTemplateUpdated,
                key.Value,
                cancellationToken);

            return Ok(
                OrganisationProfileTemplateResponse.From(
                    updated));
        }

        /// <summary>Disables a template definition for new Draft/publication activity.</summary>
        [HttpPost("{templateKey}/disable")]
        [RequireAdministrationCapability(
            OrganisationProfileAdministrationCapabilities.Resource,
            OrganisationProfileAdministrationCapabilities.Templates,
            OrganisationProfileAdministrationCapabilities.Write)]
        public Task<ActionResult<OrganisationProfileTemplateResponse>> Disable(
            Guid identityScopeId,
            string applicationKey,
            string templateKey,
            [FromBody] OrganisationProfileTemplateLifecycleRequest request,
            CancellationToken cancellationToken) =>
            ChangeStatus(
                identityScopeId,
                applicationKey,
                templateKey,
                request.ExpectedRowVersion,
                OrganisationProfileTemplateStatus.Disabled,
                cancellationToken);

        /// <summary>Re-enables a template definition.</summary>
        [HttpPost("{templateKey}/enable")]
        [RequireAdministrationCapability(
            OrganisationProfileAdministrationCapabilities.Resource,
            OrganisationProfileAdministrationCapabilities.Templates,
            OrganisationProfileAdministrationCapabilities.Write)]
        public Task<ActionResult<OrganisationProfileTemplateResponse>> Enable(
            Guid identityScopeId,
            string applicationKey,
            string templateKey,
            [FromBody] OrganisationProfileTemplateLifecycleRequest request,
            CancellationToken cancellationToken) =>
            ChangeStatus(
                identityScopeId,
                applicationKey,
                templateKey,
                request.ExpectedRowVersion,
                OrganisationProfileTemplateStatus.Active,
                cancellationToken);

        private async Task<ActionResult<OrganisationProfileTemplateResponse>> ChangeStatus(
            Guid identityScopeId,
            string applicationKey,
            string templateKey,
            long expectedRowVersion,
            OrganisationProfileTemplateStatus status,
            CancellationToken cancellationToken)
        {
            if (!definitionFeature.TryGet(out var service))
            {
                return ApiProblems.OrganisationProfileAdministrationUnavailable();
            }

            var key =
                new OrganisationProfileTemplateKey(
                    templateKey);

            var updated = await service.SetStatusAsync(
                key,
                status,
                expectedRowVersion,
                cancellationToken);

            if (updated is null)
            {
                return NotFound();
            }

            await auditWriter.TryWriteAsync(
                identityScopeId,
                tenantId: null,
                applicationKey,
                SecurityAuditEventType.OrganisationProfileTemplateStatusChanged,
                key.Value,
                cancellationToken);

            return Ok(
                OrganisationProfileTemplateResponse.From(
                    updated));
        }
    }
}
