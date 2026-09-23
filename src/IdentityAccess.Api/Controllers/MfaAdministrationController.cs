using IdentityAccess.Api.Features;
using IdentityAccess.Api.Http;
using IdentityAccess.Api.Security;
using IdentityAccess.Application.Authentication.Mfa;
using IdentityAccess.Domain;
using Microsoft.AspNetCore.Mvc;

namespace IdentityAccess.Api.Controllers
{
    /// <summary>Administers provider-neutral MFA policy and generic authenticator lifecycle metadata.</summary>
    [ApiController]
    [Route("api/v1/identity-scopes/{identityScopeId:guid}/applications/{applicationKey}/mfa")]
    [Produces("application/json")]
    public sealed class MfaAdministrationController(
        OptionalFeature<IMfaAdministrationService> feature) : ControllerBase
    {
        /// <summary>Lists authentication-factor providers installed in this host.</summary>
        [HttpGet("providers")]
        [RequireAdministrationCapability(
            IdentityAccessAdministrationCapabilities.Resource,
            IdentityAccessAdministrationCapabilities.MfaPolicies,
            IdentityAccessAdministrationCapabilities.Read)]
        public ActionResult<IReadOnlyList<MfaProviderResponse>> ListProviders()
        {
            if (!feature.TryGet(out var service)) return ApiProblems.MfaAdministrationUnavailable();
            return Ok(service.ListProviders().Select(MfaProviderResponse.From).ToArray());
        }

        /// <summary>Gets the current MFA policy.</summary>
        [HttpGet("policy")]
        [RequireAdministrationCapability(
            IdentityAccessAdministrationCapabilities.Resource,
            IdentityAccessAdministrationCapabilities.MfaPolicies,
            IdentityAccessAdministrationCapabilities.Read)]
        public async Task<ActionResult<MfaPolicyResponse>> GetPolicy(
            Guid identityScopeId,
            string applicationKey,
            CancellationToken cancellationToken)
        {
            if (!feature.TryGet(out var service)) return ApiProblems.MfaAdministrationUnavailable();
            var record = await service.GetPolicyAsync(
                identityScopeId,
                new ApplicationKey(applicationKey),
                cancellationToken);
            return record is null ? NotFound() : Ok(MfaPolicyResponse.From(record));
        }

        /// <summary>Creates the initial MFA policy.</summary>
        [HttpPost("policy")]
        [RequireAdministrationCapability(
            IdentityAccessAdministrationCapabilities.Resource,
            IdentityAccessAdministrationCapabilities.MfaPolicies,
            IdentityAccessAdministrationCapabilities.Write)]
        public async Task<ActionResult<MfaPolicyResponse>> CreatePolicy(
            Guid identityScopeId,
            string applicationKey,
            [FromBody] CreateMfaPolicyRequest request,
            CancellationToken cancellationToken)
        {
            if (!feature.TryGet(out var service)) return ApiProblems.MfaAdministrationUnavailable();
            if (!TryPolicy(request.Mode, request.AllowedProviders, out var providerKeys))
                return ApiProblems.BadRequest("Invalid MFA policy or provider list.");

            try
            {
                var created = await service.CreatePolicyAsync(
                    identityScopeId,
                    new ApplicationKey(applicationKey),
                    request.Mode,
                    providerKeys,
                    cancellationToken);
                return StatusCode(StatusCodes.Status201Created, MfaPolicyResponse.From(created));
            }
            catch (AuthenticationFactorProviderNotRegisteredException)
            {
                return ApiProblems.BadRequest("MFA policy references a provider that is not registered on this host.");
            }
        }

        /// <summary>Updates the MFA policy using optimistic concurrency.</summary>
        [HttpPut("policy")]
        [RequireAdministrationCapability(
            IdentityAccessAdministrationCapabilities.Resource,
            IdentityAccessAdministrationCapabilities.MfaPolicies,
            IdentityAccessAdministrationCapabilities.Write)]
        public async Task<ActionResult<MfaPolicyResponse>> UpdatePolicy(
            Guid identityScopeId,
            string applicationKey,
            [FromBody] UpdateMfaPolicyRequest request,
            CancellationToken cancellationToken)
        {
            if (!feature.TryGet(out var service)) return ApiProblems.MfaAdministrationUnavailable();
            if (request.ExpectedVersion < 1 || !TryPolicy(request.Mode, request.AllowedProviders, out var providerKeys))
                return ApiProblems.BadRequest("Invalid MFA policy, provider list, or expected version.");

            try
            {
                var updated = await service.UpdatePolicyAsync(
                    identityScopeId,
                    new ApplicationKey(applicationKey),
                    request.Mode,
                    providerKeys,
                    request.ExpectedVersion,
                    cancellationToken);
                return Ok(MfaPolicyResponse.From(updated));
            }
            catch (AuthenticationFactorProviderNotRegisteredException)
            {
                return ApiProblems.BadRequest("MFA policy references a provider that is not registered on this host.");
            }
        }

        /// <summary>Lists generic authenticators for one user.</summary>
        [HttpGet("users/{userId:guid}/authenticators")]
        [RequireAdministrationCapability(
            IdentityAccessAdministrationCapabilities.Resource,
            IdentityAccessAdministrationCapabilities.MfaAuthenticators,
            IdentityAccessAdministrationCapabilities.Read)]
        public async Task<ActionResult<IReadOnlyList<UserAuthenticatorResponse>>> ListAuthenticators(
            Guid identityScopeId,
            string applicationKey,
            Guid userId,
            CancellationToken cancellationToken)
        {
            if (!feature.TryGet(out var service)) return ApiProblems.MfaAdministrationUnavailable();
            var records = await service.ListAuthenticatorsAsync(
                identityScopeId,
                new ApplicationKey(applicationKey),
                userId,
                cancellationToken);
            return Ok(records.Select(UserAuthenticatorResponse.From).ToArray());
        }

        /// <summary>Revokes one authenticator without deleting provider-specific forensic state.</summary>
        [HttpDelete("users/{userId:guid}/authenticators/{authenticatorId:guid}")]
        [RequireAdministrationCapability(
            IdentityAccessAdministrationCapabilities.Resource,
            IdentityAccessAdministrationCapabilities.MfaAuthenticators,
            IdentityAccessAdministrationCapabilities.Write)]
        public async Task<ActionResult<UserAuthenticatorResponse>> RevokeAuthenticator(
            Guid identityScopeId,
            string applicationKey,
            Guid userId,
            Guid authenticatorId,
            [FromQuery] long expectedVersion,
            CancellationToken cancellationToken)
        {
            if (!feature.TryGet(out var service)) return ApiProblems.MfaAdministrationUnavailable();
            if (expectedVersion < 1) return ApiProblems.BadRequest("Expected version must be positive.");

            var updated = await service.RevokeAuthenticatorAsync(
                identityScopeId,
                new ApplicationKey(applicationKey),
                userId,
                authenticatorId,
                expectedVersion,
                cancellationToken);
            return updated is null ? NotFound() : Ok(UserAuthenticatorResponse.From(updated));
        }

        private static bool TryPolicy(
            MfaPolicyMode mode,
            IEnumerable<string>? values,
            out IReadOnlyCollection<AuthenticationFactorProviderKey> providers)
        {
            providers = Array.Empty<AuthenticationFactorProviderKey>();
            if (!Enum.IsDefined(mode) || values is null) return false;

            try
            {
                providers = values
                    .Select(value => new AuthenticationFactorProviderKey(value))
                    .Distinct()
                    .ToArray();
            }
            catch (ArgumentException)
            {
                return false;
            }

            return mode == MfaPolicyMode.Disabled
                ? providers.Count == 0
                : providers.Count > 0;
        }
    }
}
