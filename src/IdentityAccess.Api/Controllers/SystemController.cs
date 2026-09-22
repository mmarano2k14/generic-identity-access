using IdentityAccess.Api.Diagnostics;
using IdentityAccess.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace IdentityAccess.Api.Controllers
{
    /// <summary>Exposes safe runtime service metadata.</summary>
    [ApiController]
    [Route("api/v1/system")]
    [Produces("application/json")]
    public sealed class SystemController(IServiceDiagnostics diagnostics) : ControllerBase
    {
        /// <summary>Returns current service version and configured feature state.</summary>
        [HttpGet("info")]
        [ProducesResponseType<ServiceInfoResponse>(StatusCodes.Status200OK)]
        public ActionResult<ServiceInfoResponse> Info() =>
            Ok(diagnostics.Describe());
    }
}
