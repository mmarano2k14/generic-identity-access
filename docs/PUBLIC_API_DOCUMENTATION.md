# Public API Documentation

Production assemblies generate XML documentation files so IDEs and generated package
documentation can expose contract information.

XML comments are a repository quality standard, but missing XML documentation is not a
compiler or build blocker. The repository intentionally suppresses `CS1591` in both
production and test projects.

## Production Code

Public and protected production contracts should document:

- contract intent;
- security boundaries;
- externally observable behavior;
- failure semantics;
- concurrency guarantees;
- cancellation behavior where relevant.

Internal and private implementation details may also be documented when the behavior is
non-obvious or security-sensitive.

## Tests

Test code is not part of the supported product API, so XML documentation is not required.
Comments may still be added to test fixtures, architecture tests, helpers, and complex test
cases when they make intent clearer.

## Build Behavior

Production projects generate XML documentation files.

Test projects do not generate XML documentation files.

`CS1591` is suppressed for both categories. Documentation quality is maintained through
code review and repository conventions rather than compiler enforcement.

## Style

Comments should explain why a contract exists and what it guarantees. They should not
repeat identifier names mechanically or contain development history, internal planning
notes, or temporary development context.
