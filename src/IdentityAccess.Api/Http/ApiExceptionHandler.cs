using IdentityAccess.Application.Routing;
using IdentityAccess.Application.Storage;
using IdentityAccess.Infrastructure.PostgreSql;
using Microsoft.AspNetCore.Diagnostics;

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
