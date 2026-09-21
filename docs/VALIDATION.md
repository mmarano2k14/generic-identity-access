# Delivery Validation

**Server version: 0.2.0. Date: September 21, 2026.**

## Previous Foundation

The build and tests for foundation version 0.1.0 were reported as green in the consuming
environment.

No log, TRX file, or SDK version accompanied that feedback in this chat.

It therefore represents external confirmation of the foundation, not execution evidence
for the new routing provider.

## Checks Actually Executed for This Increment

| Check | Result | Scope |
|---|---|---|
| `npm test` | 26 tests passed, with no failures or skipped tests | Unchanged diagnostic client; TypeScript build included |
| `npm run typecheck` | Passed | Strict TypeScript typing |
| Local Node HTTP test | Passed within the 26-test suite | HTTP fixture server, not the .NET API |
| XML / JSON formats | Passed | Projects, props, configuration files, and routing example |
| Project graph and solution | Passed | Existing references, no cycles, routing provider included |
| Public contracts and client source | Unchanged | Exact comparison with the foundation archive |
| NuGet dependencies | Unchanged | Exact comparison of `Directory.Packages.props` |
| C# delimiters | Lexical check passed | Strings/comments excluded; this does not compile C# |
| Bash script | Syntax valid | `bash -n`; no .NET execution |

TypeScript execution environment:

```text
Node.js 22.16.0
npm 10.9.2
TypeScript 5.8.3
```

The compiler already present in the environment was used.

No npm restore or newly generated lockfile is presented as executed.

## Not Executed and Not Claimed

The .NET SDK is not installed in the generation environment.

An attempt to retrieve it did not succeed because DNS resolution from the container to
the installation endpoint failed.

No .NET restore, C# build, C# test run, API startup, or client smoke test against the
.NET server was executed here.

Source review and lexical validation do not replace those checks.

The file:

```text
docs/validation/routing-source-checks.json
```

records the structural checks.

The files:

```text
typescript-tests-routing.txt
typescript-typecheck-routing.txt
```

contain outputs from commands actually executed for this increment.

The pre-existing files:

```text
source-checks.json
typescript-tests.txt
typescript-typecheck.txt
```

are historical evidence from version 0.1.0.

They are not replaced by any claimed new .NET result.

## .NET Test Inventory Without Execution Result

The test suite contains:

- **65 `Fact` methods**
- **18 `Theory` methods**
- **77 `InlineData` datasets**
- **142 statically declared test cases in total**

Of those, **87 cases were added** for the routing provider.

The foundation previously declared 55 cases.

None of these numbers represents xUnit discovery output or a number of passed tests.

| Added test set | Behavior to verify under .NET |
|---|---|
| `RoutingContractTests` | Required inputs, positive versions, separation from public contracts, masked secret references |
| `RoutingConfigurationTests` | Strict JSON, duplicate properties, lists, limits, references, scopes, UTF-8 files, and masked errors |
| `DatabaseRouteResolverTests` | One application using multiple destinations, distinct scopes sharing a database, no fallback, concurrency, and immutable snapshots |
| `AuthenticationDirectoryLocatorTests` | Registered context, expected application, administrative states, and cancellation |
| `RoutingApiTests` | Server-side registration, invalid-configuration rejection, honest readiness, and absence of HTTP leakage |

These tests exercise in-memory route selection and configuration-file loading.

Even if all of them pass, they do not prove:

- SQL isolation;
- correct pool sizing;
- PostgreSQL connectivity;
- production revocation behavior;
- authentication-protocol correctness.

## Reproduction

From the solution root:

```powershell
.\scripts\verify.ps1
```

The script performs restore, Release build, and test execution, checks exit codes, and
writes the TRX result.

Preserve the complete output if a failure occurs.

Manual activation of the file-based provider is described in:

```text
ROUTING_CONFIGURATION.md
```

## Gates Still Open

The new .NET build and test suite still require a real execution.

Multi-database PostgreSQL is not connected yet.

The following areas remain absent:

- authentication;
- OIDC;
- MFA;
- persistent identity directory;
- integration with the existing RBAC engine.

The delivered bootstrap component is an internal directory locator, not OIDC client
validation or HTTP identity validation.

No production-readiness or production-security guarantee is claimed.
