using IdentityAccess.Api.Features;
using IdentityAccess.Api.Http;
using IdentityAccess.Api.Security;
using IdentityAccess.Application.Administration;
using IdentityAccess.Application.Security;
using IdentityAccess.Domain;
using Microsoft.AspNetCore.Mvc;

namespace IdentityAccess.Api.Controllers
{
    /// <summary>Exposes bounded, read-only security audit administration.</summary>
    [ApiController]
    [Route("api/v1/identity-scopes/{identityScopeId:guid}/applications/{applicationKey}/security-audit")]
    [Produces("application/json")]
    public sealed class SecurityAuditEventsController(
        OptionalFeature<ISecurityAuditAdministrationService> feature) : ControllerBase
    {
        /// <summary>Lists a newest-first window of secret-safe audit events.</summary>
        [HttpGet]
        [RequireAdministrationCapability(
            IdentityAccessAdministrationCapabilities.Resource,
            IdentityAccessAdministrationCapabilities.SecurityAudit,
            IdentityAccessAdministrationCapabilities.Read)]
        public async Task<ActionResult<IReadOnlyList<SecurityAuditEventResponse>>> List(
            Guid identityScopeId,
            string applicationKey,
            [FromQuery] Guid? tenantId,
            [FromQuery] Guid? userId,
            [FromQuery] string? eventType,
            [FromQuery] string? outcome,
            [FromQuery] string? correlationId,
            [FromQuery] int? offset,
            [FromQuery] int? limit,
            CancellationToken cancellationToken)
        {
            var resolvedOffset = offset ?? 0;
            var resolvedLimit = limit ?? AdministrationPaging.DefaultLimit;
            if (!AdministrationPaging.IsValid(resolvedOffset, resolvedLimit)) return BadRequest();
            if (!TryParseOptional(eventType, out SecurityAuditEventType? resolvedEventType)) return BadRequest();
            if (!TryParseOptional(outcome, out SecurityAuditOutcome? resolvedOutcome)) return BadRequest();
            if (correlationId is { Length: > 64 }) return BadRequest();
            if (!feature.TryGet(out var service)) return ApiProblems.SecurityAuditUnavailable();

            var query = new SecurityAuditQuery(
                tenantId,
                userId,
                resolvedEventType,
                resolvedOutcome,
                string.IsNullOrEmpty(correlationId) ? null : correlationId,
                resolvedOffset,
                resolvedLimit);

            var records = await service.ListAsync(
                identityScopeId,
                new ApplicationKey(applicationKey),
                query,
                cancellationToken);

            return Ok(records.Select(SecurityAuditEventResponse.From).ToArray());
        }

        private static bool TryParseOptional<TEnum>(string? value, out TEnum? parsed)
            where TEnum : struct, Enum
        {
            if (string.IsNullOrEmpty(value))
            {
                parsed = null;
                return true;
            }

            if (Enum.TryParse<TEnum>(value, ignoreCase: false, out var resolved) && Enum.IsDefined(typeof(TEnum), resolved))
            {
                parsed = resolved;
                return true;
            }

            parsed = null;
            return false;
        }
    }
}
