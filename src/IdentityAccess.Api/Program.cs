using IdentityAccess.Api;
using IdentityAccess.Api.Diagnostics;
using IdentityAccess.Api.Http;
using IdentityAccess.Api.Observability;
using IdentityAccess.Api.Security;
using IdentityAccess.Infrastructure.Authentication;
using IdentityAccess.Infrastructure.ConfigurationRouting;
using IdentityAccess.Infrastructure.PostgreSql;
using IdentityAccess.Mfa.Totp;

var builder = WebApplication.CreateBuilder(args);

if (!builder.Environment.IsDevelopment() && !builder.Environment.IsEnvironment("Testing"))
{
    throw new InvalidOperationException(
        "This foundation build may run only in Development or Testing. Production security is not implemented.");
}

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ApiExceptionHandler>();
builder.Services.AddControllers();
builder.Services.AddSingleton<IdentityAccess.Api.Oidc.IOidcLocalSessionResolver, IdentityAccess.Api.Oidc.OidcLocalSessionResolver>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.OpenApiInfo
    {
        Title = "Generic Identity & Access API",
        Version = "v1",
        Description = "Controller-based Identity & Access API with fail-closed local authentication and optional external RBAC administration authorization."
    });
});
builder.Services.AddIdentityAccessConfigurationRouting(
    builder.Configuration.GetSection("IdentityAccess:Routing"),
    builder.Environment.ContentRootPath);
builder.Services.AddIdentityAccessPostgreSql(
    builder.Configuration.GetSection("IdentityAccess:PostgreSql"));
builder.AddIdentityAdministration();
builder.Services.AddIdentityAccessAuthentication(
    builder.Configuration.GetSection("IdentityAccess:Authentication"),
    builder.Environment.ContentRootPath);
builder.Services.AddIdentityAccessTotpProvider(
    builder.Configuration.GetSection("IdentityAccess:Mfa:Totp"));
builder.AddIdentityAdministrationAuthorization();
builder.AddIdentityApiFeatures();
builder.AddIdentityAdministrationSecurity();
builder.AddIdentityAccessDiagnostics();

var app = builder.Build();
app.UseMiddleware<CorrelationMiddleware>();
app.UseExceptionHandler();

app.Use(async (context, next) =>
{
    context.Response.Headers.CacheControl = "no-store";
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    await next(context);
});

app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "Generic Identity & Access API v1");
    options.RoutePrefix = "swagger";
});

app.MapControllers();
app.Run();

/// <summary>Defines the ASP.NET Core application entry point used for hosting and integration testing.</summary>
public partial class Program { }
