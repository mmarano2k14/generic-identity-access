using System.Net;
using System.Net.Http.Json;
using IdentityAccess.Contracts;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace IdentityAccess.Tests
{

    public sealed class ApiFactory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            // Minimal-hosting startup reads these settings while Program is executing.
            // Pin the shared test host explicitly so ambient developer-machine environment
            // variables cannot accidentally enable routing, PostgreSQL, or authentication.
            builder.UseEnvironment("Testing");
            builder.UseSetting("IdentityAccess:Routing:Provider", "none");
            // Use an explicit empty value rather than null. A null host setting does not
            // override an ambient IdentityAccess__Routing__FilePath environment variable.
            builder.UseSetting("IdentityAccess:Routing:FilePath", string.Empty);
            builder.UseSetting("IdentityAccess:PostgreSql:Enabled", "false");
            builder.UseSetting("IdentityAccess:Authentication:Enabled", "false");
            builder.UseSetting("IdentityAccess:Authorization:Enabled", "false");
        }
    }
}
