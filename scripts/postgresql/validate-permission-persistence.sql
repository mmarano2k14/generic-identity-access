\set ON_ERROR_STOP on
BEGIN;

INSERT INTO identity_access.tenants(identity_scope_id, tenant_id, display_name, status)
VALUES
('31111111-1111-1111-1111-111111111111', '3bbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb', 'Tenant A', 1),
('32222222-2222-2222-2222-222222222222', '3bbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb', 'Tenant B', 1);

INSERT INTO identity_access.user_groups
(identity_scope_id, tenant_id, application_key, group_id, display_name, status)
VALUES
('31111111-1111-1111-1111-111111111111', '3bbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb',
 'app-a', '3ddddddd-dddd-dddd-dddd-dddddddddddd', 'Operators A', 1),
('32222222-2222-2222-2222-222222222222', '3bbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb',
 'app-a', '3ddddddd-dddd-dddd-dddd-dddddddddddd', 'Operators B', 1);

INSERT INTO identity_access.application_security_models(identity_scope_id, application_key, model_version)
VALUES
('31111111-1111-1111-1111-111111111111', 'app-a', 1),
('32222222-2222-2222-2222-222222222222', 'app-a', 1);

INSERT INTO identity_access.application_capabilities
(identity_scope_id, application_key, model_version, capability_resource, capability_feature, capability_action, display_name)
VALUES
('31111111-1111-1111-1111-111111111111', 'app-a', 1, 'billing', 'invoice', 'read', 'Read invoices'),
('32222222-2222-2222-2222-222222222222', 'app-a', 1, 'billing', 'invoice', 'read', 'Read invoices');

INSERT INTO identity_access.permission_policies
(identity_scope_id, tenant_id, application_key, policy_id, display_name, status)
VALUES
('31111111-1111-1111-1111-111111111111', '3bbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb',
 'app-a', '3eeeeeee-eeee-eeee-eeee-eeeeeeeeeeee', 'Invoice readers A', 1),
('32222222-2222-2222-2222-222222222222', '3bbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb',
 'app-a', '3eeeeeee-eeee-eeee-eeee-eeeeeeeeeeee', 'Invoice readers B', 1);

INSERT INTO identity_access.policy_statements
(identity_scope_id, tenant_id, application_key, policy_id, statement_id, model_version,
 capability_resource, capability_feature, capability_action)
VALUES
('31111111-1111-1111-1111-111111111111', '3bbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb',
 'app-a', '3eeeeeee-eeee-eeee-eeee-eeeeeeeeeeee', '3fffffff-ffff-ffff-ffff-ffffffffffff', 1,
 'billing', 'invoice', 'read'),
('32222222-2222-2222-2222-222222222222', '3bbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb',
 'app-a', '3eeeeeee-eeee-eeee-eeee-eeeeeeeeeeee', '3fffffff-ffff-ffff-ffff-ffffffffffff', 1,
 'billing', 'invoice', 'read');

INSERT INTO identity_access.group_policy_bindings
(identity_scope_id, tenant_id, application_key, group_id, policy_id)
VALUES
('31111111-1111-1111-1111-111111111111', '3bbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb',
 'app-a', '3ddddddd-dddd-dddd-dddd-dddddddddddd', '3eeeeeee-eeee-eeee-eeee-eeeeeeeeeeee'),
('32222222-2222-2222-2222-222222222222', '3bbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb',
 'app-a', '3ddddddd-dddd-dddd-dddd-dddddddddddd', '3eeeeeee-eeee-eeee-eeee-eeeeeeeeeeee');

DO $$
DECLARE
    policy_count integer;
    binding_count integer;
BEGIN
    SELECT count(*) INTO policy_count FROM identity_access.permission_policies
    WHERE policy_id = '3eeeeeee-eeee-eeee-eeee-eeeeeeeeeeee';
    IF policy_count <> 2 THEN
        RAISE EXCEPTION 'scope isolation validation failed for permission policies';
    END IF;

    SELECT count(*) INTO binding_count FROM identity_access.group_policy_bindings
    WHERE policy_id = '3eeeeeee-eeee-eeee-eeee-eeeeeeeeeeee';
    IF binding_count <> 2 THEN
        RAISE EXCEPTION 'scope isolation validation failed for group policy bindings';
    END IF;
END $$;

UPDATE identity_access.permission_policies
SET display_name = 'Invoice readers A updated', row_version = row_version + 1
WHERE identity_scope_id = '31111111-1111-1111-1111-111111111111'
  AND tenant_id = '3bbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb'
  AND application_key = 'app-a'
  AND policy_id = '3eeeeeee-eeee-eeee-eeee-eeeeeeeeeeee'
  AND row_version = 1;

DO $$
DECLARE
    a_name text;
    b_name text;
    stale_count integer;
BEGIN
    SELECT display_name INTO a_name FROM identity_access.permission_policies
    WHERE identity_scope_id = '31111111-1111-1111-1111-111111111111'
      AND tenant_id = '3bbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb'
      AND application_key = 'app-a'
      AND policy_id = '3eeeeeee-eeee-eeee-eeee-eeeeeeeeeeee';
    SELECT display_name INTO b_name FROM identity_access.permission_policies
    WHERE identity_scope_id = '32222222-2222-2222-2222-222222222222'
      AND tenant_id = '3bbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb'
      AND application_key = 'app-a'
      AND policy_id = '3eeeeeee-eeee-eeee-eeee-eeeeeeeeeeee';
    IF a_name <> 'Invoice readers A updated' OR b_name <> 'Invoice readers B' THEN
        RAISE EXCEPTION 'policy scoped-update independence validation failed';
    END IF;

    UPDATE identity_access.permission_policies
    SET display_name = 'stale write', row_version = row_version + 1
    WHERE identity_scope_id = '31111111-1111-1111-1111-111111111111'
      AND tenant_id = '3bbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb'
      AND application_key = 'app-a'
      AND policy_id = '3eeeeeee-eeee-eeee-eeee-eeeeeeeeeeee'
      AND row_version = 1;
    GET DIAGNOSTICS stale_count = ROW_COUNT;
    IF stale_count <> 0 THEN
        RAISE EXCEPTION 'stale policy update unexpectedly succeeded';
    END IF;
END $$;

ROLLBACK;
