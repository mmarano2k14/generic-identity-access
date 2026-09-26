using IdentityAccess.Api.Features;
using IdentityAccess.Api.Http;
using IdentityAccess.Api.Security;
using IdentityAccess.Application.Administration;
using IdentityAccess.Domain;
using Microsoft.AspNetCore.Mvc;

namespace IdentityAccess.Api.Controllers
{
    /// <summary>Administers reusable application-scoped managed policies.</summary>
    [ApiController]
    [Route("api/v1/identity-scopes/{identityScopeId:guid}/applications/{applicationKey}/managed-policies")]
    [Produces("application/json")]
    public sealed class ManagedPoliciesController(OptionalFeature<IManagedPolicyAdministrationService> feature)
        : ControllerBase
    {
        [HttpGet]
        [RequireAdministrationCapability(
            IdentityAccessAdministrationCapabilities.Resource,
            IdentityAccessAdministrationCapabilities.Policies,
            IdentityAccessAdministrationCapabilities.Read)]
        public async Task<ActionResult<IReadOnlyList<ManagedPolicyResponse>>> List(
            Guid identityScopeId,
            string applicationKey,
            [FromQuery] string? search,
            [FromQuery] int? offset,
            [FromQuery] int? limit,
            CancellationToken cancellationToken)
        {
            var resolvedOffset = offset ?? 0;
            var resolvedLimit = limit ?? AdministrationPaging.DefaultLimit;
            if (!AdministrationPaging.IsValid(resolvedOffset, resolvedLimit)) return BadRequest();
            if (!feature.TryGet(out var service)) return ApiProblems.ManagedPolicyAdministrationUnavailable();
            var records = await service.ListPoliciesAsync(
                identityScopeId,
                new ApplicationKey(applicationKey),
                search,
                resolvedOffset,
                resolvedLimit,
                cancellationToken);
            return Ok(records.Select(ManagedPolicyResponse.From).ToArray());
        }

        [HttpGet("{policyId:guid}")]
        [RequireAdministrationCapability(
            IdentityAccessAdministrationCapabilities.Resource,
            IdentityAccessAdministrationCapabilities.Policies,
            IdentityAccessAdministrationCapabilities.Read)]
        public async Task<ActionResult<ManagedPolicyResponse>> Get(
            Guid identityScopeId,
            string applicationKey,
            Guid policyId,
            CancellationToken cancellationToken)
        {
            if (!feature.TryGet(out var service)) return ApiProblems.ManagedPolicyAdministrationUnavailable();
            var record = await service.GetPolicyAsync(
                identityScopeId,
                new ApplicationKey(applicationKey),
                policyId,
                cancellationToken);
            return record is null ? NotFound() : Ok(ManagedPolicyResponse.From(record));
        }

        [HttpPost]
        [RequireAdministrationCapability(
            IdentityAccessAdministrationCapabilities.Resource,
            IdentityAccessAdministrationCapabilities.Policies,
            IdentityAccessAdministrationCapabilities.Write)]
        [ProducesResponseType<ManagedPolicyResponse>(StatusCodes.Status201Created)]
        public async Task<ActionResult<ManagedPolicyResponse>> Create(
            Guid identityScopeId,
            string applicationKey,
            [FromBody] CreateManagedPolicyRequest request,
            CancellationToken cancellationToken)
        {
            if (!feature.TryGet(out var service)) return ApiProblems.ManagedPolicyAdministrationUnavailable();
            var policyId = request.PolicyId == Guid.Empty ? Guid.NewGuid() : request.PolicyId;
            var created = await service.CreatePolicyAsync(
                identityScopeId,
                new ApplicationKey(applicationKey),
                policyId,
                new ManagedPolicyKey(request.PolicyKey),
                request.DisplayName,
                request.Status,
                cancellationToken);
            return CreatedAtAction(
                nameof(Get),
                new { identityScopeId, applicationKey, policyId },
                ManagedPolicyResponse.From(created));
        }

        [HttpPut("{policyId:guid}")]
        [RequireAdministrationCapability(
            IdentityAccessAdministrationCapabilities.Resource,
            IdentityAccessAdministrationCapabilities.Policies,
            IdentityAccessAdministrationCapabilities.Write)]
        public async Task<ActionResult<ManagedPolicyResponse>> Update(
            Guid identityScopeId,
            string applicationKey,
            Guid policyId,
            [FromBody] UpdateManagedPolicyRequest request,
            CancellationToken cancellationToken)
        {
            if (!feature.TryGet(out var service)) return ApiProblems.ManagedPolicyAdministrationUnavailable();
            var updated = await service.UpdatePolicyAsync(
                identityScopeId,
                new ApplicationKey(applicationKey),
                policyId,
                new ManagedPolicyKey(request.PolicyKey),
                request.DisplayName,
                request.Status,
                request.ExpectedVersion,
                cancellationToken);
            return updated is null ? NotFound() : Ok(ManagedPolicyResponse.From(updated));
        }

        [HttpGet("{policyId:guid}/versions")]
        [RequireAdministrationCapability(
            IdentityAccessAdministrationCapabilities.Resource,
            IdentityAccessAdministrationCapabilities.Policies,
            IdentityAccessAdministrationCapabilities.Read)]
        public async Task<ActionResult<IReadOnlyList<ManagedPolicyVersionResponse>>> ListVersions(
            Guid identityScopeId,
            string applicationKey,
            Guid policyId,
            CancellationToken cancellationToken)
        {
            if (!feature.TryGet(out var service)) return ApiProblems.ManagedPolicyAdministrationUnavailable();
            var versions = await service.ListVersionsAsync(
                identityScopeId,
                new ApplicationKey(applicationKey),
                policyId,
                cancellationToken);
            return Ok(versions.Select(ManagedPolicyVersionResponse.From).ToArray());
        }

        [HttpGet("{policyId:guid}/versions/{policyVersion:int}")]
        [RequireAdministrationCapability(
            IdentityAccessAdministrationCapabilities.Resource,
            IdentityAccessAdministrationCapabilities.Policies,
            IdentityAccessAdministrationCapabilities.Read)]
        public async Task<ActionResult<ManagedPolicyVersionResponse>> GetVersion(
            Guid identityScopeId,
            string applicationKey,
            Guid policyId,
            int policyVersion,
            CancellationToken cancellationToken)
        {
            if (!feature.TryGet(out var service)) return ApiProblems.ManagedPolicyAdministrationUnavailable();
            var version = await service.GetVersionAsync(
                identityScopeId,
                new ApplicationKey(applicationKey),
                policyId,
                policyVersion,
                cancellationToken);
            return version is null ? NotFound() : Ok(ManagedPolicyVersionResponse.From(version));
        }

        [HttpPost("{policyId:guid}/versions")]
        [RequireAdministrationCapability(
            IdentityAccessAdministrationCapabilities.Resource,
            IdentityAccessAdministrationCapabilities.Policies,
            IdentityAccessAdministrationCapabilities.Write)]
        [ProducesResponseType<ManagedPolicyVersionResponse>(StatusCodes.Status201Created)]
        public async Task<ActionResult<ManagedPolicyVersionResponse>> CreateVersion(
            Guid identityScopeId,
            string applicationKey,
            Guid policyId,
            [FromBody] CreateManagedPolicyVersionRequest request,
            CancellationToken cancellationToken)
        {
            if (!feature.TryGet(out var service)) return ApiProblems.ManagedPolicyAdministrationUnavailable();
            var version = await service.CreateVersionAsync(
                identityScopeId,
                new ApplicationKey(applicationKey),
                policyId,
                request.PolicyVersion,
                request.ModelVersion,
                cancellationToken);
            if (version is null) return NotFound();
            return CreatedAtAction(
                nameof(GetVersion),
                new { identityScopeId, applicationKey, policyId, policyVersion = request.PolicyVersion },
                ManagedPolicyVersionResponse.From(version));
        }

        [HttpPut("{policyId:guid}/versions/{policyVersion:int}/publish")]
        [RequireAdministrationCapability(
            IdentityAccessAdministrationCapabilities.Resource,
            IdentityAccessAdministrationCapabilities.Policies,
            IdentityAccessAdministrationCapabilities.Write)]
        public async Task<ActionResult<ManagedPolicyVersionResponse>> PublishVersion(
            Guid identityScopeId,
            string applicationKey,
            Guid policyId,
            int policyVersion,
            [FromBody] PublishManagedPolicyVersionRequest request,
            CancellationToken cancellationToken)
        {
            if (!feature.TryGet(out var service)) return ApiProblems.ManagedPolicyAdministrationUnavailable();
            var version = await service.PublishVersionAsync(
                identityScopeId,
                new ApplicationKey(applicationKey),
                policyId,
                policyVersion,
                request.MakeDefault,
                cancellationToken);
            return version is null ? NotFound() : Ok(ManagedPolicyVersionResponse.From(version));
        }

        [HttpGet("{policyId:guid}/versions/{policyVersion:int}/statements")]
        [RequireAdministrationCapability(
            IdentityAccessAdministrationCapabilities.Resource,
            IdentityAccessAdministrationCapabilities.PolicyStatements,
            IdentityAccessAdministrationCapabilities.Read)]
        public async Task<ActionResult<IReadOnlyList<ManagedPolicyStatementResponse>>> ListStatements(
            Guid identityScopeId,
            string applicationKey,
            Guid policyId,
            int policyVersion,
            CancellationToken cancellationToken)
        {
            if (!feature.TryGet(out var service)) return ApiProblems.ManagedPolicyAdministrationUnavailable();
            var statements = await service.ListStatementsAsync(
                identityScopeId,
                new ApplicationKey(applicationKey),
                policyId,
                policyVersion,
                cancellationToken);
            return Ok(statements.Select(ManagedPolicyStatementResponse.From).ToArray());
        }

        [HttpPost("{policyId:guid}/versions/{policyVersion:int}/statements")]
        [RequireAdministrationCapability(
            IdentityAccessAdministrationCapabilities.Resource,
            IdentityAccessAdministrationCapabilities.PolicyStatements,
            IdentityAccessAdministrationCapabilities.Write)]
        [ProducesResponseType<ManagedPolicyStatementResponse>(StatusCodes.Status201Created)]
        public async Task<ActionResult<ManagedPolicyStatementResponse>> AddStatement(
            Guid identityScopeId,
            string applicationKey,
            Guid policyId,
            int policyVersion,
            [FromBody] AddManagedPolicyStatementRequest request,
            CancellationToken cancellationToken)
        {
            if (!feature.TryGet(out var service)) return ApiProblems.ManagedPolicyAdministrationUnavailable();
            var statementId = request.StatementId == Guid.Empty ? Guid.NewGuid() : request.StatementId;
            var statement = await service.AddStatementAsync(
                identityScopeId,
                new ApplicationKey(applicationKey),
                policyId,
                policyVersion,
                statementId,
                new CapabilityPattern(request.Resource, request.Feature, request.Action),
                cancellationToken);
            if (statement is null) return NotFound();
            return Created($"{Request.Path}/{statement.StatementId}", ManagedPolicyStatementResponse.From(statement));
        }

        [HttpDelete("{policyId:guid}/versions/{policyVersion:int}/statements/{statementId:guid}")]
        [RequireAdministrationCapability(
            IdentityAccessAdministrationCapabilities.Resource,
            IdentityAccessAdministrationCapabilities.PolicyStatements,
            IdentityAccessAdministrationCapabilities.Write)]
        public async Task<IActionResult> RemoveStatement(
            Guid identityScopeId,
            string applicationKey,
            Guid policyId,
            int policyVersion,
            Guid statementId,
            CancellationToken cancellationToken)
        {
            if (!feature.TryGet(out var service)) return ApiProblems.ManagedPolicyAdministrationUnavailable();
            var removed = await service.RemoveStatementAsync(
                identityScopeId,
                new ApplicationKey(applicationKey),
                policyId,
                policyVersion,
                statementId,
                cancellationToken);
            return removed ? NoContent() : NotFound();
        }
    }
}
