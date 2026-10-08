# Generic Identity — Release Qualification

Release Qualification validates the distributable Generic Identity package boundary independently from registry publication.

## Current package family

```text
@generic-identity/contracts  1.5.0
@generic-identity/auth       1.5.0
@generic-identity/react      1.5.0
@generic-identity/next       1.5.0
```

The transitional transport dependency remains:

```text
@identity-access/client      0.26.0
```

Consumers should use `@generic-identity/*` public surfaces rather than importing the transport bridge directly.

## Qualification scope

Release qualification validates:

```text
package version alignment
packed manifest correctness
package dependency boundaries
forbidden-content rules
checksums
public subpath exports
self-contained consumer installation
React / Next.js peer dependency constraints
consumer integration compilation
```

Local release artifact generation is a qualification mechanism only; it does not publish packages to a registry.

## Administration workflow qualification

The current `1.5.0` source line also carries reusable administration workflows for Directory, Organizations, Access Control, Application Security, Security Audit, Sessions, and MFA administration. These workflows are validated by their dedicated source/behavior gates in addition to the package release checks.

MFA administration remains pending dedicated live functional acceptance; that operational qualification state does not alter the package version or create a speculative backend API.
