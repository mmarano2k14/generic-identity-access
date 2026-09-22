# Health and Runtime Diagnostics

Identity Access derives diagnostics from the services actually configured in the running
host. Version and readiness metadata are not hardcoded.

## Liveness

```text
GET /health/live
```

Liveness is backed by the ASP.NET Core health-check subsystem and the `live` health-check
tag.

A successful response indicates that the process can serve the request. It does not imply
that persistence, authentication, or administration authorization are configured.

## Readiness

```text
GET /health/ready
```

Readiness is backed by the ASP.NET Core health-check subsystem and the `ready` tag.

The current blockers are derived from the host service graph:

```text
database-routing
postgresql-persistence
authentication
administration-authorization
```

A blocker is present only when the corresponding server capability is unavailable.

When no blockers remain, readiness returns HTTP 200 with:

```text
ready = true
stage = operational
```

Otherwise readiness returns HTTP 503 with:

```text
ready = false
stage = configuration
```

Readiness describes server capability availability. It is not a tenant-specific database
probe and does not enumerate configured database destinations or secret references.

## System Information

```text
GET /api/v1/system/info
```

The response reports:

- service identifier;
- API version;
- module version;
- runtime stage;
- storage provider;
- database-routing configuration state;
- persistence configuration state;
- authentication configuration state;
- administration-authorization configuration state.

The module version is derived from the running API assembly's informational version. It is
not duplicated as a hardcoded diagnostic constant.

## Information Disclosure

Diagnostic responses do not expose:

- route destination names;
- identity scopes;
- authentication context registrations;
- secret references;
- connection strings;
- tokens;
- credentials.

Readiness and service information expose capability state only.
