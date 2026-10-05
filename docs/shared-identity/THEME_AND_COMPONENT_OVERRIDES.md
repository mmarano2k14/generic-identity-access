# Shared Identity Theme and Component Overrides — Theme and Component Overrides

## Goal

Make the shared React pages visually reusable by different applications without forking page logic or changing authentication/authorization semantics.

## Theme boundary

The default stylesheet is exported as:

```text
@generic-identity/react/theme.css
```

Consumers may override documented `--gi-*` CSS variables on `IdentityThemeRoot` or any stable ancestor. The default stylesheet uses only the shared semantic `gi-*` classes and `data-gi-*` attributes introduced by the shared pages.

## Visual override boundary

`IdentityProvider` now accepts optional `components` overrides for the visual primitives already used by shared pages:

```text
Button
Input
Panel
Table
```

These overrides are rendering-only. They cannot replace authentication, authorization, sessions, MFA, policy evaluation, TRN generation or access-context behavior.

The default semantic components continue to render correctly when no `IdentityProvider` is present; presentation-only pages therefore remain usable as pure React views.

## Non-goals

Theme and Component Overrides does not:

- activate the Next.js package;
- redirect the existing Next.js administration host;
- move or delete existing host pages;
- change backend APIs, RBAC, authentication, MFA or storage;
- add application-specific branding;
- add Dialog/Tabs override contracts before a real shared component requires them.

## Validation

Run:

```powershell
.\scripts\verify.ps1 -Configuration Release
```

The repository must remain fully GREEN, including Theme and Component Overrides source validation and React typecheck.
