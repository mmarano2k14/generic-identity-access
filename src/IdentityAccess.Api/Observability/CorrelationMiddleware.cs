using System.Diagnostics;
using IdentityAccess.Application.Security;

namespace IdentityAccess.Api.Observability
{
    /// <summary>
    /// Adds a server-generated correlation identifier to responses and establishes a structured
    /// logging scope for the complete HTTP request.
    /// </summary>
    internal sealed class CorrelationMiddleware(
        RequestDelegate next,
        ILogger<CorrelationMiddleware> logger)
    {
        internal const string ResponseHeaderName = "X-Correlation-ID";

        /// <summary>Executes the request within a correlation-aware structured logging scope.</summary>
        public async Task InvokeAsync(HttpContext context)
        {
            ArgumentNullException.ThrowIfNull(context);

            var correlationId = Activity.Current?.TraceId.ToString();
            if (string.IsNullOrWhiteSpace(correlationId))
                correlationId = context.TraceIdentifier;

            Activity.Current?.SetTag(
                SecurityAuditActivityTagNames.CorrelationId,
                correlationId);

            context.Response.OnStarting(() =>
            {
                context.Response.Headers[ResponseHeaderName] = correlationId;
                return Task.CompletedTask;
            });

            using var scope = logger.BeginScope(
                new Dictionary<string, object>(StringComparer.Ordinal)
                {
                    ["CorrelationId"] = correlationId
                });

            var started = Stopwatch.GetTimestamp();
            await next(context);

            var elapsed = Stopwatch.GetElapsedTime(started);
            var endpoint = context.GetEndpoint()?.DisplayName ?? "unmatched";

            logger.LogInformation(
                "HTTP {Method} {Endpoint} completed with status {StatusCode} in {ElapsedMilliseconds} ms.",
                context.Request.Method,
                endpoint,
                context.Response.StatusCode,
                elapsed.TotalMilliseconds);
        }
    }
}
