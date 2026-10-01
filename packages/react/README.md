# @generic-identity/react

Reusable React UI boundary for Generic Identity.

Packs 4-6 activate providers, hooks, authorization gates, stable semantic UI primitives, shared Identity page views, a documented theme contract and optional visual component overrides. The proven Next.js administration host remains untouched until the dedicated Next.js and consumer-integration packs.

## Public surface

```text
IdentityProvider
useIdentityClient
useAuthorization
useCapability
useIdentityComponents
RequireCapability

IdentityThemeRoot
identityThemeTokenNames
IdentityButton
IdentityInput
IdentityPageFrame
IdentityPanel
IdentityTable
IdentityStatus
IdentityEmptyState

SignInPage
RecoveryPage
AccountPage
ProfilePage
UsersPage
UserDetailsPage
GroupsPage
GroupDetailsPage
PoliciesPage
PolicyDetailsPage
SessionsPage
MfaPage
SecurityPage
```

Dependency direction:

```text
@generic-identity/contracts
          ↑
@generic-identity/auth
          ↑
@generic-identity/react
```

React and React DOM are peer dependencies. This package must not depend on Next.js.

## Theme contract

Import the default theme once in the consuming application:

```ts
import "@generic-identity/react/theme.css";
```

Use `IdentityThemeRoot` or any stable ancestor class to override documented `--gi-*` custom properties:

```tsx
<IdentityThemeRoot className="consumer-identity">
  <GroupsPage groups={groups} />
</IdentityThemeRoot>
```

```css
.consumer-identity {
  --gi-background: var(--app-background);
  --gi-surface: var(--app-surface);
  --gi-text: var(--app-text);
  --gi-border: var(--app-border);
  --gi-primary: var(--app-accent);
}
```

The public styling surface is limited to documented `--gi-*` variables, stable `gi-*` classes and `data-gi-*` attributes. Consumers must not depend on undocumented DOM nesting.

## Visual component overrides

`IdentityProvider` accepts optional rendering overrides:

```tsx
<IdentityProvider
  client={client}
  components={{
    Button: AppButton,
    Input: AppInput,
    Panel: AppPanel,
    Table: AppTable,
  }}
>
  {children}
</IdentityProvider>
```

Overrides may replace presentation only. They do not own authentication, authorization, sessions, MFA, policy evaluation, TRN generation or access-context semantics.

The initial Pack 6 override contract covers the visual primitives already used by the shared pages: `Button`, `Input`, `Panel` and `Table`. Additional visual slots should be added only when a real shared component requires them.

## Page ownership

The shared page components own reusable presentation structure only. They receive already-authorized data and action slots/URLs from their consumer integration. They do not own:

- Next.js routes or middleware;
- cookie/session persistence;
- protected backend mutations;
- PostgreSQL/Redis access;
- RBAC decisions or TRN parsing;
- consumer-specific navigation or branding.

`RequireCapability` remains an UX gate only. Protected backend operations must still re-check authorization server-side.

## Existing host cleanup

No existing administration page is deleted in Pack 6. The current Next.js host still proves production behavior. Local host pages become cleanup candidates only after the future Next.js package and consumer integration have switched routes to the shared page surface and the full verification is GREEN.
