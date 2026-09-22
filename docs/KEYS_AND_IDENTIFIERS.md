# Keys and Identifier Contracts

Identity Access uses strongly typed value objects for security-relevant identifiers.
Shared syntax mechanics are centralized where the grammar is genuinely identical, while
semantic types remain distinct.

## Canonical Lowercase Slugs

`ApplicationKey` and `ResourceScopeTypeKey` use the same canonical slug grammar:

- 1 to 64 characters;
- first character is a lowercase ASCII letter;
- remaining characters are lowercase ASCII letters, digits, or hyphens;
- no trimming or case normalization is applied.

Non-canonical external input is rejected rather than silently changed.

## Capability Segments

`CapabilityKey` uses the same lowercase slug character grammar but preserves its existing
wire-contract behavior by trimming and lowercasing each concrete segment.

`CapabilityPattern` applies the same normalization and additionally permits a whole-segment
`*` wildcard. Wildcard shape validation remains separate from authorization evaluation.

Identity Access does not evaluate wildcard authorization semantics. The external RBAC
engine remains the authorization authority.

## Authentication Context Keys

Authentication context keys intentionally use the same grammar as `ApplicationKey`.
Validation therefore reuses the application-key contract rather than maintaining a
separate copy of the syntax rules.

The authentication context remains a separate semantic concept; sharing syntax does not
make it an application identifier or an authorization credential.

## RBAC Context Segments

RBAC project and namespace values use `RbacContextKey`.

The value is:

- trimmed;
- lowercased invariantly;
- limited to 128 characters;
- rejected when it contains `:` or `*`.

The same contract is used by authorization requests and TRN materialization, avoiding
different acceptance rules at different integration boundaries.

## Design Rule

Genericize syntax mechanics, not domain semantics.

Types such as `ApplicationKey`, `ResourceScopeTypeKey`, `CapabilityKey`, and
`RbacContextKey` remain distinct even when some of their validation rules overlap. Their
types communicate different security and domain meanings at compile time.
