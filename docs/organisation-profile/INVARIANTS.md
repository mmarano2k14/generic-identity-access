# OrganisationProfile Invariants

Foundation freezes the following invariants.

1. `OrganisationProfile` is module-owned.
2. `Organization` remains owned by Generic Organization Directory.
3. One Organization may have zero or one OrganisationProfile initially.
4. An OrganisationProfile cannot reference empty Identity Scope, Tenant, or Organization identifiers.
5. An Organization can exist without an OrganisationProfile.
6. OrganisationProfile does not create Organization identity.
7. Organization membership remains `OrganizationMembership`; no profile-membership model is introduced.
8. OrganisationProfile does not grant authorization.
9. Published profile-template versions are immutable.
10. Template versions are positive explicit integers.
11. Domain versions are positive explicit integers.
12. Effective domain composition uses version pins, never implicit latest-version semantics.
13. `Enable` domain overrides require a version.
14. `Disable` domain overrides carry no version.
15. Duplicate domain keys are invalid inside one published/effective composition.
16. Deterministic published/effective domain lists are key ordered.
17. `RowVersion` and semantic `OrganisationProfileVersionNumber` are different concepts.
18. Provider credentials, tokens, secrets, and provider DTOs do not belong in this subsystem.
19. Integration with Organization Directory uses a narrow adapter contract rather than implementation-project dependencies.
20. No god service or god UI component may own the entire OrganisationProfile subsystem.
21. Concurrent mutable writes using the same expected `RowVersion` cannot both commit.
22. Concurrent resolution of identical effective content must not append duplicate semantic versions.
23. Equivalent effective content must reproduce the same deterministic content hash.
24. Reverting semantic content appends a new semantic version; it never rewrites historical versions.
25. Lifecycle-only changes do not create a new semantic version when effective content is unchanged.
26. Recovery grouping with external schemas does not transfer ownership of Tenant or Organization authority into OrganisationProfile.
