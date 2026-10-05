# delivery 06 — Theme and Component Overrides Manifest

## Scope

Add the first public Generic Identity visual customization contract without changing authentication, authorization, session, MFA, RBAC, storage or consumer routing behavior.

This delivery also backfills the repository `CHANGELOG.md` with explicit incremental Shared Identity entries for the Baseline, Public Contracts, Authentication SDK, React Foundation and Shared Pages milestones and adds the Theme and Component Overrides entry. Future milestones must continue updating the changelog incrementally.

## ADDED

```text
docs/shared-identity/THEME_AND_COMPONENT_OVERRIDES.md
docs/shared-identity/validation-manifests/THEME_AND_COMPONENT_OVERRIDES.md
packages/react/src/components/IdentityButton.tsx
packages/react/src/components/IdentityInput.tsx
packages/react/src/hooks/useIdentityComponents.ts
packages/react/src/internal/IdentityVisualContext.ts
packages/react/src/theme/IdentityThemeRoot.tsx
packages/react/src/theme/default.css
packages/react/src/theme/index.ts
packages/react/src/theme/tokens.ts
packages/react/src/visual/index.ts
packages/react/src/visual/types.ts
packages/react/test/consumer-theme.tsx
scripts/shared-identity/verify-delivery-06-theme.ps1
```

## MODIFIED

```text
CHANGELOG.md
packages/README.md
packages/react/README.md
packages/react/package.json
packages/react/src/components/IdentityPanel.tsx
packages/react/src/components/IdentityTable.tsx
packages/react/src/components/index.ts
packages/react/src/hooks/index.ts
packages/react/src/index.ts
packages/react/src/pages/RecoveryPage.tsx
packages/react/src/pages/SignInPage.tsx
packages/react/src/providers/IdentityProvider.tsx
scripts/shared-identity/verify.ps1
scripts/verify.ps1
```

## MOVED

```text
NONE
```

## DELETED

```text
NONE
```

## DELETE AFTER VALIDATION

```text
NONE IN Theme and Component Overrides
```

The existing Next.js administration host remains authoritative and must not be cleaned up in this delivery. Existing host Identity pages remain future cleanup candidates only after the dedicated Next.js package and consumer integration are GREEN.

## DATABASE MIGRATIONS

```text
NONE
```

## BACKEND / RUNTIME CHANGES

```text
NONE
```

## HOST INTEGRATION CHANGES

```text
NONE
```

The existing Next.js host is not redirected to `@generic-identity/react` by Theme and Component Overrides.

## PUBLIC SURFACE CHANGES

`@generic-identity/react` is bumped from extraction version `0.1.0` to private version `0.2.0` and adds:

```text
IdentityThemeRoot
identityThemeTokenNames
useIdentityComponents
IdentityButton
IdentityInput
IdentityComponentOverrides
IdentityButtonProps
IdentityInputProps
IdentityPanelVisualProps
IdentityTableVisualProps
@generic-identity/react/theme.css
```

`IdentityProvider` adds the optional `components` property. Existing callers remain source-compatible because the property is optional.

## VISUAL OVERRIDE BOUNDARY

The initial override contract covers only primitives already used by shared pages:

```text
Button
Input
Panel
Table
```

No speculative Dialog/Tabs contract is added before a real shared component requires it.

Visual overrides may not replace security semantics.

## CHANGELOG POLICY FROM THIS delivery FORWARD

Theme and Component Overrides repairs the missing incremental Shared Identity changelog history by recording the previously qualified Shared Identity milestones in `CHANGELOG.md`.

Every subsequent delivery must add its own Shared Identity changelog entry in the same delivery. Changelog omission is considered a delivery qualification failure.

## POST-APPLY VALIDATION

```powershell
.\scripts\verify.ps1 -Configuration Release
```

Expected Theme and Component Overrides markers include:

```text
Shared Identity theme/component override source validation: GREEN
Shared Identity theme/component override typecheck: GREEN
```

The complete pre-existing repository verification must remain GREEN.
