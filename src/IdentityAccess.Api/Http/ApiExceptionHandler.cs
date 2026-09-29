using IdentityAccess.Application.Routing;
using IdentityAccess.Application.Storage;
using IdentityAccess.Infrastructure.PostgreSql;
using Microsoft.AspNetCore.Diagnostics;
using OrganizationDirectory.Application.Storage;

namespace IdentityAccess.Api.Http
{
    /// <summary>
    /// Maps expected application and infrastructure exceptions to stable HTTP problem responses.
    /// </summary>
    internal sealed class ApiExceptionHandler(
        IProblemDetailsService problemDetailsService,
        ILogger<ApiExceptionHandler> logger) : IExceptionHandler
    {
        /// <summary>
        /// Attempts to handle an expected exception without exposing internal exception details.
        /// </summary>
        public async ValueTask<bool> TryHandleAsync(
            HttpContext httpContext,
            Exception exception,
            CancellationToken cancellationToken)
        {
            if (exception is OperationCanceledException &&
                httpContext.RequestAborted.IsCancellationRequested)
            {
                return false;
            }

            var problem = exception switch
            {
                ArgumentException =>
                    ApiProblems.Details(
                        StatusCodes.Status400BadRequest,
                        "Invalid request",
                        "One or more request values are invalid."),

                IdentityConcurrencyException =>
                    ApiProblems.Details(
                        StatusCodes.Status409Conflict,
                        "Concurrency conflict",
                        "The resource was modified by another operation."),

                OrganizationConcurrencyException =>
                    ApiProblems.Details(
                        StatusCodes.Status409Conflict,
                        "Organization concurrency conflict",
                        "The organization was modified by another operation."),

                OrganizationHierarchyConflictException =>
                    ApiProblems.Details(
                        StatusCodes.Status409Conflict,
                        "Organization hierarchy conflict",
                        "The requested organization hierarchy mutation is not allowed."),

                OrganizationKeyConflictException =>
                    ApiProblems.Details(
                        StatusCodes.Status409Conflict,
                        "Organization key conflict",
                        "The organization key is already in use in this tenant."),

                OrganizationIdentityConflictException =>
                    ApiProblems.Details(
                        StatusCodes.Status409Conflict,
                        "Organization identity conflict",
                        "The organization identity is already in use in this tenant."),

                OrganizationParentNotFoundException =>
                    ApiProblems.Details(
                        StatusCodes.Status404NotFound,
                        "Organization parent not found",
                        "The requested parent organization does not exist in this tenant."),

                OrganizationTenantNotFoundException =>
                    ApiProblems.Details(
                        StatusCodes.Status404NotFound,
                        "Tenant not found",
                        "The requested Identity Access tenant does not exist."),

                OrganizationMembershipAlreadyExistsException =>
                    ApiProblems.Details(
                        StatusCodes.Status409Conflict,
                        "Organization membership already exists",
                        "The tenant member already belongs to this organization."),

                OrganizationMembershipConcurrencyException =>
                    ApiProblems.Details(
                        StatusCodes.Status409Conflict,
                        "Organization membership concurrency conflict",
                        "The organization membership was modified by another operation."),

                OrganizationMembershipReferenceNotFoundException =>
                    ApiProblems.Details(
                        StatusCodes.Status404NotFound,
                        "Organization not found",
                        "The requested organization does not exist in this tenant."),

                TenantMembershipReferenceNotFoundException =>
                    ApiProblems.Details(
                        StatusCodes.Status404NotFound,
                        "Tenant membership not found",
                        "The requested Identity Access tenant membership does not exist in this tenant."),

                TenantMembershipInactiveException =>
                    ApiProblems.Details(
                        StatusCodes.Status409Conflict,
                        "Tenant membership inactive",
                        "An inactive tenant membership cannot be added to or reactivated in an organization."),

                OrganizationMembershipOrganizationInactiveException =>
                    ApiProblems.Details(
                        StatusCodes.Status409Conflict,
                        "Organization inactive",
                        "Organization membership cannot be added or activated while the organization is inactive."),

                OrganizationResourceScopeLinkAlreadyExistsException =>
                    ApiProblems.Details(
                        StatusCodes.Status409Conflict,
                        "Organization ResourceScope link already exists",
                        "The Organization already has a ResourceScope link for this application."),

                OrganizationResourceScopeAlreadyLinkedException =>
                    ApiProblems.Details(
                        StatusCodes.Status409Conflict,
                        "ResourceScope already linked",
                        "The ResourceScope is already linked to another Organization in this application."),

                OrganizationResourceScopeLinkConcurrencyException =>
                    ApiProblems.Details(
                        StatusCodes.Status409Conflict,
                        "Organization ResourceScope-link concurrency conflict",
                        "The ResourceScope link was modified by another operation."),

                OrganizationResourceScopeReferenceNotFoundException =>
                    ApiProblems.Details(
                        StatusCodes.Status404NotFound,
                        "ResourceScope not found",
                        "The requested ResourceScope does not exist in the expected tenant and application boundary."),

                OrganizationResourceScopeInactiveException =>
                    ApiProblems.Details(
                        StatusCodes.Status409Conflict,
                        "ResourceScope inactive",
                        "An inactive ResourceScope cannot be newly linked or used to replace the current Organization link."),

                OrganizationResourceScopeOrganizationNotFoundException =>
                    ApiProblems.Details(
                        StatusCodes.Status404NotFound,
                        "Organization not found",
                        "The requested Organization does not exist in this tenant."),

                OrganizationResourceScopeOrganizationInactiveException =>
                    ApiProblems.Details(
                        StatusCodes.Status409Conflict,
                        "Organization inactive",
                        "ResourceScope linkage cannot be changed while the Organization is inactive."),

                DatabaseRouteException =>
                    ApiProblems.Details(
                        StatusCodes.Status503ServiceUnavailable,
                        "Database routing unavailable",
                        "The requested identity data route is currently unavailable."),

                PostgreSqlStorageException =>
                    ApiProblems.Details(
                        StatusCodes.Status503ServiceUnavailable,
                        "Storage unavailable",
                        "The identity data store is currently unavailable."),

                _ => null
            };

            if (problem is null)
            {
                return false;
            }

            if (problem.Status is >= StatusCodes.Status500InternalServerError)
            {
                logger.LogError(
                    exception,
                    "Handled API dependency failure with HTTP status {StatusCode}.",
                    problem.Status);
            }
            else
            {
                logger.LogWarning(
                    exception,
                    "Handled API request failure with HTTP status {StatusCode}.",
                    problem.Status);
            }

            httpContext.Response.StatusCode = problem.Status!.Value;

            var context = new ProblemDetailsContext
            {
                HttpContext = httpContext,
                ProblemDetails = problem,
                Exception = exception
            };

            if (!await problemDetailsService.TryWriteAsync(context))
            {
                await httpContext.Response.WriteAsJsonAsync(problem, cancellationToken);
            }

            return true;
        }
    }
}
