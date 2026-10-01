# Shared Identity Integration — Pack 6 Fix 02 — Windows PowerShell 5.1 parser compatibility

## Purpose

Repair the Pack 6 verification script only. No runtime or product behavior changes.

## Modified

- `scripts/shared-identity/verify-pack-06-theme.ps1`
- `CHANGELOG.md`

## Added

- `docs/shared-identity/packs/PACK_06_FIX_02_POWERSHELL_51_PARSER_MANIFEST.md`

## Moved

NONE

## Deleted

NONE

## Delete after validation

NONE

## Database migrations

NONE

## Runtime changes

NONE

## Fix details

- Replaced invalid C-style `\"` quote escaping in PowerShell string literals with PowerShell-safe single-quoted literals.
- Removed non-ASCII em-dash literals from the `.ps1` source and changed changelog validation to ASCII-only regex matching.
- Replaced direct `Get-Content -Raw` JSON loading with `System.IO.File.ReadAllText` for consistency with the other Windows PowerShell compatibility fixes.
- Preserved the Pack 6 package version gate (`>= 0.2.0`).

## Validation

Run:

```powershell
.\scripts\verify.ps1 -Configuration Release
```

Expected Pack 6 source gate:

```text
Shared Identity Pack 6 theme/component override source validation: GREEN
```
