using IdentityAccess.Contracts;

namespace IdentityAccess.Api.Diagnostics
{
    /// <summary>
    /// Provides safe runtime diagnostics derived from the services actually configured in the
    /// current host.
    /// </summary>
    public interface IServiceDiagnostics
    {
        /// <summary>Returns service metadata and configured feature state.</summary>
        ServiceInfoResponse Describe();

        /// <summary>Returns the current readiness state and explicit blocking capabilities.</summary>
        ReadinessResponse Readiness();
    }
}
