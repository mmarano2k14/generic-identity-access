\set ON_ERROR_STOP on
BEGIN;

INSERT INTO identity_access.users
(identity_scope_id, user_id, display_name, status)
VALUES
('33111111-1111-1111-1111-111111111111',
 '33aaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa',
 'Scope Authority User',
 1);

INSERT INTO identity_access.application_security_models
(identity_scope_id, application_key, model_version)
VALUES
('33111111-1111-1111-1111-111111111111', 'scope-admin-test', 33001);

INSERT INTO identity_access.application_capabilities
(identity_scope_id, application_key, model_version,
 capability_resource, capability_feature, capability_action, display_name)
VALUES
('33111111-1111-1111-1111-111111111111',
 'scope-admin-test',
 33001,
 'identity-access',
 'user',
 'write',
 'User Write');

INSERT INTO identity_access.identity_scope_administration_groups
(identity_scope_id, application_key, group_id, display_name, status)
VALUES
('33111111-1111-1111-1111-111111111111',
 'scope-admin-test',
 '33bbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb',
 'Scope Administrators',
 1);

INSERT INTO identity_access.identity_scope_administration_group_memberships
(identity_scope_id, application_key, group_id, user_id)
VALUES
('33111111-1111-1111-1111-111111111111',
 'scope-admin-test',
 '33bbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb',
 '33aaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa');

INSERT INTO identity_access.identity_scope_administration_policies
(identity_scope_id, application_key, policy_id, display_name, status)
VALUES
('33111111-1111-1111-1111-111111111111',
 'scope-admin-test',
 '33cccccc-cccc-cccc-cccc-cccccccccccc',
 'Scope Policy',
 1);

INSERT INTO identity_access.identity_scope_administration_policy_statements
(identity_scope_id, application_key, policy_id, statement_id, model_version,
 capability_resource, capability_feature, capability_action)
VALUES
('33111111-1111-1111-1111-111111111111',
 'scope-admin-test',
 '33cccccc-cccc-cccc-cccc-cccccccccccc',
 '33dddddd-dddd-dddd-dddd-dddddddddddd',
 33001,
 'identity-access',
 'user',
 'write');

INSERT INTO identity_access.identity_scope_administration_group_policy_bindings
(identity_scope_id, application_key, group_id, policy_id)
VALUES
('33111111-1111-1111-1111-111111111111',
 'scope-admin-test',
 '33bbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb',
 '33cccccc-cccc-cccc-cccc-cccccccccccc');

DO $$
DECLARE
    projected_count integer;
BEGIN
    SELECT count(*)
    INTO projected_count
    FROM identity_access.users AS u
    JOIN identity_access.identity_scope_administration_group_memberships AS gm
      ON gm.identity_scope_id = u.identity_scope_id
     AND gm.user_id = u.user_id
     AND gm.application_key = 'scope-admin-test'
    JOIN identity_access.identity_scope_administration_groups AS g
      ON g.identity_scope_id = gm.identity_scope_id
     AND g.application_key = gm.application_key
     AND g.group_id = gm.group_id
    JOIN identity_access.identity_scope_administration_group_policy_bindings AS b
      ON b.identity_scope_id = g.identity_scope_id
     AND b.application_key = g.application_key
     AND b.group_id = g.group_id
    JOIN identity_access.identity_scope_administration_policies AS p
      ON p.identity_scope_id = b.identity_scope_id
     AND p.application_key = b.application_key
     AND p.policy_id = b.policy_id
    JOIN identity_access.identity_scope_administration_policy_statements AS s
      ON s.identity_scope_id = p.identity_scope_id
     AND s.application_key = p.application_key
     AND s.policy_id = p.policy_id
    WHERE u.identity_scope_id = '33111111-1111-1111-1111-111111111111'
      AND u.user_id = '33aaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa'
      AND u.status = 1
      AND g.status = 1
      AND p.status = 1
      AND s.capability_resource = 'identity-access'
      AND s.capability_feature = 'user'
      AND s.capability_action = 'write';

    IF projected_count <> 1 THEN
        RAISE EXCEPTION 'identity-scope administration grant projection failed';
    END IF;
END
$$;

UPDATE identity_access.identity_scope_administration_groups
SET status = 2,
    row_version = row_version + 1,
    updated_at = transaction_timestamp()
WHERE identity_scope_id = '33111111-1111-1111-1111-111111111111'
  AND application_key = 'scope-admin-test'
  AND group_id = '33bbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb';

DO $$
DECLARE
    active_count integer;
BEGIN
    SELECT count(*)
    INTO active_count
    FROM identity_access.users AS u
    JOIN identity_access.identity_scope_administration_group_memberships AS gm
      ON gm.identity_scope_id = u.identity_scope_id
     AND gm.user_id = u.user_id
     AND gm.application_key = 'scope-admin-test'
    JOIN identity_access.identity_scope_administration_groups AS g
      ON g.identity_scope_id = gm.identity_scope_id
     AND g.application_key = gm.application_key
     AND g.group_id = gm.group_id
    JOIN identity_access.identity_scope_administration_group_policy_bindings AS b
      ON b.identity_scope_id = g.identity_scope_id
     AND b.application_key = g.application_key
     AND b.group_id = g.group_id
    JOIN identity_access.identity_scope_administration_policies AS p
      ON p.identity_scope_id = b.identity_scope_id
     AND p.application_key = b.application_key
     AND p.policy_id = b.policy_id
    JOIN identity_access.identity_scope_administration_policy_statements AS s
      ON s.identity_scope_id = p.identity_scope_id
     AND s.application_key = p.application_key
     AND s.policy_id = p.policy_id
    WHERE u.identity_scope_id = '33111111-1111-1111-1111-111111111111'
      AND u.user_id = '33aaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa'
      AND u.status = 1
      AND g.status = 1
      AND p.status = 1;

    IF active_count <> 0 THEN
        RAISE EXCEPTION 'disabled identity-scope administration group still projects grants';
    END IF;
END
$$;

ROLLBACK;
