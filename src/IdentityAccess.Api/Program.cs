using IdentityAccess.Api;
using IdentityAccess.Application;
using IdentityAccess.Contracts;

var builder = WebApplication.CreateBuilder(args);

// The foundation has no account, authentication or authorization endpoints.
// Do not allow it to be mistaken for a production identity service.
if (!builder.Environment.IsDevelopment() && !builder.Environment.IsEnvironment("Testing"))
{
    throw new InvalidOperationException(
        "This foundation build may run only in Development or Testing. Production security is not implemented.");
}

builder.Services.AddProblemDetails();
builder.AddIdentityRouting();

var app = builder.Build();
app.UseExceptionHandler();

app.Use(async (context, next) =>
{
    context.Response.Headers.CacheControl = "no-store";
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    await next(context);
});

app.MapGet("/health/live", () => TypedResults.Ok(new LivenessResponse("alive")));
app.MapGet("/health/ready", (FoundationStatus status) =>
    Results.Json(status.Readiness(), statusCode: StatusCodes.Status503ServiceUnavailable));
app.MapGet("/api/v1/system/info", (FoundationStatus status) => TypedResults.Ok(status.Describe()));

// No permissive mock authentication, permissive RBAC, CRUD or database fallback is registered.
app.Run();

public partial class Program { }
