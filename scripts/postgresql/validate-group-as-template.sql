BEGIN;

DO $$
BEGIN
    IF to_regclass('identity_access.group_templates') IS NOT NULL THEN
        RAISE EXCEPTION 'retired group_templates table still exists';
    END IF;

    IF NOT EXISTS
    (
        SELECT 1
        FROM information_schema.columns
        WHERE table_schema = 'identity_access'
          AND table_name = 'user_groups'
          AND column_name = 'is_template'
          AND data_type = 'boolean'
          AND is_nullable = 'NO'
    ) THEN
        RAISE EXCEPTION 'user_groups.is_template is missing or nullable';
    END IF;

    IF EXISTS
    (
        SELECT 1
        FROM information_schema.columns
        WHERE table_schema = 'identity_access'
          AND table_name = 'user_groups'
          AND column_name IN ('origin', 'template_id')
    ) THEN
        RAISE EXCEPTION 'retired user_groups provenance columns still exist';
    END IF;
END
$$;

INSERT INTO identity_access.tenants(identity_scope_id, tenant_id, display_name, status)
VALUES
('49aaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa', '49bbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb', 'Template Source Tenant', 1),
('49aaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa', '49bbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbc', 'Template Target Tenant', 1);

INSERT INTO identity_access.user_groups
(identity_scope_id, tenant_id, application_key, group_id, display_name, status, is_template)
VALUES
('49aaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa', '49bbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb', 'admin-web',
 '49cccccc-cccc-cccc-cccc-cccccccccccc', 'Reusable Finance Administrators', 1, TRUE),
('49aaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa', '49bbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbc', 'admin-web',
 '49dddddd-dddd-dddd-dddd-dddddddddddd', 'Normal Tenant Group', 1, FALSE);

DO $$
BEGIN
    IF NOT EXISTS
    (
        SELECT 1 FROM identity_access.user_groups
        WHERE identity_scope_id = '49aaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa'
          AND tenant_id = '49bbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb'
          AND application_key = 'admin-web'
          AND group_id = '49cccccc-cccc-cccc-cccc-cccccccccccc'
          AND is_template = TRUE
    ) THEN
        RAISE EXCEPTION 'reusable group marker was not persisted';
    END IF;
END
$$;

ROLLBACK;
