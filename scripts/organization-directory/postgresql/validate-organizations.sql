BEGIN;

DO $$
BEGIN
    IF to_regclass('identity_access.tenants') IS NULL THEN RAISE EXCEPTION 'identity_access.tenants is missing'; END IF;
    IF to_regclass('organization_directory.organizations') IS NULL THEN RAISE EXCEPTION 'organization_directory.organizations is missing'; END IF;
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'fk_organizations_tenant' AND conrelid = 'organization_directory.organizations'::regclass) THEN RAISE EXCEPTION 'Identity Access tenant foreign key is missing'; END IF;
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'fk_organizations_parent' AND conrelid = 'organization_directory.organizations'::regclass) THEN RAISE EXCEPTION 'tenant-local parent foreign key is missing'; END IF;
    IF NOT EXISTS (SELECT 1 FROM pg_trigger WHERE tgname = 'trg_organizations_reject_cycle' AND tgrelid = 'organization_directory.organizations'::regclass AND NOT tgisinternal) THEN RAISE EXCEPTION 'hierarchy cycle trigger is missing'; END IF;
END;
$$;

INSERT INTO identity_access.tenants (identity_scope_id, tenant_id, display_name, status, row_version)
VALUES
('51000000-0000-0000-0000-000000000001', '52000000-0000-0000-0000-000000000001', 'Organization Probe Tenant A', 1, 1),
('51000000-0000-0000-0000-000000000001', '52000000-0000-0000-0000-000000000002', 'Organization Probe Tenant B', 1, 1);

INSERT INTO organization_directory.organizations
(identity_scope_id, tenant_id, organization_id, organization_key, display_name, organization_type, parent_organization_id, status, row_version, created_at, updated_at)
VALUES
('51000000-0000-0000-0000-000000000001','52000000-0000-0000-0000-000000000001','53000000-0000-0000-0000-000000000001','root','Root','organization',NULL,1,1,transaction_timestamp(),transaction_timestamp()),
('51000000-0000-0000-0000-000000000001','52000000-0000-0000-0000-000000000001','53000000-0000-0000-0000-000000000002','child','Child','business','53000000-0000-0000-0000-000000000001',1,1,transaction_timestamp(),transaction_timestamp());

DO $$
BEGIN
    BEGIN
        UPDATE organization_directory.organizations SET parent_organization_id='53000000-0000-0000-0000-000000000002'
        WHERE identity_scope_id='51000000-0000-0000-0000-000000000001' AND tenant_id='52000000-0000-0000-0000-000000000001' AND organization_id='53000000-0000-0000-0000-000000000001';
        RAISE EXCEPTION 'cycle update unexpectedly succeeded';
    EXCEPTION WHEN check_violation THEN NULL; END;
END;
$$;

DO $$
BEGIN
    BEGIN
        INSERT INTO organization_directory.organizations
        (identity_scope_id, tenant_id, organization_id, organization_key, display_name, organization_type, parent_organization_id, status, row_version, created_at, updated_at)
        VALUES
        ('51000000-0000-0000-0000-000000000001','52000000-0000-0000-0000-000000000002','53000000-0000-0000-0000-000000000003','cross-tenant','Cross Tenant','business','53000000-0000-0000-0000-000000000001',1,1,transaction_timestamp(),transaction_timestamp());
        RAISE EXCEPTION 'cross-tenant parent unexpectedly succeeded';
    EXCEPTION WHEN foreign_key_violation THEN NULL; END;
END;
$$;

UPDATE organization_directory.organizations
SET display_name='Child Updated', row_version=row_version+1, updated_at=transaction_timestamp()
WHERE identity_scope_id='51000000-0000-0000-0000-000000000001' AND tenant_id='52000000-0000-0000-0000-000000000001' AND organization_id='53000000-0000-0000-0000-000000000002' AND row_version=1;

DO $$
DECLARE current_version bigint;
BEGIN
    SELECT row_version INTO current_version FROM organization_directory.organizations
    WHERE identity_scope_id='51000000-0000-0000-0000-000000000001' AND tenant_id='52000000-0000-0000-0000-000000000001' AND organization_id='53000000-0000-0000-0000-000000000002';
    IF current_version <> 2 THEN RAISE EXCEPTION 'optimistic row version validation failed'; END IF;
END;
$$;

ROLLBACK;
