# OrganisationProfile Namespace and Aggregate Naming Rule

The module intentionally uses:

```text
namespace OrganisationProfile.*
```

and also exposes the aggregate type:

```text
OrganisationProfile.Domain.OrganisationProfile
```

Inside another `OrganisationProfile.*` namespace, unqualified `OrganisationProfile` can resolve to the root namespace rather than the aggregate type.

Executable/adaptor code inside the module should therefore use the explicit alias:

```csharp
using OrganisationProfileAggregate =
    global::OrganisationProfile.Domain.OrganisationProfile;
```

and then:

```csharp
OrganisationProfileAggregate.Create(...);
new OrganisationProfileAggregate(...);
```

This preserves the selected public module naming without renaming the domain aggregate.
