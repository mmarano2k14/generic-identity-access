using IdentityAccess.Application.Routing;
using IdentityAccess.Application.Storage;
using IdentityAccess.Infrastructure.PostgreSql;
using Microsoft.AspNetCore.Diagnostics;
using OrganizationDirectory.Application.Storage;
using OrganisationProfile.Application.Registry;
using OrganisationProfile.Application.Storage;

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

                OrganisationProfileConcurrencyException =>
                    ApiProblems.Details(
                        StatusCodes.Status409Conflict,
                        "OrganisationProfile concurrency conflict",
                        "The OrganisationProfile was modified by another operation."),

                OrganisationProfileAlreadyExistsException =>
                    ApiProblems.Details(
                        StatusCodes.Status409Conflict,
                        "OrganisationProfile already exists",
                        "The Organization already has an OrganisationProfile."),

                OrganisationProfileIdentityConflictException =>
                    ApiProblems.Details(
                        StatusCodes.Status409Conflict,
                        "OrganisationProfile identity conflict",
                        "The OrganisationProfile identity conflicts with durable state."),

                OrganisationProfileOrganizationNotFoundException =>
                    ApiProblems.Details(
                        StatusCodes.Status404NotFound,
                        "Organization not found",
                        "The referenced Organization does not exist."),

                OrganisationProfileOrganizationInactiveException =>
                    ApiProblems.Details(
                        StatusCodes.Status409Conflict,
                        "Organization disabled",
                        "The referenced Organization is disabled."),

                OrganisationProfileInactiveException =>
                    ApiProblems.Details(
                        StatusCodes.Status409Conflict,
                        "OrganisationProfile disabled",
                        "The requested profile mutation requires an active OrganisationProfile."),

                OrganisationProfileTemplateAlreadyExistsException =>
                    ApiProblems.Details(
                        StatusCodes.Status409Conflict,
                        "OrganisationProfile template conflict",
                        "The template key already exists."),

                OrganisationProfileTemplateNotFoundException =>
                    ApiProblems.Details(
                        StatusCodes.Status404NotFound,
                        "OrganisationProfile template not found",
                        "The requested template does not exist."),

                OrganisationProfileTemplateInactiveException =>
                    ApiProblems.Details(
                        StatusCodes.Status409Conflict,
                        "OrganisationProfile template disabled",
                        "The requested operation requires an active template definition."),

                OrganisationProfileTemplateConcurrencyException =>
                    ApiProblems.Details(
                        StatusCodes.Status409Conflict,
                        "OrganisationProfile template concurrency conflict",
                        "The template definition was modified by another operation."),

                OrganisationProfileTemplateVersionAlreadyExistsException =>
                    ApiProblems.Details(
                        StatusCodes.Status409Conflict,
                        "OrganisationProfile template version conflict",
                        "The requested template version already exists."),

                OrganisationProfileTemplateVersionNotFoundException =>
                    ApiProblems.Details(
                        StatusCodes.Status404NotFound,
                        "OrganisationProfile template version not found",
                        "The requested template version does not exist."),

                OrganisationProfileTemplateVersionConcurrencyException =>
                    ApiProblems.Details(
                        StatusCodes.Status409Conflict,
                        "OrganisationProfile template version concurrency conflict",
                        "The template version was modified by another operation."),

                OrganisationProfileTemplateVersionImmutableException =>
                    ApiProblems.Details(
                        StatusCodes.Status409Conflict,
                        "OrganisationProfile template version immutable",
                        "Published or retired template content cannot be changed."),

                OrganisationProfileTemplateVersionNotPublishedException =>
                    ApiProblems.Details(
                        StatusCodes.Status409Conflict,
                        "OrganisationProfile template version unavailable",
                        "New profile composition requires a Published template version."),

                DomainRegistryVersionUnavailableException =>
                    ApiProblems.Details(
                        StatusCodes.Status409Conflict,
                        "Domain version unavailable",
                        "One or more exact domain versions are unavailable for the requested operation."),

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
