# OrganisationProfile Domain Registry Boundary

## Ownership

OrganisationProfile does not own the Domain Registry.

The only Domain Composition integration contract is:

```text
IDomainRegistryReader
```

which resolves:

```text
DomainKey
+
DomainVersion
```

to minimal registry state.

No Domain Registry persistence, package loading, domain business rules, or domain implementation code is added to this module.

## Registry states

Domain Composition distinguishes:

```text
Published
Retired
```

The difference is intentional.

### Published

May be selected for:

```text
new template publication
new or changed Enable override
```

### Retired

Cannot be newly selected.

It remains resolvable for historical profile composition and replay.

This prevents retiring a domain package from invalidating already published profile history.

## Selection policy

The validator exposes:

```text
RequireSelectableAsync
```

and requires every exact domain version to be:

```text
status = Published
```

## Resolution policy

The validator also exposes:

```text
RequireResolvableAsync
```

and accepts:

```text
Published
Retired
```

Unknown versions always fail.

## Version discipline

The registry boundary never accepts:

```text
latest
compatible-latest
any
*
```

OrganisationProfile always asks for one exact:

```text
DomainKey@DomainVersion
```

This preserves deterministic effective profile history.
