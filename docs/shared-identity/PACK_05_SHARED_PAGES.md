# Shared Identity Pack 5 — Shared React Pages

Pack 5 adds framework-neutral, presentation-only shared Identity page views to `@generic-identity/react`.

## Scope

The package now exposes reusable pages for sign-in, recovery, account/profile, users, groups, managed policies, sessions, MFA and security composition. Pages accept passive contracts and consumer-supplied action slots/URLs; they do not fetch data, persist sessions, own routes or perform protected backend mutations.

## Boundary

```text
contracts -> auth -> react pages -> future Next.js integration
```

The existing Next.js administration host is deliberately not switched in this pack. This prevents a page extraction from becoming a runtime migration before the Next.js adapter exists.

## Stable semantic DOM

Pack 5 introduces `gi-*` class names and `data-gi-*` attributes as the semantic styling surface. Visual tokens, CSS variables and component overrides are owned by Pack 6.

## Cleanup policy

There are no file moves or deletions in Pack 5. Existing host pages/components are future cleanup candidates only after route integration has switched to the shared packages and complete build/test/browser qualification is GREEN.
