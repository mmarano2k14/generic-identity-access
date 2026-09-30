using IdentityAccess.Api.Features;
using IdentityAccess.Api.Http;
using IdentityAccess.Api.OrganisationProfiles;
using IdentityAccess.Api.Security;
using IdentityAccess.Application.Security;
using Microsoft.AspNetCore.Mvc;
using OrganisationProfile.Application.Composition;
using OrganisationProfile.Application.Storage;
using OrganisationProfile.Domain;

namespace IdentityAccess.Api.Controllers
{
    /// <summary>Administers Organization-specific domain overrides for one profile.</summary>
    [ApiController]
    [Route("api/v1/identity-scopes/{identityScopeId:guid}/applications/{applicationKey}/tenants/{tenantId:guid}/organisation-profiles/{organisationProfileId:guid}/domain-overrides")]
    [Produces("application/json")]
    public sealed class OrganisationProfileDomainOverridesController(
        OptionalFeature<IOrganisationProfileStore> profileStoreFeature,
        OptionalFeature<IOrganisationProfileDomainOverrideStore> overrideStoreFeature,
        OptionalFeature<OrganisationProfileDomainOverrideService> serviceFeature,
        IOrganisationProfileSecurityAuditWriter auditWriter) : ControllerBase
    {
        /// <summary>Lists the complete deterministic override set.</summary>
        [HttpGet]
        [RequireAdministrationCapability(
            OrganisationProfileAdministrationCapabilities.Resource,
            OrganisationProfileAdministrationCapabilities.DomainOverrides,
            OrganisationProfileAdministrationCapabilities.Read)]
        public async Task<ActionResult<IReadOnlyList<OrganisationProfileDomainOverrideResponse>>> List(
            Guid identityScopeId,
            string applicationKey,
            Guid tenantId,
            Guid organisationProfileId,
            CancellationToken cancellationToken)
        {
            _ = applicationKey;

            if (!profileStoreFeature.TryGet(out var profileStore) ||
                !overrideStoreFeature.TryGet(out var overrideStore))
            {
                return ApiProblems.OrganisationProfileAdministrationUnavailable();
            }

            var profileId =
                new OrganisationProfileId(organisationProfileId);

            var profile = await profileStore.GetAsync(
                profileId,
                cancellationToken);

            if (profile is null ||
                !OrganisationProfileTenantBoundary.Matches(
                    profile,
                    identityScopeId,
                    tenantId))
            {
                return NotFound();
            }

            var overrides = await overrideStore.ListAsync(
                profileId,
                cancellationToken);

            return Ok(
                overrides
                    .Select(OrganisationProfileDomainOverrideResponse.From)
                    .ToArray());
        }

        /// <summary>Atomically replaces the complete override set and advances profile RowVersion.</summary>
        [HttpPut]
        [RequireAdministrationCapability(
            OrganisationProfileAdministrationCapabilities.Resource,
            OrganisationProfileAdministrationCapabilities.DomainOverrides,
            OrganisationProfileAdministrationCapabilities.Write)]
        public async Task<ActionResult<OrganisationProfileResponse>> Replace(
            Guid identityScopeId,
            string applicationKey,
            Guid tenantId,
            Guid organisationProfileId,
            [FromBody] ReplaceOrganisationProfileDomainOverridesRequest request,
            CancellationToken cancellationToken)
        {
            if (!profileStoreFeature.TryGet(out var profileStore) ||
                !serviceFeature.TryGet(out var service))
            {
                return ApiProblems.OrganisationProfileAdministrationUnavailable();
            }

            var profileId =
                new OrganisationProfileId(organisationProfileId);

            var current = await profileStore.GetAsync(
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

            var updated = await service.ReplaceAsync(
                profileId,
                request.ExpectedRowVersion,
                OrganisationProfileRequestMapping.DomainOverrides(
                    request.Overrides),
                cancellationToken);

            if (updated is null)
            {
                return NotFound();
            }

            await auditWriter.TryWriteAsync(
                identityScopeId,
                tenantId,
                applicationKey,
                SecurityAuditEventType.OrganisationProfileDomainOverridesReplaced,
                profileId.ToString(),
                cancellationToken);

            return Ok(OrganisationProfileResponse.From(updated));
        }
    }
}
