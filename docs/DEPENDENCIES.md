# Dependencies and toolchain

Reference date: 2026-09-21. Public package metadata was inspected; the NuGet dependency
graph could not be restored in the generation environment. Listed versions are not a
claim that a full dependency security audit or build has passed.

| Component | Selection | Scope |
|---|---|---|
| .NET / ASP.NET Core | `net10.0` | Stable LTS target, no .NET 11 preview dependency |
| SDK selection | 10.0.100 minimum, `latestFeature`, prereleases disabled | Accepts newer installed stable 10.0 SDKs; not an instruction to install the old minimum |
| Microsoft.AspNetCore.Mvc.Testing | 10.0.12 | API test host only |
| Microsoft.NET.Test.Sdk | 18.10.0 | Test execution |
| xunit.v3 | 3.2.2 | Test framework; executable test project |
| xunit.runner.visualstudio | 3.1.5 | VSTest adapter |
| TypeScript | 5.8.3 | Exact development compiler version; available and executed locally |
| Node.js | Capabilities required; no upper version restriction | Test execution and optional server-side client |
| PostgreSQL / Npgsql / EF Core | Not installed or version-locked in this increment | Real persistence implementation pending |
| OpenIddict / Identity / MFA | Not installed in this increment | Authentication integration pending |

The client uses standard `fetch`, `URL` and `AbortController` capabilities and does not
load `.ts` files through an experimental Node loader. TypeScript is compiled to JavaScript
before tests. No Node downgrade is required by package metadata. Only Node 22.16.0 was
actually exercised here; other versions are not claimed as tested.

The Next.js example requires `server-only` in the consuming application. No React or
Next.js dependency is embedded in the reusable core client.

No license is assigned to the new project by this increment. Third-party packages retain
their own licensing; no third-party binaries are redistributed in the source archive.
Direct package versions are pinned. NuGet/npm lockfiles are not fabricated; restoration
and dependency locking can be completed on the development machine with registry access.

## Primary sources consulted

.NET 10 target and support:
`https://dotnet.microsoft.com/en-us/download/dotnet/10.0`

SDK selection:
`https://learn.microsoft.com/en-us/dotnet/core/tools/global-json`

API test package:
`https://www.nuget.org/packages/Microsoft.AspNetCore.Mvc.Testing/10.0.12`

Test SDK:
`https://www.nuget.org/packages/Microsoft.NET.Test.Sdk/18.10.0`

xUnit v3:
`https://www.nuget.org/packages/xunit.v3/3.2.2`

VSTest adapter:
`https://www.nuget.org/packages/xunit.runner.visualstudio/3.1.5`

Next.js server/client boundary:
`https://nextjs.org/docs/app/getting-started/server-and-client-components`
