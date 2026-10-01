# Pack 06 — Theme and Component Overrides Manifest

## Scope

Add the first public Generic Identity visual customization contract without changing authentication, authorization, session, MFA, RBAC, storage or consumer routing behavior.

This pack also backfills the repository `CHANGELOG.md` with explicit incremental Shared Identity entries for Packs 1 through 5 and adds the Pack 6 entry. Future packs must continue updating the changelog incrementally.

## ADDED

```text
docs/shared-identity/PACK_06_THEME_AND_COMPONENT_OVERRIDES.md
docs/shared-identity/packs/PACK_06_THEME_AND_COMPONENT_OVERRIDES_MANIFEST.md
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
scripts/shared-identity/verify-pack-06-theme.ps1
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
NONE IN PACK 6
```

The existing Next.js administration host remains authoritative and must not be cleaned up in this pack. Existing host Identity pages remain future cleanup candidates only after the dedicated Next.js package and consumer integration are GREEN.

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

The existing Next.js host is not redirected to `@generic-identity/react` by Pack 6.

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

## CHANGELOG POLICY FROM THIS PACK FORWARD

Pack 6 repairs the missing incremental Shared Identity changelog history by recording Packs 1-6 in `CHANGELOG.md`.

Every subsequent pack must add its own Shared Identity changelog entry in the same delivery. Changelog omission is considered a pack qualification failure.

## POST-APPLY VALIDATION

```powershell
.\scripts\verify.ps1 -Configuration Release
```

Expected Pack 6 markers include:

```text
Shared Identity Pack 6 theme/component override source validation: GREEN
Shared Identity Pack 6 theme/component override typecheck: GREEN
```

The complete pre-existing repository verification must remain GREEN.
