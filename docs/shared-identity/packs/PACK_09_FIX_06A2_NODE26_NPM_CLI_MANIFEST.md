# Pack 9 Fix 06A.2 - Node 26 npm CLI spawning

**Destination:** `identity-access` repository only.

## ADDED
- `docs/shared-identity/packs/PACK_09_FIX_06A2_NODE26_NPM_CLI_MANIFEST.md`

## MODIFIED
- `scripts/shared-identity/build-local-consumer-packages.mjs`
- `scripts/shared-identity/verify-pack-09-local-package-artifacts.ps1`
- `CHANGELOG.md`

## MOVED
NONE

## DELETED
NONE

## CLEANUP
NONE

## DATABASE MIGRATIONS
NONE

## Purpose
Avoid direct Windows `.cmd` execution from Node.js child processes. Node.js 26 can reject `spawnSync npm.cmd` with `EINVAL`. The builder now invokes the npm JavaScript CLI through the current Node executable, using `npm_execpath` first and installation-local fallbacks.
