BEGIN;

DO $$
BEGIN
    IF to_regclass('organization_directory.organization_memberships') IS NULL THEN
        RAISE EXCEPTION 'organization_directory.organization_memberships is missing';
    END IF;

    IF NOT EXISTS
    (
        SELECT 1
        FROM pg_constraint
        WHERE conname = 'fk_organization_memberships_organization'
          AND conrelid = 'organization_directory.organization_memberships'::regclass
    ) THEN
        RAISE EXCEPTION 'organization membership organization FK is missing';
    END IF;

    IF NOT EXISTS
    (
        SELECT 1
        FROM pg_constraint
        WHERE conname = 'fk_organization_memberships_tenant_membership'
          AND conrelid = 'organization_directory.organization_memberships'::regclass
    ) THEN
        RAISE EXCEPTION 'organization membership tenant-membership FK is missing';
    END IF;
END;
$$;

INSERT INTO identity_access.users
(identity_scope_id, user_id, display_name, status, row_version)
VALUES
(
    '81000000-0000-0000-0000-000000000001',
    '82000000-0000-0000-0000-000000000001',
    'Organization Membership Probe User',
    1,
    1
);

INSERT INTO identity_access.tenants
(identity_scope_id, tenant_id, display_name, status, row_version)
VALUES
(
    '81000000-0000-0000-0000-000000000001',
    '83000000-0000-0000-0000-000000000001',
    'Organization Membership Probe Tenant',
    1,
    1
);

INSERT INTO identity_access.tenant_memberships
(identity_scope_id, membership_id, tenant_id, user_id, status, row_version)
VALUES
(
    '81000000-0000-0000-0000-000000000001',
    '84000000-0000-0000-0000-000000000001',
    '83000000-0000-0000-0000-000000000001',
    '82000000-0000-0000-0000-000000000001',
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
    '81000000-0000-0000-0000-000000000001',
    '83000000-0000-0000-0000-000000000001',
    '85000000-0000-0000-0000-000000000001',
    'probe-organization',
    'Probe Organization',
    'business',
    NULL,
    1,
    1,
    transaction_timestamp(),
    transaction_timestamp()
);

INSERT INTO organization_directory.organization_memberships
(
    identity_scope_id,
    tenant_id,
    organization_id,
    tenant_membership_id,
    status,
    row_version,
    created_at,
    updated_at
)
VALUES
(
    '81000000-0000-0000-0000-000000000001',
    '83000000-0000-0000-0000-000000000001',
    '85000000-0000-0000-0000-000000000001',
    '84000000-0000-0000-0000-000000000001',
    1,
    1,
    transaction_timestamp(),
    transaction_timestamp()
);

UPDATE organization_directory.organization_memberships
SET status = 2,
    row_version = row_version + 1,
    updated_at = transaction_timestamp()
WHERE identity_scope_id = '81000000-0000-0000-0000-000000000001'
  AND tenant_id = '83000000-0000-0000-0000-000000000001'
  AND organization_id = '85000000-0000-0000-0000-000000000001'
  AND tenant_membership_id = '84000000-0000-0000-0000-000000000001'
  AND row_version = 1;

DO $$
DECLARE
    durable_status smallint;
    durable_version bigint;
BEGIN
    SELECT status, row_version
    INTO durable_status, durable_version
    FROM organization_directory.organization_memberships
    WHERE identity_scope_id = '81000000-0000-0000-0000-000000000001'
      AND tenant_id = '83000000-0000-0000-0000-000000000001'
      AND organization_id = '85000000-0000-0000-0000-000000000001'
      AND tenant_membership_id = '84000000-0000-0000-0000-000000000001';

    IF durable_status <> 2 OR durable_version <> 2 THEN
        RAISE EXCEPTION 'organization membership lifecycle/concurrency validation failed';
    END IF;
END;
$$;

DO $$
BEGIN
    BEGIN
        INSERT INTO organization_directory.organization_memberships
        (
            identity_scope_id,
            tenant_id,
            organization_id,
            tenant_membership_id,
            status,
            row_version,
            created_at,
            updated_at
        )
        VALUES
        (
            '81000000-0000-0000-0000-000000000001',
            '83000000-0000-0000-0000-000000000001',
            '85000000-0000-0000-0000-000000000001',
            '84000000-0000-0000-0000-000000000099',
            1,
            1,
            transaction_timestamp(),
            transaction_timestamp()
        );

        RAISE EXCEPTION 'missing tenant membership unexpectedly succeeded';
    EXCEPTION
        WHEN foreign_key_violation THEN NULL;
    END;
END;
$$;

ROLLBACK;
