# Controller API and Swagger

**Version: 0.11.0.**

The HTTP surface now uses ASP.NET Core MVC controllers rather than Minimal API endpoint mappings.
Swagger/OpenAPI is available at `/swagger` and `/swagger/v1/swagger.json` in the currently allowed Development and Testing hosts.

## Controllers

- `HealthController`: liveness and readiness.
- `SystemController`: service metadata.
- `PoliciesController`: policy metadata and policy statements.
- `PolicyBindingsController`: group-to-policy bindings.

Administrative endpoints are real controller routes, but they remain fail-closed until a trusted `IAdministrationRequestAuthorizer` is registered. The default implementation returns `503 Service Unavailable`; it never grants access. This prevents the new CRUD surface from becoming an unauthenticated administration API before the authentication and trusted access-context integration are complete.

## Administrative routing

Policy routes use the identity scope, tenant, and application from the URL to build server-side references. The database destination is still resolved through `IDatabaseRouteResolver`; no database or connection string is accepted from the HTTP client.

Policy updates require `ExpectedVersion` and map an optimistic-concurrency failure to HTTP `409 Conflict`.

All asynchronous controller operations accept an explicit `CancellationToken` from the HTTP request pipeline.

## Swagger

Swagger documents both diagnostic and administrative controller routes. Swagger is documentation and an invocation surface only; it does not bypass the administration authorization filter.
