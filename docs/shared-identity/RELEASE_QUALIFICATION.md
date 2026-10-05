# Shared Identity Release Qualification — Release Qualification

Release Qualification freezes the first stable package boundary after the shared contracts, auth SDK, React UI, Next.js integration, consumer integration, server-side authorization and Webpack/Turbopack qualification are GREEN.

The four consumer-facing packages are versioned at `1.0.0`. The existing `@identity-access/client` remains a transitional transport implementation dependency at `0.26.0`; consumers should continue importing the `@generic-identity/*` surfaces rather than the compatibility transport package directly.

Release qualification is deliberately separate from registry publication. `scripts/shared-identity/qualify-release-packages.mjs` builds deterministic local tarballs under `artifacts/shared-identity-release`, verifies checksums and packed manifests, rejects local-link dependencies and forbidden files, and writes `release-manifest.json` with status `qualified-not-published`.

No Release Qualification step publishes packages, changes authentication/RBAC semantics, migrates a database, moves source files or deletes source files.
