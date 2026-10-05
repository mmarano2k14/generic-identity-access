# Scope Type Catalog and Resource Scope Selector

## Status

Implemented administration increment. The underlying generic Scope Type catalog and PostgreSQL constraints already existed in the Resource Scope hierarchy foundation; this increment exposes the catalog in the administration UI and removes free-text Scope Type entry from Resource Scope create/edit flows.

## Existing Generic Model

`identity_access.application_scope_types` is versioned by application security model and owns generic scope metadata only:

- `scope_type_key`
- `display_name`
- optional `parent_scope_type_key`
- `can_attach_to_tenant`

`identity_access.resource_scopes` references the registered type through `(identity_scope_id, application_key, scope_model_version, scope_type_key)`. PostgreSQL hierarchy validation continues to enforce parent type, model-version consistency, tenant-local parent ownership, and active-parent requirements.

## Administration Flow

The Security Models workspace now displays the Scope Type catalog for the selected model and can register a new type through the existing protected Scope Type API.

Resource Scope create/edit now uses:

```text
Security model version
        ↓
GET registered scope types
        ↓
Scope type selector
        ↓
Resource Scope mutation
```

The browser calls only the local protected Next.js route. Identity Access credentials and authorization decisions remain server-side.

## Consumer Application Boundary

A consumer application may register values such as `ecommerce` or `restaurant` for validation, but the generic Identity & Access core does not know their business meaning or Domain composition.

```text
Identity & Access
Tenant: Urban Group
├── ecommerce  -> Urban Flowers
└── restaurant -> Urban Cafe

consumer application
├── ecommerce  -> Domain composition
└── restaurant -> Domain composition
```

This example does not, by itself, finalize consumer application's complete Resource Scope architecture.

## No RBAC Change

Scope Type registration is not a permission grant. Managed Policies, capability coordinates, TRN materialization, and external RBAC evaluation remain unchanged.
