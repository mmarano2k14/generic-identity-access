# Dependencies and Toolchain

## .NET

The repository targets:

```text
net10.0
```

`global.json` defines a stable .NET 10 baseline with feature-band roll-forward enabled.

Centrally managed package versions:

| Package | Version | Purpose |
|---|---:|---|
| `Npgsql` | 10.0.3 | PostgreSQL connectivity and pooling |
| `Swashbuckle.AspNetCore` | 10.2.3 | Swagger / OpenAPI UI and document generation |
| `Microsoft.AspNetCore.Mvc.Testing` | 10.0.12 | ASP.NET Core integration testing |
| `Microsoft.NET.Test.Sdk` | 18.10.0 | .NET test host |
| `xunit.v3` | 3.2.2 | Unit and integration tests |
| `xunit.runner.visualstudio` | 3.1.5 | Visual Studio test runner integration |

Package versions are maintained in `Directory.Packages.props`.

`System.Formats.Cbor` is consumed from the .NET 10 shared framework surface and is intentionally not declared as an explicit NuGet `PackageReference`; explicitly referencing it produces `NU1510` under the repository's warnings-as-errors policy.

## TypeScript

The class-based TypeScript connector uses:

```text
TypeScript 5.8.3
```

Runtime client code has no third-party npm runtime dependency. Authentication/OIDC connector code uses the standard Fetch, URL, TextEncoder, and Web Crypto APIs for transport, PKCE randomness, and S256 hashing.

## PostgreSQL

Local development currently uses PostgreSQL 18. Database access is implemented through Npgsql.

The default local database is:

```text
generic_identity_access_default
```

The owned schema is:

```text
identity_access
```

## External RBAC

External RBAC assemblies are not part of the generic core dependency graph.

`IdentityAccess.Rbac.MultiplexedAdapter` provides the integration boundary.

The external binaries remain runtime-only dependencies. The adapter validates and
process-pins their assembly identities, SHA-256 fingerprints, required reflection types,
constructors, properties, and methods before administration authorization is enabled.

The dedicated compatibility suite loads a separately built external RBAC distribution for
conformance testing.

## Dependency Policy

- dependency versions are explicit and centrally managed where applicable;
- secrets and connection strings are configuration, not package metadata;
- infrastructure implementation details are not exposed through public domain contracts;
- package upgrades require the normal build, test, PostgreSQL, and RBAC compatibility gates appropriate to the affected component.


## OIDC Protocol Dependencies

The Authorization Code + PKCE implementation uses .NET framework cryptography and JSON
APIs directly.

There is no IdentityServer or equivalent external OIDC server dependency.

The active RSA private key is loaded from a trusted server-side PEM path at startup and
never committed to the repository. The public key is exposed through JWKS.
