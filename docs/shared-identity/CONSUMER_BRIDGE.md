# Generic Identity — Consumer Integration Boundary

The consumer integration boundary exists so applications can adopt Generic Identity without copying endpoint logic or security semantics into product code.

## Architecture

```text
consumer route / Server Action
        │
        v
@generic-identity/next/server
        │
        v
@generic-identity/auth categorized clients
        │
        v
proven TypeScript transport
        │
        v
Identity API
```

The integration is no longer read-only. Reusable server workflows cover the supported administration lifecycle for Directory, Organizations, Access Control, Application Security, Security Audit, Sessions, and MFA administration.

## Invariants

- authentication, authorization, RBAC and tenant routing remain server-authoritative;
- the consumer never imports the compatibility transport directly for application behavior;
- browser identifiers are requested context, not authorization context;
- mutations execute through server-only integration code;
- relation selection uses bounded server-backed lookup rather than unrestricted catalogs;
- failures remain distinguishable from authorization denial;
- consumer branding and route ownership stay outside the reusable SDK.

No second authentication, authorization, RBAC, MFA, session, or persistence engine is introduced.
