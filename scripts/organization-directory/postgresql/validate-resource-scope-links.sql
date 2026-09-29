BEGIN;

DO $$
BEGIN
    IF to_regclass('organization_directory.organization_resource_scope_links') IS NULL THEN
        RAISE EXCEPTION 'organization_directory.organization_resource_scope_links is missing';
    END IF;

    IF NOT EXISTS
    (
        SELECT 1
        FROM pg_constraint
        WHERE conname = 'fk_organization_resource_scope_links_organization'
          AND conrelid = 'organization_directory.organization_resource_scope_links'::regclass
    ) THEN
        RAISE EXCEPTION 'Organization ResourceScope-link organization FK is missing';
    END IF;

    IF NOT EXISTS
    (
        SELECT 1
        FROM pg_constraint
        WHERE conname = 'fk_organization_resource_scope_links_resource_scope'
          AND conrelid = 'organization_directory.organization_resource_scope_links'::regclass
    ) THEN
        RAISE EXCEPTION 'Organization ResourceScope-link ResourceScope FK is missing';
    END IF;
END;
$$;

INSERT INTO identity_access.tenants
(identity_scope_id, tenant_id, display_name, status, row_version)
VALUES
(
    'a1000000-0000-0000-0000-000000000001',
    'a2000000-0000-0000-0000-000000000001',
    'Organization Scope-Link Probe Tenant',
    1,
    1
);

INSERT INTO identity_access.application_security_models
(identity_scope_id, application_key, model_version)
VALUES
(
    'a1000000-0000-0000-0000-000000000001',
    'organization-probe',
    1
);

INSERT INTO identity_access.application_scope_types
(
    identity_scope_id,
    application_key,
    model_version,
    scope_type_key,
    display_name,
    parent_scope_type_key,
    can_attach_to_tenant
)
VALUES
(
    'a1000000-0000-0000-0000-000000000001',
    'organization-probe',
    1,
    'organization',
    'Organization',
    NULL,
    TRUE
);

INSERT INTO identity_access.resource_scopes
(
    identity_scope_id,
    tenant_id,
    application_key,
    resource_scope_id,
    scope_model_version,
    scope_type_key,
    external_resource_id,
    display_name,
    parent_resource_scope_id,
    status,
    row_version
)
VALUES
(
    'a1000000-0000-0000-0000-000000000001',
    'a2000000-0000-0000-0000-000000000001',
    'organization-probe',
    'a3000000-0000-0000-0000-000000000001',
    1,
    'organization',
    'urban-flower',
    'Urban Flower',
    NULL,
    1,
    1
),
(
    'a1000000-0000-0000-0000-000000000001',
    'a2000000-0000-0000-0000-000000000001',
    'organization-probe',
    'a3000000-0000-0000-0000-000000000002',
    1,
    'organization',
    'urban-cafe',
    'Urban Cafe',
    NULL,
    1,
    1
);

INSERT INTO organization_directory.organizations
(
    identity_scope_id,
    tenant_id,
    organization_id,
    organization_key,
    display_name,
    organization_type,
    parent_organization_id,
    status,
    row_version,
    created_at,
    updated_at
)
VALUES
(
    'a1000000-0000-0000-0000-000000000001',
    'a2000000-0000-0000-0000-000000000001',
    'a4000000-0000-0000-0000-000000000001',
    'urban-flower',
    'Urban Flower',
    'business',
    NULL,
    1,
    1,
    transaction_timestamp(),
    transaction_timestamp()
),
(
    'a1000000-0000-0000-0000-000000000001',
    'a2000000-0000-0000-0000-000000000001',
    'a4000000-0000-0000-0000-000000000002',
    'urban-cafe',
    'Urban Cafe',
    'business',
    NULL,
    1,
    1,
    transaction_timestamp(),
    transaction_timestamp()
);

INSERT INTO organization_directory.organization_resource_scope_links
(
    identity_scope_id,
    tenant_id,
    organization_id,
    application_key,
    resource_scope_id,
    status,
    row_version,
    created_at,
    updated_at
)
VALUES
(
    'a1000000-0000-0000-0000-000000000001',
    'a2000000-0000-0000-0000-000000000001',
    'a4000000-0000-0000-0000-000000000001',
    'organization-probe',
    'a3000000-0000-0000-0000-000000000001',
    1,
    1,
    transaction_timestamp(),
    transaction_timestamp()
);

UPDATE organization_directory.organization_resource_scope_links
SET resource_scope_id = 'a3000000-0000-0000-0000-000000000002',
    row_version = row_version + 1,
    updated_at = transaction_timestamp()
WHERE identity_scope_id = 'a1000000-0000-0000-0000-000000000001'
  AND tenant_id = 'a2000000-0000-0000-0000-000000000001'
  AND organization_id = 'a4000000-0000-0000-0000-000000000001'
  AND application_key = 'organization-probe'
  AND row_version = 1;

DO $$
DECLARE
    durable_scope uuid;
    durable_version bigint;
BEGIN
    SELECT resource_scope_id, row_version
    INTO durable_scope, durable_version
    FROM organization_directory.organization_resource_scope_links
    WHERE identity_scope_id = 'a1000000-0000-0000-0000-000000000001'
      AND tenant_id = 'a2000000-0000-0000-0000-000000000001'
      AND organization_id = 'a4000000-0000-0000-0000-000000000001'
      AND application_key = 'organization-probe';

    IF durable_scope <> 'a3000000-0000-0000-0000-000000000002'::uuid
       OR durable_version <> 2 THEN
        RAISE EXCEPTION 'Organization ResourceScope relink/concurrency validation failed';
    END IF;
END;
$$;

DO $$
BEGIN
    BEGIN
        INSERT INTO organization_directory.organization_resource_scope_links
        (
            identity_scope_id,
            tenant_id,
            organization_id,
            application_key,
            resource_scope_id,
            status,
            row_version,
            created_at,
            updated_at
        )
        VALUES
        (
            'a1000000-0000-0000-0000-000000000001',
            'a2000000-0000-0000-0000-000000000001',
            'a4000000-0000-0000-0000-000000000002',
            'organization-probe',
            'a3000000-0000-0000-0000-000000000002',
            1,
            1,
            transaction_timestamp(),
            transaction_timestamp()
        );

        RAISE EXCEPTION 'duplicate ResourceScope mapping unexpectedly succeeded';
    EXCEPTION
        WHEN unique_violation THEN NULL;
    END;
END;
$$;

DO $$
BEGIN
    BEGIN
        INSERT INTO organization_directory.organization_resource_scope_links
        (
            identity_scope_id,
            tenant_id,
            organization_id,
            application_key,
            resource_scope_id,
            status,
            row_version,
            created_at,
            updated_at
        )
        VALUES
        (
            'a1000000-0000-0000-0000-000000000001',
            'a2000000-0000-0000-0000-000000000001',
            'a4000000-0000-0000-0000-000000000002',
            'organization-probe',
            'a3000000-0000-0000-0000-000000000099',
            1,
            1,
            transaction_timestamp(),
            transaction_timestamp()
        );

        RAISE EXCEPTION 'missing ResourceScope unexpectedly succeeded';
    EXCEPTION
        WHEN foreign_key_violation THEN NULL;
    END;
END;
$$;

ROLLBACK;
