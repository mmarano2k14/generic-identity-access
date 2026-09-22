using IdentityAccess.Api.Diagnostics;
using IdentityAccess.Contracts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace IdentityAccess.Api.Controllers
{
    /// <summary>Exposes liveness and readiness health endpoints.</summary>
    [ApiController]
    [Produces("application/json")]
    public sealed class HealthController(
        HealthCheckService healthChecks,
        IServiceDiagnostics diagnostics) : ControllerBase
    {
        /// <summary>Returns process liveness through the ASP.NET Core health-check subsystem.</summary>
        [HttpGet("/health/live")]
        [ProducesResponseType<LivenessResponse>(StatusCodes.Status200OK)]
        [ProducesResponseType<LivenessResponse>(StatusCodes.Status503ServiceUnavailable)]
        public async Task<ActionResult<LivenessResponse>> Live(
            CancellationToken cancellationToken)
        {
            var report = await healthChecks.CheckHealthAsync(
                registration => registration.Tags.Contains("live"),
                cancellationToken);

            var response = new LivenessResponse(
                report.Status == HealthStatus.Healthy ? "alive" : "unhealthy");

            return report.Status == HealthStatus.Healthy
                ? Ok(response)
                : StatusCode(StatusCodes.Status503ServiceUnavailable, response);
        }

        /// <summary>Returns current dependency readiness derived from the configured host services.</summary>
        [HttpGet("/health/ready")]
        [ProducesResponseType<ReadinessResponse>(StatusCodes.Status200OK)]
        [ProducesResponseType<ReadinessResponse>(StatusCodes.Status503ServiceUnavailable)]
        public async Task<ActionResult<ReadinessResponse>> Ready(
            CancellationToken cancellationToken)
        {
            var report = await healthChecks.CheckHealthAsync(
                registration => registration.Tags.Contains("ready"),
                cancellationToken);

            var response = diagnostics.Readiness();

            return report.Status == HealthStatus.Healthy
                ? Ok(response)
                : StatusCode(StatusCodes.Status503ServiceUnavailable, response);
        }
    }
}
