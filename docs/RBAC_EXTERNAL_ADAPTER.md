# External RBAC Adapter

The external RBAC project remains a separately built and versioned dependency.

Identity Access owns:

```text
IdentityAccess.Rbac
    neutral request/result contracts

IdentityAccess.Rbac.MultiplexedAdapter
    anti-corruption integration boundary
```

Neither the domain nor application layer has a compile-time dependency on:

```text
Multiplexed.Rbac.Core
Multiplexed.Abstractions
```

The adapter also keeps those assemblies out of its compile-time project references.

## Process-Pinned Compatibility Contract

The adapter no longer discovers the external reflection surface independently on every
authorization request.

Each `MultiplexedRbacAuthorizationAdapter` owns one thread-safe lazy binding.

The first compatibility probe or authorization call:

1. verifies both required binary files exist;
2. reads assembly identities and SHA-256 fingerprints;
3. loads the external assemblies;
4. validates every external type, constructor, writable property, and method required by
   the integration contract;
5. builds one immutable reflection binding;
6. pins that binding for the lifetime of the adapter instance.

Replacing external binaries on disk does not hot-swap an already running adapter. A process
restart is required to bind a different external distribution.

This prevents request-to-request contract drift.

## Compatibility Preflight

`IMultiplexedRbacCompatibilityProbe` exposes:

```csharp
MultiplexedRbacCompatibilityReport ProbeCompatibility();
```

The report contains only neutral metadata:

```text
IsCompatible
FailureCode
DiagnosticDetail
CoreAssemblyVersion
CoreAssemblySha256
AbstractionsAssemblyVersion
AbstractionsAssemblySha256
```

No external assembly type is exposed.

When administration RBAC is enabled, application startup creates the same adapter instance
that will be used at runtime and executes the compatibility preflight.

An incompatible distribution fails startup rather than allowing the service to start with a
partially functional authorization boundary.

## Required External Contract

The compatibility binder validates the external surface required to construct:

```text
NamespaceEntry
ExecutionContext
ExecutionContextAccessor
AuthorizationScope
TrnBuilderOptions
OptionsWrapper<TrnBuilderOptions>
TrnBuilder
TrnAuthorizationEngine
```

It also validates the required properties and methods, including:

```text
ExecutionContextAccessor.Set(...)
ExecutionContextAccessor.Clear()
TrnAuthorizationEngine.IsAllowed(string, string, string) -> bool
```

A binary that exists but no longer satisfies this shape produces:

```text
ExternalContractMismatch
```

rather than a business denial.

A binary distribution that cannot be loaded produces:

```text
ExternalLoadFailed
```

## External Context Lifecycle

Every authorization evaluation creates a fresh external execution context and accessor.

After `Set(...)` succeeds, `Clear()` executes from a `finally` block.

Therefore cleanup occurs on:

```text
Allowed
Denied
external exception
cancellation after context establishment
```

A cleanup failure becomes a technical invocation failure rather than being hidden.

Concurrent authorization calls do not share an accessor instance.

## Wildcard Authority

Identity Access does not implement external wildcard authorization semantics.

The adapter only filters candidate TRNs by:

```text
TRN wire shape
project
namespace
```

before importing them into the external execution context.

It does not decide whether:

```text
r:f:*
r:*:a
r:*:*
*:*:a
*:*:*
```

match a concrete capability.

The final decision remains:

```text
TrnAuthorizationEngine.IsAllowed(resource, feature, action)
```

inside the external RBAC implementation.

Partial wildcard patterns are likewise left to the external engine and fail closed under
the compatibility suite.

## Failure Semantics

The neutral result remains:

```text
Allowed
Denied
TechnicalFailure
```

Technical RBAC integration categories are:

```text
ExternalBinariesMissing
ExternalLoadFailed
ExternalContractMismatch
ExternalInvocationFailed
```

None of these categories is converted to `Denied`.

Cancellation remains cancellation and is rethrown.

`DiagnosticDetail` is non-contractual operational detail and must not be used as a stable
decision category.

## Validation

Build the supported external RBAC distribution first and point the compatibility runner to
its `net10.0` output directory:

```powershell
.\scripts\verify-multiplexed-rbac.ps1 `
  -ReferenceDirectory "D:\Dev\Personal\multiplexed-rbac\implementations\dotnet\src\Multiplexed.Rbac.Core\bin\Release\net10.0"
```

The directory must contain:

```text
Multiplexed.Rbac.Core.dll
Multiplexed.Abstractions.dll
```

The integration suite verifies:

```text
compatibility preflight
assembly versions and SHA-256 fingerprints
exact grants
all supported wildcard shapes
partial wildcard rejection
project/namespace isolation
malformed grant isolation
concurrent context independence
```

The main repository remains buildable without vendoring the external RBAC implementation.
