# Generic Identity — Application Security

**Release:** 1.4.0  
**Status:** Public SDK and shared UI available  
**Scope:** Generic Identity application security metadata and trusted administration context

## 1. Purpose

Application Security is the Generic Identity category responsible for the security metadata that a consuming application declares and registers with Identity. It exposes the registered security model, its RBAC context, its capability catalog and its resource-scope type definitions without moving authorization decisions into the browser or consumer application.

The category is backed by existing server capabilities:

- `ApplicationSecurityModelsController` for immutable model discovery and manifest registration;
- `ScopeTypesController` for model-scoped resource-scope type discovery and registration;
- `AdministrationContextController` for the server-trusted effective administration context.

No synthetic endpoint or second authorization engine is introduced.

## 2. Public SDK

The categorized client is available as:

```ts
identity.applicationSecurity.models
identity.applicationSecurity.manifests
identity.applicationSecurity.scopeTypes
identity.applicationSecurity.capabilities
identity.applicationSecurity.context
```

Representative operations:

```ts
const models = await identity.applicationSecurity.models.list(context);
const model = await identity.applicationSecurity.models.get(context, 3);

await identity.applicationSecurity.manifests.register(context, manifest);

const scopeTypes = await identity.applicationSecurity.scopeTypes.list(context, 3);
await identity.applicationSecurity.scopeTypes.add(context, 3, request);

const capabilities = await identity.applicationSecurity.capabilities.listForModel(context, 3);
const effectiveContext = await identity.applicationSecurity.context.get(context);
```

The implementation composes the proven `IdentityAccessSecurityModelsClient` and `IdentityAccessAdministrationContextClient`. It does not duplicate HTTP transport or backend semantics.

## 3. Shared presentation surface

The reusable React package exports:

- `ApplicationSecurityModelsPage`;
- `ApplicationSecurityModelDetailsPage`;
- `ApplicationCapabilitiesPage`;
- `ApplicationScopeTypesPage`;
- `ApplicationSecurityContextPage`;
- `ApplicationSecurityManifestForm`;
- `ApplicationScopeTypeForm`.

The Next.js package re-exports the same presentation surface under `@generic-identity/next/application-security`.

Routes, navigation, server actions, branding and product-specific labels remain consumer-owned.

## 4. Structured permission references

`IdentityApplicationSecurityPermissionReference` is the public representation for a capability selected from a registered model and RBAC namespace.

`createApplicationSecurityPermissionReference(...)` validates that:

1. the RBAC namespace is declared by the registered model;
2. the requested capability exists in that model;
3. the returned reference retains the application key, model version and RBAC project that established the coordinates.

The reference is deliberately **not** a credential, grant, access context or authorization result. It also does not expose a public helper that manufactures TRN strings. External TRN materialization and authorization remain server-side concerns.

## 5. Security invariants

The Application Security surface preserves the following rules:

- registration metadata never grants a permission by itself;
- a capability declared by a manifest is catalog metadata, not authority;
- a browser-visible page is not an authorization boundary;
- effective administration context is obtained from the server and is not reconstructed from client input;
- unknown or undeclared capability coordinates fail closed in the structured reference helper;
- authorization evaluation continues through the existing Generic Identity / .NET RBAC path;
- no consumer-specific business semantics are introduced into Generic Identity.

## 6. Compatibility

Existing `administration.*` and legacy TypeScript client surfaces remain available. The categorized `applicationSecurity` surface is additive and is the preferred API for new consumers.

No backend database migration, route change or RBAC behavior change is required by this release.

## 7. Package version

The four public Generic Identity packages advance together to `1.4.0`:

```text
@generic-identity/contracts
@generic-identity/auth
@generic-identity/react
@generic-identity/next
```

This keeps the public package family version-aligned while preserving the existing dependency direction:

```text
contracts
   ↑
auth
   ↑
react
   ↑
next
```
