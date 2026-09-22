using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace IdentityAccess.Api.Diagnostics
{
    /// <summary>
    /// Bridges Identity Access feature readiness into the ASP.NET Core health-check subsystem.
    /// </summary>
    internal sealed class IdentityAccessReadinessHealthCheck(
        IServiceDiagnostics diagnostics) : IHealthCheck
    {
        /// <inheritdoc />
        public Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var readiness = diagnostics.Readiness();

            if (readiness.Ready)
            {
                return Task.FromResult(
                    HealthCheckResult.Healthy("Identity Access is ready."));
            }

            IReadOnlyDictionary<string, object> data =
                new Dictionary<string, object>(StringComparer.Ordinal)
                {
                    ["blockingCapabilities"] = readiness.BlockingCapabilities.ToArray()
                };

            return Task.FromResult(
                HealthCheckResult.Unhealthy(
                    "Identity Access is not ready.",
                    data: data));
        }
    }
}
