BEGIN;

INSERT INTO identity_access.users
    (identity_scope_id, user_id, display_name, status)
VALUES
    ('71000000-0000-4000-8000-000000000001', '71000000-0000-4000-8000-000000000011', 'Projection User', 1);

INSERT INTO identity_access.tenants
    (identity_scope_id, tenant_id, display_name, status)
VALUES
    ('71000000-0000-4000-8000-000000000001', '71000000-0000-4000-8000-000000000021', 'Projection Tenant', 1);

INSERT INTO identity_access.tenant_memberships
    (identity_scope_id, membership_id, tenant_id, user_id, status)
VALUES
    ('71000000-0000-4000-8000-000000000001', '71000000-0000-4000-8000-000000000031',
     '71000000-0000-4000-8000-000000000021', '71000000-0000-4000-8000-000000000011', 1);

INSERT INTO identity_access.user_groups
    (identity_scope_id, tenant_id, application_key, group_id, display_name, status)
VALUES
    ('71000000-0000-4000-8000-000000000001', '71000000-0000-4000-8000-000000000021',
     'app-a', '71000000-0000-4000-8000-000000000041', 'Active Group', 1),
    ('71000000-0000-4000-8000-000000000001', '71000000-0000-4000-8000-000000000021',
     'app-a', '71000000-0000-4000-8000-000000000042', 'Suspended Group', 2);

INSERT INTO identity_access.group_memberships
    (identity_scope_id, tenant_id, application_key, group_id, tenant_membership_id)
VALUES
    ('71000000-0000-4000-8000-000000000001', '71000000-0000-4000-8000-000000000021',
     'app-a', '71000000-0000-4000-8000-000000000041', '71000000-0000-4000-8000-000000000031'),
    ('71000000-0000-4000-8000-000000000001', '71000000-0000-4000-8000-000000000021',
     'app-a', '71000000-0000-4000-8000-000000000042', '71000000-0000-4000-8000-000000000031');

INSERT INTO identity_access.application_security_models
    (identity_scope_id, application_key, model_version)
VALUES
    ('71000000-0000-4000-8000-000000000001', 'app-a', 1);

INSERT INTO identity_access.application_capabilities
    (identity_scope_id, application_key, model_version,
     capability_resource, capability_feature, capability_action, display_name)
VALUES
    ('71000000-0000-4000-8000-000000000001', 'app-a', 1,
     'billing', 'invoice', 'read', 'Read invoices');

INSERT INTO identity_access.permission_policies
    (identity_scope_id, tenant_id, application_key, policy_id, display_name, status)
VALUES
    ('71000000-0000-4000-8000-000000000001', '71000000-0000-4000-8000-000000000021',
     'app-a', '71000000-0000-4000-8000-000000000051', 'Active Policy', 1),
    ('71000000-0000-4000-8000-000000000001', '71000000-0000-4000-8000-000000000021',
     'app-a', '71000000-0000-4000-8000-000000000052', 'Suspended Policy', 2);

INSERT INTO identity_access.policy_statements
    (identity_scope_id, tenant_id, application_key, policy_id, statement_id, model_version,
     capability_resource, capability_feature, capability_action)
VALUES
    ('71000000-0000-4000-8000-000000000001', '71000000-0000-4000-8000-000000000021',
     'app-a', '71000000-0000-4000-8000-000000000051', '71000000-0000-4000-8000-000000000061', 1,
     'billing', 'invoice', 'read'),
    ('71000000-0000-4000-8000-000000000001', '71000000-0000-4000-8000-000000000021',
     'app-a', '71000000-0000-4000-8000-000000000052', '71000000-0000-4000-8000-000000000062', 1,
     'billing', 'invoice', 'read');

INSERT INTO identity_access.group_policy_bindings
    (identity_scope_id, tenant_id, application_key, group_id, policy_id)
VALUES
    ('71000000-0000-4000-8000-000000000001', '71000000-0000-4000-8000-000000000021',
     'app-a', '71000000-0000-4000-8000-000000000041', '71000000-0000-4000-8000-000000000051'),
    ('71000000-0000-4000-8000-000000000001', '71000000-0000-4000-8000-000000000021',
     'app-a', '71000000-0000-4000-8000-000000000041', '71000000-0000-4000-8000-000000000052'),
    ('71000000-0000-4000-8000-000000000001', '71000000-0000-4000-8000-000000000021',
     'app-a', '71000000-0000-4000-8000-000000000042', '71000000-0000-4000-8000-000000000051');

DO $$
DECLARE
    projected_count integer;
BEGIN
    SELECT count(*) INTO projected_count
    FROM identity_access.users u
    JOIN identity_access.tenants t
      ON t.identity_scope_id = u.identity_scope_id
     AND t.tenant_id = '71000000-0000-4000-8000-000000000021'
    JOIN identity_access.tenant_memberships tm
      ON tm.identity_scope_id = u.identity_scope_id
     AND tm.tenant_id = t.tenant_id
     AND tm.user_id = u.user_id
    JOIN identity_access.group_memberships gm
      ON gm.identity_scope_id = tm.identity_scope_id
     AND gm.tenant_id = tm.tenant_id
     AND gm.tenant_membership_id = tm.membership_id
     AND gm.application_key = 'app-a'
    JOIN identity_access.user_groups ug
      ON ug.identity_scope_id = gm.identity_scope_id
     AND ug.tenant_id = gm.tenant_id
     AND ug.application_key = gm.application_key
     AND ug.group_id = gm.group_id
    JOIN identity_access.group_policy_bindings gpb
      ON gpb.identity_scope_id = ug.identity_scope_id
     AND gpb.tenant_id = ug.tenant_id
     AND gpb.application_key = ug.application_key
     AND gpb.group_id = ug.group_id
    JOIN identity_access.permission_policies pp
      ON pp.identity_scope_id = gpb.identity_scope_id
     AND pp.tenant_id = gpb.tenant_id
     AND pp.application_key = gpb.application_key
     AND pp.policy_id = gpb.policy_id
    JOIN identity_access.policy_statements ps
      ON ps.identity_scope_id = pp.identity_scope_id
     AND ps.tenant_id = pp.tenant_id
     AND ps.application_key = pp.application_key
     AND ps.policy_id = pp.policy_id
    WHERE u.identity_scope_id = '71000000-0000-4000-8000-000000000001'
      AND u.user_id = '71000000-0000-4000-8000-000000000011'
      AND u.status = 1
      AND t.status = 1
      AND tm.status = 1
      AND ug.status = 1
      AND pp.status = 1;

    IF projected_count <> 1 THEN
        RAISE EXCEPTION 'Expected exactly one active assigned capability, got %', projected_count;
    END IF;
END $$;

ROLLBACK;
