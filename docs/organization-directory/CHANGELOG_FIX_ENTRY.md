# CHANGELOG entry to merge

## 0.6.1 — ResourceScope-link capability test alignment

### Fixed

- Updated the Organization ResourceScope-link API architecture test to require the dedicated `organization-scope-link` administration capability introduced in 0.6.0.
- Added a regression assertion preventing the controller from falling back to the broader `organization` capability.
