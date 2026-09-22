# Dependency Injection

The HTTP layer uses constructor injection for controller dependencies.

Controllers do not receive `IServiceProvider`, do not resolve application services through
`GetService`, and do not inspect `HttpContext.RequestServices` to obtain their use-case
dependencies.

## Configurable Features

Some application services exist only when the corresponding infrastructure is configured,
for example:

- directory administration;
- policy administration;
- resource-scope administration;
- local authentication;
- credential administration.

Controllers receive these services through `OptionalFeature<TService>`.

`OptionalFeature<TService>` is always registered after configurable feature registration is
complete. It contains either the exact configured service instance or an unavailable state.

This preserves two requirements simultaneously:

1. controller dependencies remain explicit;
2. an API host can still expose diagnostics and fail closed when an optional security or
   persistence feature is not configured.

The optional feature handle does not provide a fallback implementation and does not change
feature semantics.

## Composition Root

`ApiFeatureRegistration` is part of the API composition root. It is the only place where
optional application services are inspected to create controller-facing feature handles.

Service location inside controllers is prohibited by architecture tests.

## Authorization Filters

Authorization filters remain separate from controller use-case dependency injection.
Their request-pipeline integration is handled independently from controller construction.
