using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace IdentityAccess.Api.Diagnostics
{
    /// <summary>
    /// Registers runtime diagnostics and tagged ASP.NET Core health checks.
    /// </summary>
    internal static class DiagnosticsServiceRegistration
    {
        /// <summary>Registers service diagnostics, liveness, and readiness health checks.</summary>
        public static void AddIdentityAccessDiagnostics(this WebApplicationBuilder builder)
        {
            ArgumentNullException.ThrowIfNull(builder);

            builder.Services.AddSingleton<IServiceDiagnostics, ServiceDiagnostics>();

            builder.Services
                .AddHealthChecks()
                .AddCheck(
                    "identity-access-self",
                    () => HealthCheckResult.Healthy("Process is alive."),
                    tags: ["live"])
                .AddCheck<IdentityAccessReadinessHealthCheck>(
                    "identity-access-readiness",
                    tags: ["ready"]);
        }
    }
}
