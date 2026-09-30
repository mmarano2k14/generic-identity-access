using IdentityAccess.Api.Features;
using IdentityAccess.Api.Http;
using IdentityAccess.Api.OrganisationProfiles;
using IdentityAccess.Api.Security;
using IdentityAccess.Application.Security;
using Microsoft.AspNetCore.Mvc;
using OrganisationProfile.Application.Storage;
using OrganisationProfile.Application.Templates;
using OrganisationProfile.Domain;

namespace IdentityAccess.Api.Controllers
{
    /// <summary>Administers Draft, Published, and Retired OrganisationProfile template versions.</summary>
    [ApiController]
    [Route("api/v1/identity-scopes/{identityScopeId:guid}/applications/{applicationKey}/organisation-profile-templates/{templateKey}/versions")]
    [Produces("application/json")]
    public sealed class OrganisationProfileTemplateVersionsController(
        OptionalFeature<IOrganisationProfileTemplateVersionStore> storeFeature,
        OptionalFeature<OrganisationProfileTemplateDraftService> draftFeature,
        OptionalFeature<OrganisationProfileTemplatePublicationService> publicationFeature,
        IOrganisationProfileSecurityAuditWriter auditWriter) : ControllerBase
    {
        /// <summary>Lists all versions for one template.</summary>
        [HttpGet]
        [RequireAdministrationCapability(
            OrganisationProfileAdministrationCapabilities.Resource,
            OrganisationProfileAdministrationCapabilities.Templates,
            OrganisationProfileAdministrationCapabilities.Read)]
        public async Task<ActionResult<IReadOnlyList<OrganisationProfileTemplateVersionResponse>>> List(
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

            var versions = await store.ListAsync(
                new OrganisationProfileTemplateKey(templateKey),
                cancellationToken);

            return Ok(
                versions
                    .Select(OrganisationProfileTemplateVersionResponse.From)
                    .ToArray());
        }

        /// <summary>Gets one exact template version.</summary>
        [HttpGet("{version:int}")]
        [RequireAdministrationCapability(
            OrganisationProfileAdministrationCapabilities.Resource,
            OrganisationProfileAdministrationCapabilities.Templates,
            OrganisationProfileAdministrationCapabilities.Read)]
        public async Task<ActionResult<OrganisationProfileTemplateVersionResponse>> Get(
            Guid identityScopeId,
            string applicationKey,
            string templateKey,
            int version,
            CancellationToken cancellationToken)
        {
            _ = identityScopeId;
            _ = applicationKey;

            if (!storeFeature.TryGet(out var store))
            {
                return ApiProblems.OrganisationProfileAdministrationUnavailable();
            }

            var templateVersion = await store.GetAsync(
                new OrganisationProfileTemplateKey(templateKey),
                new OrganisationProfileTemplateVersionNumber(version),
                cancellationToken);

            return templateVersion is null
                ? NotFound()
                : Ok(
                    OrganisationProfileTemplateVersionResponse.From(
                        templateVersion));
        }

        /// <summary>Creates one explicit Draft template version.</summary>
        [HttpPost]
        [RequireAdministrationCapability(
            OrganisationProfileAdministrationCapabilities.Resource,
            OrganisationProfileAdministrationCapabilities.Templates,
            OrganisationProfileAdministrationCapabilities.Write)]
        [ProducesResponseType<OrganisationProfileTemplateVersionResponse>(
            StatusCodes.Status201Created)]
        public async Task<ActionResult<OrganisationProfileTemplateVersionResponse>> CreateDraft(
            Guid identityScopeId,
            string applicationKey,
            string templateKey,
            [FromBody] CreateOrganisationProfileTemplateVersionRequest request,
            CancellationToken cancellationToken)
        {
            if (!draftFeature.TryGet(out var service))
            {
                return ApiProblems.OrganisationProfileAdministrationUnavailable();
            }

            var key =
                new OrganisationProfileTemplateKey(
                    templateKey);

            var version =
                new OrganisationProfileTemplateVersionNumber(
                    request.TemplateVersion);

            var created = await service.CreateAsync(
                key,
                version,
                OrganisationProfileRequestMapping.DomainSelections(
                    request.Domains),
                cancellationToken);

            await auditWriter.TryWriteAsync(
                identityScopeId,
                tenantId: null,
                applicationKey,
                SecurityAuditEventType.OrganisationProfileTemplateDraftCreated,
                $"{key.Value}@{version.Value}",
                cancellationToken);

            return CreatedAtAction(
                nameof(Get),
                new
                {
                    identityScopeId,
                    applicationKey,
                    templateKey = key.Value,
                    version = version.Value
                },
                OrganisationProfileTemplateVersionResponse.From(
                    created));
        }

        /// <summary>Atomically replaces the complete domain composition of one Draft version.</summary>
        [HttpPut("{version:int}/domains")]
        [RequireAdministrationCapability(
            OrganisationProfileAdministrationCapabilities.Resource,
            OrganisationProfileAdministrationCapabilities.Templates,
            OrganisationProfileAdministrationCapabilities.Write)]
        public async Task<ActionResult<OrganisationProfileTemplateVersionResponse>> ReplaceDomains(
            Guid identityScopeId,
            string applicationKey,
            string templateKey,
            int version,
            [FromBody] ReplaceOrganisationProfileTemplateDomainsRequest request,
            CancellationToken cancellationToken)
        {
            if (!draftFeature.TryGet(out var service))
            {
                return ApiProblems.OrganisationProfileAdministrationUnavailable();
            }

            var key =
                new OrganisationProfileTemplateKey(
                    templateKey);

            var versionNumber =
                new OrganisationProfileTemplateVersionNumber(
                    version);

            var updated = await service.ReplaceDomainsAsync(
                key,
                versionNumber,
                OrganisationProfileRequestMapping.DomainSelections(
                    request.Domains),
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
                SecurityAuditEventType.OrganisationProfileTemplateDraftCompositionReplaced,
                $"{key.Value}@{versionNumber.Value}",
                cancellationToken);

            return Ok(
                OrganisationProfileTemplateVersionResponse.From(
                    updated));
        }

        /// <summary>Publishes and freezes one Draft template version.</summary>
        [HttpPost("{version:int}/publish")]
        [RequireAdministrationCapability(
            OrganisationProfileAdministrationCapabilities.Resource,
            OrganisationProfileAdministrationCapabilities.Templates,
            OrganisationProfileAdministrationCapabilities.Write)]
        public Task<ActionResult<OrganisationProfileTemplateVersionResponse>> Publish(
            Guid identityScopeId,
            string applicationKey,
            string templateKey,
            int version,
            [FromBody] OrganisationProfileTemplateVersionLifecycleRequest request,
            CancellationToken cancellationToken) =>
            ChangePublicationState(
                identityScopeId,
                applicationKey,
                templateKey,
                version,
                request.ExpectedRowVersion,
                publish: true,
                cancellationToken);

        /// <summary>Retires one immutable Published template version.</summary>
        [HttpPost("{version:int}/retire")]
        [RequireAdministrationCapability(
            OrganisationProfileAdministrationCapabilities.Resource,
            OrganisationProfileAdministrationCapabilities.Templates,
            OrganisationProfileAdministrationCapabilities.Write)]
        public Task<ActionResult<OrganisationProfileTemplateVersionResponse>> Retire(
            Guid identityScopeId,
            string applicationKey,
            string templateKey,
            int version,
            [FromBody] OrganisationProfileTemplateVersionLifecycleRequest request,
            CancellationToken cancellationToken) =>
            ChangePublicationState(
                identityScopeId,
                applicationKey,
                templateKey,
                version,
                request.ExpectedRowVersion,
                publish: false,
                cancellationToken);

        private async Task<ActionResult<OrganisationProfileTemplateVersionResponse>>
            ChangePublicationState(
                Guid identityScopeId,
                string applicationKey,
                string templateKey,
                int version,
                long expectedRowVersion,
                bool publish,
                CancellationToken cancellationToken)
        {
            if (!publicationFeature.TryGet(out var service))
            {
                return ApiProblems.OrganisationProfileAdministrationUnavailable();
            }

            var key =
                new OrganisationProfileTemplateKey(
                    templateKey);

            var versionNumber =
                new OrganisationProfileTemplateVersionNumber(
                    version);

            var updated = publish
                ? await service.PublishAsync(
                    key,
                    versionNumber,
                    expectedRowVersion,
                    cancellationToken)
                : await service.RetireAsync(
                    key,
                    versionNumber,
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
                publish
                    ? SecurityAuditEventType.OrganisationProfileTemplateVersionPublished
                    : SecurityAuditEventType.OrganisationProfileTemplateVersionRetired,
                $"{key.Value}@{versionNumber.Value}",
                cancellationToken);

            return Ok(
                OrganisationProfileTemplateVersionResponse.From(
                    updated));
        }
    }
}
