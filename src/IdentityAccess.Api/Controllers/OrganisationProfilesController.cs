using IdentityAccess.Api.Features;
using IdentityAccess.Api.Http;
using IdentityAccess.Api.OrganisationProfiles;
using IdentityAccess.Api.Security;
using IdentityAccess.Application.Security;
using Microsoft.AspNetCore.Mvc;
using OrganisationProfile.Application;
using OrganisationProfile.Application.Storage;
using OrganisationProfile.Domain;

namespace IdentityAccess.Api.Controllers
{
    /// <summary>Administers tenant-owned OrganisationProfile definition state.</summary>
    [ApiController]
    [Route("api/v1/identity-scopes/{identityScopeId:guid}/applications/{applicationKey}/tenants/{tenantId:guid}/organisation-profiles")]
    [Produces("application/json")]
    public sealed class OrganisationProfilesController(
        OptionalFeature<IOrganisationProfileStore> storeFeature,
        OptionalFeature<OrganisationProfileDefinitionService> definitionFeature,
        IOrganisationProfileSecurityAuditWriter auditWriter) : ControllerBase
    {
        /// <summary>Lists profiles in a bounded deterministic tenant window.</summary>
        [HttpGet]
        [RequireAdministrationCapability(
            OrganisationProfileAdministrationCapabilities.Resource,
            OrganisationProfileAdministrationCapabilities.Profiles,
            OrganisationProfileAdministrationCapabilities.Read)]
        public async Task<ActionResult<IReadOnlyList<OrganisationProfileResponse>>> List(
            Guid identityScopeId,
            string applicationKey,
            Guid tenantId,
            [FromQuery] int? offset,
            [FromQuery] int? limit,
            CancellationToken cancellationToken)
        {
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

            var profiles = await store.ListAsync(
                identityScopeId,
                tenantId,
                resolvedOffset,
                resolvedLimit,
                cancellationToken);

            return Ok(
                profiles
                    .Select(OrganisationProfileResponse.From)
                    .ToArray());
        }

        /// <summary>Gets one profile while preserving the route tenant boundary.</summary>
        [HttpGet("{organisationProfileId:guid}")]
        [RequireAdministrationCapability(
            OrganisationProfileAdministrationCapabilities.Resource,
            OrganisationProfileAdministrationCapabilities.Profiles,
            OrganisationProfileAdministrationCapabilities.Read)]
        public async Task<ActionResult<OrganisationProfileResponse>> Get(
            Guid identityScopeId,
            string applicationKey,
            Guid tenantId,
            Guid organisationProfileId,
            CancellationToken cancellationToken)
        {
            _ = applicationKey;

            if (!storeFeature.TryGet(out var store))
            {
                return ApiProblems.OrganisationProfileAdministrationUnavailable();
            }

            var profile = await store.GetAsync(
                new OrganisationProfileId(organisationProfileId),
                cancellationToken);

            if (profile is null ||
                !OrganisationProfileTenantBoundary.Matches(
                    profile,
                    identityScopeId,
                    tenantId))
            {
                return NotFound();
            }

            return Ok(OrganisationProfileResponse.From(profile));
        }

        /// <summary>Gets the at-most-one profile attached to one Organization.</summary>
        [HttpGet("by-organization/{organizationId:guid}")]
        [RequireAdministrationCapability(
            OrganisationProfileAdministrationCapabilities.Resource,
            OrganisationProfileAdministrationCapabilities.Profiles,
            OrganisationProfileAdministrationCapabilities.Read)]
        public async Task<ActionResult<OrganisationProfileResponse>> GetByOrganization(
            Guid identityScopeId,
            string applicationKey,
            Guid tenantId,
            Guid organizationId,
            CancellationToken cancellationToken)
        {
            _ = applicationKey;

            if (!storeFeature.TryGet(out var store))
            {
                return ApiProblems.OrganisationProfileAdministrationUnavailable();
            }

            var profile = await store.FindByOrganizationAsync(
                new OrganizationReference(
                    identityScopeId,
                    tenantId,
                    organizationId),
                cancellationToken);

            return profile is null
                ? NotFound()
                : Ok(OrganisationProfileResponse.From(profile));
        }

        /// <summary>Creates one active profile for an active Organization.</summary>
        [HttpPost]
        [RequireAdministrationCapability(
            OrganisationProfileAdministrationCapabilities.Resource,
            OrganisationProfileAdministrationCapabilities.Profiles,
            OrganisationProfileAdministrationCapabilities.Write)]
        [ProducesResponseType<OrganisationProfileResponse>(
            StatusCodes.Status201Created)]
        public async Task<ActionResult<OrganisationProfileResponse>> Create(
            Guid identityScopeId,
            string applicationKey,
            Guid tenantId,
            [FromBody] CreateOrganisationProfileRequest request,
            CancellationToken cancellationToken)
        {
            if (!definitionFeature.TryGet(out var service))
            {
                return ApiProblems.OrganisationProfileAdministrationUnavailable();
            }

            var profileId =
                request.OrganisationProfileId is { } suppliedId &&
                suppliedId != Guid.Empty
                    ? new OrganisationProfileId(suppliedId)
                    : OrganisationProfileId.New();

            var created = await service.CreateAsync(
                profileId,
                new OrganizationReference(
                    identityScopeId,
                    tenantId,
                    request.OrganizationId),
                OrganisationProfileRequestMapping.TemplatePin(
                    request.TemplateKey,
                    request.TemplateVersion),
                cancellationToken);

            await auditWriter.TryWriteAsync(
                identityScopeId,
                tenantId,
                applicationKey,
                SecurityAuditEventType.OrganisationProfileCreated,
                profileId.ToString(),
                cancellationToken);

            return CreatedAtAction(
                nameof(Get),
                new
                {
                    identityScopeId,
                    applicationKey,
                    tenantId,
                    organisationProfileId = profileId.Value
                },
                OrganisationProfileResponse.From(created));
        }

        /// <summary>Replaces or removes the exact immutable template pin.</summary>
        [HttpPut("{organisationProfileId:guid}/template")]
        [RequireAdministrationCapability(
            OrganisationProfileAdministrationCapabilities.Resource,
            OrganisationProfileAdministrationCapabilities.Profiles,
            OrganisationProfileAdministrationCapabilities.Write)]
        public async Task<ActionResult<OrganisationProfileResponse>> SetTemplate(
            Guid identityScopeId,
            string applicationKey,
            Guid tenantId,
            Guid organisationProfileId,
            [FromBody] SetOrganisationProfileTemplateRequest request,
            CancellationToken cancellationToken)
        {
            if (!TryGetMutationServices(
                out var store,
                out var service))
            {
                return ApiProblems.OrganisationProfileAdministrationUnavailable();
            }

            var profileId =
                new OrganisationProfileId(organisationProfileId);

            var current = await store.GetAsync(
                profileId,
                cancellationToken);

            if (current is null ||
                !OrganisationProfileTenantBoundary.Matches(
                    current,
                    identityScopeId,
                    tenantId))
            {
                return NotFound();
            }

            var updated = await service.SetTemplateAsync(
                profileId,
                OrganisationProfileRequestMapping.TemplatePin(
                    request.TemplateKey,
                    request.TemplateVersion),
                request.ExpectedRowVersion,
                cancellationToken);

            if (updated is null)
            {
                return NotFound();
            }

            await auditWriter.TryWriteAsync(
                identityScopeId,
                tenantId,
                applicationKey,
                SecurityAuditEventType.OrganisationProfileTemplatePinChanged,
                profileId.ToString(),
                cancellationToken);

            return Ok(OrganisationProfileResponse.From(updated));
        }

        /// <summary>Disables the profile without deleting durable identity or semantic history.</summary>
        [HttpPost("{organisationProfileId:guid}/disable")]
        [RequireAdministrationCapability(
            OrganisationProfileAdministrationCapabilities.Resource,
            OrganisationProfileAdministrationCapabilities.Profiles,
            OrganisationProfileAdministrationCapabilities.Write)]
        public Task<ActionResult<OrganisationProfileResponse>> Disable(
            Guid identityScopeId,
            string applicationKey,
            Guid tenantId,
            Guid organisationProfileId,
            [FromBody] OrganisationProfileLifecycleRequest request,
            CancellationToken cancellationToken) =>
            ChangeStatus(
                identityScopeId,
                applicationKey,
                tenantId,
                organisationProfileId,
                request.ExpectedRowVersion,
                OrganisationProfileStatus.Disabled,
                cancellationToken);

        /// <summary>Enables the profile when its external Organization is still active.</summary>
        [HttpPost("{organisationProfileId:guid}/enable")]
        [RequireAdministrationCapability(
            OrganisationProfileAdministrationCapabilities.Resource,
            OrganisationProfileAdministrationCapabilities.Profiles,
            OrganisationProfileAdministrationCapabilities.Write)]
        public Task<ActionResult<OrganisationProfileResponse>> Enable(
            Guid identityScopeId,
            string applicationKey,
            Guid tenantId,
            Guid organisationProfileId,
            [FromBody] OrganisationProfileLifecycleRequest request,
            CancellationToken cancellationToken) =>
            ChangeStatus(
                identityScopeId,
                applicationKey,
                tenantId,
                organisationProfileId,
                request.ExpectedRowVersion,
                OrganisationProfileStatus.Active,
                cancellationToken);

        private async Task<ActionResult<OrganisationProfileResponse>> ChangeStatus(
            Guid identityScopeId,
            string applicationKey,
            Guid tenantId,
            Guid organisationProfileId,
            long expectedRowVersion,
            OrganisationProfileStatus status,
            CancellationToken cancellationToken)
        {
            if (!TryGetMutationServices(
                out var store,
                out var service))
            {
                return ApiProblems.OrganisationProfileAdministrationUnavailable();
            }

            var profileId =
                new OrganisationProfileId(organisationProfileId);

            var current = await store.GetAsync(
                profileId,
                cancellationToken);

            if (current is null ||
                !OrganisationProfileTenantBoundary.Matches(
                    current,
                    identityScopeId,
                    tenantId))
            {
                return NotFound();
            }

            var updated = await service.SetStatusAsync(
                profileId,
                status,
                expectedRowVersion,
                cancellationToken);

            if (updated is null)
            {
                return NotFound();
            }

            await auditWriter.TryWriteAsync(
                identityScopeId,
                tenantId,
                applicationKey,
                SecurityAuditEventType.OrganisationProfileStatusChanged,
                profileId.ToString(),
                cancellationToken);

            return Ok(OrganisationProfileResponse.From(updated));
        }

        private bool TryGetMutationServices(
            out IOrganisationProfileStore store,
            out OrganisationProfileDefinitionService service)
        {
            var hasStore = storeFeature.TryGet(out var resolvedStore);
            var hasService = definitionFeature.TryGet(out var resolvedService);

            store = resolvedStore!;
            service = resolvedService!;

            return hasStore && hasService;
        }
    }
}
