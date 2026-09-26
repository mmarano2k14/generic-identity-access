using IdentityAccess.Api.Features;
using IdentityAccess.Api.Http;
using IdentityAccess.Api.Security;
using IdentityAccess.Application.Administration;
using IdentityAccess.Domain;
using Microsoft.AspNetCore.Mvc;

namespace IdentityAccess.Api.Controllers
{
    /// <summary>Administers identity-scope authority policies and statements.</summary>
    [ApiController]
    [Route("api/v1/identity-scopes/{identityScopeId:guid}/applications/{applicationKey}/scope-authority/policies")]
    [Produces("application/json")]
    public sealed class IdentityScopeAdministrationPoliciesController(
        OptionalFeature<IIdentityScopeAuthorityAdministrationService> feature)
        : ControllerBase
    {
        /// <summary>Lists scope-authority policies in a bounded deterministic window.</summary>
        [HttpGet]
        [RequireAdministrationCapability(
            IdentityAccessAdministrationCapabilities.Resource,
            IdentityAccessAdministrationCapabilities.IdentityScopeAuthorityPolicies,
            IdentityAccessAdministrationCapabilities.Read)]
        public async Task<ActionResult<IReadOnlyList<IdentityScopeAdministrationPolicyResponse>>> List(
            Guid identityScopeId, string applicationKey, [FromQuery] string? search, [FromQuery] int? offset, [FromQuery] int? limit,
            CancellationToken cancellationToken)
        {
            var resolvedOffset = offset ?? 0;
            var resolvedLimit = limit ?? AdministrationPaging.DefaultLimit;
            if (!AdministrationPaging.IsValid(resolvedOffset, resolvedLimit)) return BadRequest();
            if (!feature.TryGet(out var service))
                return ApiProblems.IdentityScopeAuthorityAdministrationUnavailable();

            var records = await service.ListPoliciesAsync(identityScopeId, new ApplicationKey(applicationKey), search,
                resolvedOffset, resolvedLimit, cancellationToken);
            return Ok(records.Select(IdentityScopeAdministrationPolicyResponse.From).ToArray());
        }

        /// <summary>Gets a policy.</summary>
        [HttpGet("{policyId:guid}")]
        [RequireAdministrationCapability(
            IdentityAccessAdministrationCapabilities.Resource,
            IdentityAccessAdministrationCapabilities.IdentityScopeAuthorityPolicies,
            IdentityAccessAdministrationCapabilities.Read)]
        public async Task<ActionResult<IdentityScopeAdministrationPolicyResponse>> Get(
            Guid identityScopeId,
            string applicationKey,
            Guid policyId,
            CancellationToken cancellationToken)
        {
            if (!feature.TryGet(out var service))
                return ApiProblems.IdentityScopeAuthorityAdministrationUnavailable();

            var record = await service.GetPolicyAsync(
                identityScopeId,
                new ApplicationKey(applicationKey),
                policyId,
                cancellationToken);

            return record is null
                ? NotFound()
                : Ok(IdentityScopeAdministrationPolicyResponse.From(record));
        }

        /// <summary>Creates a policy.</summary>
        [HttpPost]
        [RequireAdministrationCapability(
            IdentityAccessAdministrationCapabilities.Resource,
            IdentityAccessAdministrationCapabilities.IdentityScopeAuthorityPolicies,
            IdentityAccessAdministrationCapabilities.Write)]
        public async Task<ActionResult<IdentityScopeAdministrationPolicyResponse>> Create(
            Guid identityScopeId,
            string applicationKey,
            [FromBody] CreatePolicyRequest request,
            CancellationToken cancellationToken)
        {
            if (!feature.TryGet(out var service))
                return ApiProblems.IdentityScopeAuthorityAdministrationUnavailable();

            var policyId = request.PolicyId == Guid.Empty ? Guid.NewGuid() : request.PolicyId;

            var created = await service.CreatePolicyAsync(
                identityScopeId,
                new ApplicationKey(applicationKey),
                policyId,
                request.DisplayName,
                request.Status,
                cancellationToken);

            return CreatedAtAction(
                nameof(Get),
                new { identityScopeId, applicationKey, policyId },
                IdentityScopeAdministrationPolicyResponse.From(created));
        }

        /// <summary>Updates a policy.</summary>
        [HttpPut("{policyId:guid}")]
        [RequireAdministrationCapability(
            IdentityAccessAdministrationCapabilities.Resource,
            IdentityAccessAdministrationCapabilities.IdentityScopeAuthorityPolicies,
            IdentityAccessAdministrationCapabilities.Write)]
        public async Task<ActionResult<IdentityScopeAdministrationPolicyResponse>> Update(
            Guid identityScopeId,
            string applicationKey,
            Guid policyId,
            [FromBody] UpdatePolicyRequest request,
            CancellationToken cancellationToken)
        {
            if (!feature.TryGet(out var service))
                return ApiProblems.IdentityScopeAuthorityAdministrationUnavailable();

            var updated = await service.UpdatePolicyAsync(
                identityScopeId,
                new ApplicationKey(applicationKey),
                policyId,
                request.DisplayName,
                request.Status,
                request.ExpectedVersion,
                cancellationToken);

            return Ok(IdentityScopeAdministrationPolicyResponse.From(updated));
        }

        /// <summary>Lists statements.</summary>
        [HttpGet("{policyId:guid}/statements")]
        [RequireAdministrationCapability(
            IdentityAccessAdministrationCapabilities.Resource,
            IdentityAccessAdministrationCapabilities.IdentityScopeAuthorityStatements,
            IdentityAccessAdministrationCapabilities.Read)]
        public async Task<ActionResult<IReadOnlyList<IdentityScopeAdministrationPolicyStatementResponse>>> ListStatements(
            Guid identityScopeId,
            string applicationKey,
            Guid policyId,
            CancellationToken cancellationToken)
        {
            if (!feature.TryGet(out var service))
                return ApiProblems.IdentityScopeAuthorityAdministrationUnavailable();

            var statements = await service.ListStatementsAsync(
                identityScopeId,
                new ApplicationKey(applicationKey),
                policyId,
                cancellationToken);

            return Ok(statements.Select(IdentityScopeAdministrationPolicyStatementResponse.From).ToArray());
        }

        /// <summary>Adds a statement.</summary>
        [HttpPost("{policyId:guid}/statements")]
        [RequireAdministrationCapability(
            IdentityAccessAdministrationCapabilities.Resource,
            IdentityAccessAdministrationCapabilities.IdentityScopeAuthorityStatements,
            IdentityAccessAdministrationCapabilities.Write)]
        public async Task<ActionResult<IdentityScopeAdministrationPolicyStatementResponse>> AddStatement(
            Guid identityScopeId,
            string applicationKey,
            Guid policyId,
            [FromBody] AddPolicyStatementRequest request,
            CancellationToken cancellationToken)
        {
            if (!feature.TryGet(out var service))
                return ApiProblems.IdentityScopeAuthorityAdministrationUnavailable();

            var statementId = request.StatementId == Guid.Empty ? Guid.NewGuid() : request.StatementId;

            var statement = await service.AddStatementAsync(
                identityScopeId,
                new ApplicationKey(applicationKey),
                policyId,
                statementId,
                request.ModelVersion,
                new CapabilityPattern(request.Resource, request.Feature, request.Action),
                cancellationToken);

            return Created(
                $"{Request.Path}/{statement.StatementId}",
                IdentityScopeAdministrationPolicyStatementResponse.From(statement));
        }

        /// <summary>Removes a statement.</summary>
        [HttpDelete("{policyId:guid}/statements/{statementId:guid}")]
        [RequireAdministrationCapability(
            IdentityAccessAdministrationCapabilities.Resource,
            IdentityAccessAdministrationCapabilities.IdentityScopeAuthorityStatements,
            IdentityAccessAdministrationCapabilities.Write)]
        public async Task<IActionResult> RemoveStatement(
            Guid identityScopeId,
            string applicationKey,
            Guid policyId,
            Guid statementId,
            CancellationToken cancellationToken)
        {
            if (!feature.TryGet(out var service))
                return ApiProblems.IdentityScopeAuthorityAdministrationUnavailable();

            var removed = await service.RemoveStatementAsync(
                identityScopeId,
                new ApplicationKey(applicationKey),
                policyId,
                statementId,
                cancellationToken);

            return removed ? NoContent() : NotFound();
        }
    }
}
