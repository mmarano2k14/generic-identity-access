using IdentityAccess.Application.Administration;
using IdentityAccess.Domain;
using Microsoft.AspNetCore.Mvc;
using IdentityAccess.Api.Security;
using IdentityAccess.Api.Features;
using IdentityAccess.Api.Http;

namespace IdentityAccess.Api.Controllers
{

    /// <summary>Exposes HTTP endpoints for policies.</summary>
    [ApiController]
    [Route("api/v1/identity-scopes/{identityScopeId:guid}/tenants/{tenantId:guid}/applications/{applicationKey}/policies")]
    [Produces("application/json")]
    public sealed class PoliciesController(OptionalFeature<IPolicyAdministrationService> feature) : ControllerBase
    {
        /// <summary>Gets the requested policies.</summary>
        [HttpGet("{policyId:guid}")]
        [RequireAdministrationCapability(IdentityAccessAdministrationCapabilities.Resource, IdentityAccessAdministrationCapabilities.Policies, IdentityAccessAdministrationCapabilities.Read)]
        public async Task<ActionResult<PolicyResponse>> Get(Guid identityScopeId, Guid tenantId, string applicationKey,
            Guid policyId, CancellationToken cancellationToken)
        {
            if (!feature.TryGet(out var service)) return ApiProblems.PolicyAdministrationUnavailable();
            var record = await service.GetPolicyAsync(identityScopeId, tenantId, new ApplicationKey(applicationKey),
                policyId, cancellationToken);
            return record is null ? NotFound() : Ok(PolicyResponse.From(record));
        }

        /// <summary>Creates policies.</summary>
        [HttpPost]
        [RequireAdministrationCapability(IdentityAccessAdministrationCapabilities.Resource, IdentityAccessAdministrationCapabilities.Policies, IdentityAccessAdministrationCapabilities.Write)]
        [ProducesResponseType<PolicyResponse>(StatusCodes.Status201Created)]
        public async Task<ActionResult<PolicyResponse>> Create(Guid identityScopeId, Guid tenantId,
            string applicationKey, [FromBody] CreatePolicyRequest request, CancellationToken cancellationToken)
        {
            if (!feature.TryGet(out var service)) return ApiProblems.PolicyAdministrationUnavailable();
            var policyId = request.PolicyId == Guid.Empty ? Guid.NewGuid() : request.PolicyId;
            var created = await service.CreatePolicyAsync(identityScopeId, tenantId, new ApplicationKey(applicationKey),
                policyId, request.DisplayName, request.Status, cancellationToken);
            return CreatedAtAction(nameof(Get), new { identityScopeId, tenantId, applicationKey, policyId },
                PolicyResponse.From(created));
        }

        /// <summary>Updates policies.</summary>
        [HttpPut("{policyId:guid}")]
        [RequireAdministrationCapability(IdentityAccessAdministrationCapabilities.Resource, IdentityAccessAdministrationCapabilities.Policies, IdentityAccessAdministrationCapabilities.Write)]
        public async Task<ActionResult<PolicyResponse>> Update(Guid identityScopeId, Guid tenantId,
            string applicationKey, Guid policyId, [FromBody] UpdatePolicyRequest request,
            CancellationToken cancellationToken)
        {
            if (!feature.TryGet(out var service)) return ApiProblems.PolicyAdministrationUnavailable();
            var updated = await service.UpdatePolicyAsync(identityScopeId, tenantId,
                new ApplicationKey(applicationKey), policyId, request.DisplayName, request.Status,
                request.ExpectedVersion, cancellationToken);
            return Ok(PolicyResponse.From(updated));
            
        }

        /// <summary>Lists statements.</summary>
        [HttpGet("{policyId:guid}/statements")]
        [RequireAdministrationCapability(IdentityAccessAdministrationCapabilities.Resource, IdentityAccessAdministrationCapabilities.PolicyStatements, IdentityAccessAdministrationCapabilities.Read)]
        public async Task<ActionResult<IReadOnlyList<PolicyStatementResponse>>> ListStatements(Guid identityScopeId,
            Guid tenantId, string applicationKey, Guid policyId, CancellationToken cancellationToken)
        {
            if (!feature.TryGet(out var service)) return ApiProblems.PolicyAdministrationUnavailable();
            var statements = await service.ListStatementsAsync(identityScopeId, tenantId,
                new ApplicationKey(applicationKey), policyId, cancellationToken);
            return Ok(statements.Select(PolicyStatementResponse.From).ToArray());
        }

        /// <summary>Adds statement.</summary>
        [HttpPost("{policyId:guid}/statements")]
        [RequireAdministrationCapability(IdentityAccessAdministrationCapabilities.Resource, IdentityAccessAdministrationCapabilities.PolicyStatements, IdentityAccessAdministrationCapabilities.Write)]
        [ProducesResponseType<PolicyStatementResponse>(StatusCodes.Status201Created)]
        public async Task<ActionResult<PolicyStatementResponse>> AddStatement(Guid identityScopeId, Guid tenantId,
            string applicationKey, Guid policyId, [FromBody] AddPolicyStatementRequest request,
            CancellationToken cancellationToken)
        {
            if (!feature.TryGet(out var service)) return ApiProblems.PolicyAdministrationUnavailable();
            var statementId = request.StatementId == Guid.Empty ? Guid.NewGuid() : request.StatementId;
            var statement = await service.AddStatementAsync(identityScopeId, tenantId,
                new ApplicationKey(applicationKey), policyId, statementId, request.ModelVersion,
                new CapabilityPattern(request.Resource, request.Feature, request.Action), cancellationToken);
            return Created($"{Request.Path}/{statement.StatementId}", PolicyStatementResponse.From(statement));
        }

        /// <summary>Removes statement.</summary>
        [HttpDelete("{policyId:guid}/statements/{statementId:guid}")]
        [RequireAdministrationCapability(IdentityAccessAdministrationCapabilities.Resource, IdentityAccessAdministrationCapabilities.PolicyStatements, IdentityAccessAdministrationCapabilities.Write)]
        public async Task<IActionResult> RemoveStatement(Guid identityScopeId, Guid tenantId, string applicationKey,
            Guid policyId, Guid statementId, CancellationToken cancellationToken)
        {
            if (!feature.TryGet(out var service)) return ApiProblems.PolicyAdministrationUnavailable();
            var removed = await service.RemoveStatementAsync(identityScopeId, tenantId,
                new ApplicationKey(applicationKey), policyId, statementId, cancellationToken);
            return removed ? NoContent() : NotFound();
        }

    }
}
