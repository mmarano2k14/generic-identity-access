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
    /// <summary>Queries and resolves immutable effective OrganisationProfile semantic versions.</summary>
    [ApiController]
    [Route("api/v1/identity-scopes/{identityScopeId:guid}/applications/{applicationKey}/tenants/{tenantId:guid}/organisation-profiles/{organisationProfileId:guid}/effective-versions")]
    [Produces("application/json")]
    public sealed class OrganisationProfileEffectiveVersionsController(
        OptionalFeature<IOrganisationProfileStore> profileStoreFeature,
        OptionalFeature<IOrganisationProfileVersionStore> versionStoreFeature,
        OptionalFeature<OrganisationProfileCompositionService> compositionFeature,
        IOrganisationProfileSecurityAuditWriter auditWriter) : ControllerBase
    {
        /// <summary>Lists immutable semantic versions in ascending version order.</summary>
        [HttpGet]
        [RequireAdministrationCapability(
            OrganisationProfileAdministrationCapabilities.Resource,
            OrganisationProfileAdministrationCapabilities.EffectiveVersions,
            OrganisationProfileAdministrationCapabilities.Read)]
        public async Task<ActionResult<IReadOnlyList<EffectiveOrganisationProfileResponse>>> List(
            Guid identityScopeId,
            string applicationKey,
            Guid tenantId,
            Guid organisationProfileId,
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

            if (!profileStoreFeature.TryGet(out var profileStore) ||
                !versionStoreFeature.TryGet(out var versionStore))
            {
                return ApiProblems.OrganisationProfileAdministrationUnavailable();
            }

            var profileId =
                new OrganisationProfileId(organisationProfileId);

            if (!await BelongsToRouteAsync(
                    profileStore,
                    profileId,
                    identityScopeId,
                    tenantId,
                    cancellationToken))
            {
                return NotFound();
            }

            var versions = await versionStore.ListAsync(
                profileId,
                resolvedOffset,
                resolvedLimit,
                cancellationToken);

            return Ok(
                versions
                    .Select(EffectiveOrganisationProfileResponse.From)
                    .ToArray());
        }

        /// <summary>Gets one immutable semantic profile version.</summary>
        [HttpGet("{version:long}")]
        [RequireAdministrationCapability(
            OrganisationProfileAdministrationCapabilities.Resource,
            OrganisationProfileAdministrationCapabilities.EffectiveVersions,
            OrganisationProfileAdministrationCapabilities.Read)]
        public async Task<ActionResult<EffectiveOrganisationProfileResponse>> Get(
            Guid identityScopeId,
            string applicationKey,
            Guid tenantId,
            Guid organisationProfileId,
            long version,
            CancellationToken cancellationToken)
        {
            _ = applicationKey;

            if (!profileStoreFeature.TryGet(out var profileStore) ||
                !versionStoreFeature.TryGet(out var versionStore))
            {
                return ApiProblems.OrganisationProfileAdministrationUnavailable();
            }

            var profileId =
                new OrganisationProfileId(organisationProfileId);

            if (!await BelongsToRouteAsync(
                    profileStore,
                    profileId,
                    identityScopeId,
                    tenantId,
                    cancellationToken))
            {
                return NotFound();
            }

            var effective = await versionStore.GetAsync(
                profileId,
                new OrganisationProfileVersionNumber(version),
                cancellationToken);

            if (effective is null ||
                !OrganisationProfileTenantBoundary.Matches(
                    effective,
                    identityScopeId,
                    tenantId))
            {
                return NotFound();
            }

            return Ok(
                EffectiveOrganisationProfileResponse.From(
                    effective));
        }

        /// <summary>Resolves deterministic effective content and appends a semantic version only when changed.</summary>
        [HttpPost("resolve")]
        [RequireAdministrationCapability(
            OrganisationProfileAdministrationCapabilities.Resource,
            OrganisationProfileAdministrationCapabilities.EffectiveVersions,
            OrganisationProfileAdministrationCapabilities.Write)]
        public async Task<ActionResult<EffectiveOrganisationProfileResponse>> Resolve(
            Guid identityScopeId,
            string applicationKey,
            Guid tenantId,
            Guid organisationProfileId,
            [FromBody] ResolveOrganisationProfileRequest request,
            CancellationToken cancellationToken)
        {
            if (!profileStoreFeature.TryGet(out var profileStore) ||
                !compositionFeature.TryGet(out var composition))
            {
                return ApiProblems.OrganisationProfileAdministrationUnavailable();
            }

            var profileId =
                new OrganisationProfileId(organisationProfileId);

            if (!await BelongsToRouteAsync(
                    profileStore,
                    profileId,
                    identityScopeId,
                    tenantId,
                    cancellationToken))
            {
                return NotFound();
            }

            var effective = await composition.ResolveAndSnapshotAsync(
                profileId,
                request.ExpectedRowVersion,
                cancellationToken);

            if (effective is null)
            {
                return NotFound();
            }

            await auditWriter.TryWriteAsync(
                identityScopeId,
                tenantId,
                applicationKey,
                SecurityAuditEventType.OrganisationProfileEffectiveVersionResolved,
                $"{profileId}@{effective.Version}",
                cancellationToken);

            return Ok(
                EffectiveOrganisationProfileResponse.From(
                    effective));
        }

        private static async Task<bool> BelongsToRouteAsync(
            IOrganisationProfileStore profileStore,
            OrganisationProfileId profileId,
            Guid identityScopeId,
            Guid tenantId,
            CancellationToken cancellationToken)
        {
            var profile = await profileStore.GetAsync(
                profileId,
                cancellationToken);

            return profile is not null &&
                OrganisationProfileTenantBoundary.Matches(
                    profile,
                    identityScopeId,
                    tenantId);
        }
    }
}
