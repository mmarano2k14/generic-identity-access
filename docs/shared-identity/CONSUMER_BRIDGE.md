# Shared Identity Consumer Bridge — Consumer administration bridge

This closure is additive and exists solely to make the already-qualified shared
Identity pages consumable by a real Next.js application without copying legacy
administration endpoint logic into the consumer.

It adds a read-focused administration facade to `@generic-identity/auth`, server
helpers to construct trusted administration contexts in `@generic-identity/next`,
and runtime source export conditions required for local `file:` package
consumption during pre-publication integration.

No authentication, authorization, RBAC, TRN, MFA or persistence semantics are
reimplemented. The proven legacy TypeScript client remains the runtime bridge.

No files are moved or deleted.
