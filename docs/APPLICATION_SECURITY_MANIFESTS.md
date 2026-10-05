# Application Security Manifests

> For an end-to-end consumer onboarding tutorial, including initial local administrator authority and SDK integration, see [`CONSUMER_APPLICATION_ONBOARDING.md`](CONSUMER_APPLICATION_ONBOARDING.md).

## Purpose

An application security manifest is the project-owned declaration of the security vocabulary that an application supports. It removes the need to hardcode capability catalogs in C# constants or to let administrators invent arbitrary capability coordinates in the administration UI.

The design preserves the existing external RBAC contract:

```text
CapabilityKey = resource / feature / action
RBAC context  = project + namespace
TRN           = trn:{project}:{namespace}:{resource}:{feature}:{action}
```

`Project` and `namespace` are execution-context coordinates. They are intentionally not added to `CapabilityKey`.

## Authoring source and registered projection

The consuming project owns a JSON manifest. A generic example is available at:

```text
config/application-security-manifest.example.json
```

The reusable administration host follows the same rule for its own capability catalog:

```text
config/identity-access-admin-security-manifest.json
```

The local bootstrap reads that JSON file; it does not maintain a parallel hardcoded feature/action list.

Example:

```json
{
  "schemaVersion": 1,
  "applicationKey": "sample-app",
  "modelVersion": 1,
  "rbac": {
    "project": "sample-project",
    "namespaces": ["crm"]
  },
  "resources": [
    {
      "name": "billing",
      "features": [
        {
          "name": "invoice",
          "actions": [
            { "name": "read", "displayName": "Read invoices" },
            { "name": "refund", "displayName": "Refund invoices" }
          ]
        }
      ]
    }
  ]
}
```

The JSON file is the application authoring source. PostgreSQL stores the registered, normalized, immutable projection used by Identity Access administration. The administration UI may register a complete project-owned `.json` file, but it does not expose fields for inventing or editing individual resource, feature, action, project, namespace, or model-version coordinates.

This separation has two purposes:

- application source control defines what the software supports;
- Identity Access durable state records exactly which model versions were registered and which versions policies reference.

Policies, statements, group bindings, memberships, and resource scopes remain dynamic database-owned administration state. The browser does not become an authoring authority for application capabilities.

## Registration

A manifest can be registered through the protected Security Models administration workspace by selecting the complete project-owned `.json` file. The browser does not decompose the manifest into editable capability fields; a server-only parser validates the bounded JSON document and submits it through the same typed registration contract.

The underlying API is:

```text
PUT /api/v1/identity-scopes/{identityScopeId}/applications/{applicationKey}/security-models/{modelVersion}
```

The route application key and model version must match the manifest body.

Registration normalizes and validates:

- schema version;
- application key and model version;
- RBAC project;
- one or more concrete RBAC namespaces;
- unique concrete `resource / feature / action` capabilities;
- display metadata.

A deterministic SHA-256 fingerprint is computed over the normalized semantic model.

Registration semantics are:

```text
same application + same model version + same normalized fingerprint
    -> idempotent

same application + same model version + different normalized fingerprint
    -> conflict
```

A legacy security-model row that has no manifest registration is not silently adopted through this public registration contract. A new manifest-backed version must be registered explicitly.

### Initial authorization bootstrap

Manifest registration is an administration write operation; it is never an unauthenticated bootstrap endpoint. A brand-new application/identity scope therefore needs an already trusted provisioning path for its initial administration authority, in the same way that other privileged Identity Access administration requires a root-of-trust bootstrap. Local development uses the dedicated development bootstrap script. Production provisioning must establish that initial authority through controlled deployment/onboarding before the manifest registration endpoint is used.

## PostgreSQL projection

Migration `0021_application_security_manifest_catalog.sql` adds:

```text
application_security_model_registrations
application_security_namespaces
```

Existing tables continue to own:

```text
application_security_models
application_capabilities
permission_policies
policy_statements
group_policy_bindings
```

`application_capabilities` remains a concrete capability catalog:

```text
capability_resource
capability_feature
capability_action
```

The manifest registration tables add the RBAC context and provenance:

```text
manifest_schema_version
rbac_project
manifest_sha256
rbac_namespace(s)
```

The physical database placement remains controlled by the existing trusted multi-database route resolver. A manifest never supplies a connection string or database destination.

## Policy Builder

The administration policy builder reads registered security models and presents their exact capabilities. New statements select a registered catalog entry rather than accepting free-text capability coordinates.

Conceptually:

```text
Project JSON manifest
        |
        v
registered security model version
        |
        v
resource / feature / action catalog
        |
        v
Policy Builder
        |
        v
PolicyStatement pinned to modelVersion
        |
        v
existing TRN compiler / external RBAC
```

The browser selection is not a trust boundary. The backend continues to validate persisted policy patterns against the pinned security model.

Existing wildcard policy contracts are preserved. Wildcard matching and final allow/deny evaluation remain external-RBAC responsibilities; the Policy Builder does not implement a second wildcard or authorization engine.

## TRN previews

The Security Models administration workspace displays registered immutable projections and may display descriptive previews such as:

```text
trn:sample-project:crm:billing:invoice:read
```

A preview demonstrates how registered context and capability coordinates compose. It is not proof that a user is authorized.

Runtime authorization continues to use the external RBAC execution context and calls shaped as:

```text
IsAllowed(resource, feature, action)
```

with project and current namespace supplied by the execution context.

## Security invariants

- Application capability catalogs are not hardcoded into reusable Identity Access source.
- The browser cannot invent individual application capability coordinates; registration accepts only a complete project-owned manifest document.
- Reusing an immutable model version for different semantics is rejected.
- `CapabilityKey` remains `resource / feature / action`.
- RBAC project and namespace remain context, not capability segments.
- TRNs remain a derived wire representation.
- Policy persistence is not an authorization verdict.
- The administration UI never becomes an RBAC evaluator.
- Raw credentials, tokens, secret references, connection strings, and RBAC internal stores are not part of manifest responses.
