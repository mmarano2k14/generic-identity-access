\set ON_ERROR_STOP on
BEGIN;

INSERT INTO identity_access.users
(identity_scope_id, user_id, display_name, status)
VALUES
('34111111-1111-1111-1111-111111111111',
 '34aaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa',
 'Authority Admin Fixture',
 1),
('34111111-1111-1111-1111-111111111111',
 '34bbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb',
 'Inactive Authority Fixture',
 2);

INSERT INTO identity_access.application_security_models
(identity_scope_id, application_key, model_version)
VALUES
('34111111-1111-1111-1111-111111111111', 'authority-admin-test', 34001);

INSERT INTO identity_access.application_capabilities
(identity_scope_id, application_key, model_version,
 capability_resource, capability_feature, capability_action, display_name)
VALUES
('34111111-1111-1111-1111-111111111111',
 'authority-admin-test',
 34001,
 'identity-access',
 'scope-authority-group',
 'write',
 'Scope Authority Group Write');

INSERT INTO identity_access.identity_scope_administration_groups
(identity_scope_id, application_key, group_id, display_name, status)
VALUES
('34111111-1111-1111-1111-111111111111',
 'authority-admin-test',
 '34cccccc-cccc-cccc-cccc-cccccccccccc',
 'Active Authority Group',
 1),
('34111111-1111-1111-1111-111111111111',
 'authority-admin-test',
 '34dddddd-dddd-dddd-dddd-dddddddddddd',
 'Inactive Authority Group',
 2);

WITH eligible AS
(
    SELECT 1
    FROM identity_access.identity_scope_administration_groups AS g
    JOIN identity_access.users AS u
      ON u.identity_scope_id = g.identity_scope_id
     AND u.user_id = '34aaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa'
    WHERE g.identity_scope_id = '34111111-1111-1111-1111-111111111111'
      AND g.application_key = 'authority-admin-test'
      AND g.group_id = '34cccccc-cccc-cccc-cccc-cccccccccccc'
      AND g.status = 1
      AND u.status = 1
)
INSERT INTO identity_access.identity_scope_administration_group_memberships
(identity_scope_id, application_key, group_id, user_id)
SELECT
'34111111-1111-1111-1111-111111111111',
'authority-admin-test',
'34cccccc-cccc-cccc-cccc-cccccccccccc',
'34aaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa'
FROM eligible;

DO $$
BEGIN
    IF NOT EXISTS
    (
        SELECT 1
        FROM identity_access.identity_scope_administration_group_memberships
        WHERE identity_scope_id = '34111111-1111-1111-1111-111111111111'
          AND application_key = 'authority-admin-test'
          AND group_id = '34cccccc-cccc-cccc-cccc-cccccccccccc'
          AND user_id = '34aaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa'
    ) THEN
        RAISE EXCEPTION 'active user was not added to active authority group';
    END IF;
END
$$;

WITH eligible AS
(
    SELECT 1
    FROM identity_access.identity_scope_administration_groups AS g
    JOIN identity_access.users AS u
      ON u.identity_scope_id = g.identity_scope_id
     AND u.user_id = '34bbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb'
    WHERE g.identity_scope_id = '34111111-1111-1111-1111-111111111111'
      AND g.application_key = 'authority-admin-test'
      AND g.group_id = '34cccccc-cccc-cccc-cccc-cccccccccccc'
      AND g.status = 1
      AND u.status = 1
)
INSERT INTO identity_access.identity_scope_administration_group_memberships
(identity_scope_id, application_key, group_id, user_id)
SELECT
'34111111-1111-1111-1111-111111111111',
'authority-admin-test',
'34cccccc-cccc-cccc-cccc-cccccccccccc',
'34bbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb'
FROM eligible;

DO $$
BEGIN
    IF EXISTS
    (
        SELECT 1
        FROM identity_access.identity_scope_administration_group_memberships
        WHERE identity_scope_id = '34111111-1111-1111-1111-111111111111'
          AND application_key = 'authority-admin-test'
          AND group_id = '34cccccc-cccc-cccc-cccc-cccccccccccc'
          AND user_id = '34bbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb'
    ) THEN
        RAISE EXCEPTION 'inactive user was added to authority group';
    END IF;
END
$$;

INSERT INTO identity_access.identity_scope_administration_policies
(identity_scope_id, application_key, policy_id, display_name, status)
VALUES
('34111111-1111-1111-1111-111111111111',
 'authority-admin-test',
 '34eeeeee-eeee-eeee-eeee-eeeeeeeeeeee',
 'Active Authority Policy',
 1);

INSERT INTO identity_access.identity_scope_administration_policy_statements
(identity_scope_id, application_key, policy_id, statement_id, model_version,
 capability_resource, capability_feature, capability_action)
VALUES
('34111111-1111-1111-1111-111111111111',
 'authority-admin-test',
 '34eeeeee-eeee-eeee-eeee-eeeeeeeeeeee',
 '34ffffff-ffff-ffff-ffff-ffffffffffff',
 34001,
 'identity-access',
 'scope-authority-group',
 'write');

WITH eligible AS
(
    SELECT 1
    FROM identity_access.identity_scope_administration_groups AS g
    JOIN identity_access.identity_scope_administration_policies AS p
      ON p.identity_scope_id = g.identity_scope_id
     AND p.application_key = g.application_key
    WHERE g.identity_scope_id = '34111111-1111-1111-1111-111111111111'
      AND g.application_key = 'authority-admin-test'
      AND g.group_id = '34cccccc-cccc-cccc-cccc-cccccccccccc'
      AND g.status = 1
      AND p.policy_id = '34eeeeee-eeee-eeee-eeee-eeeeeeeeeeee'
      AND p.status = 1
)
INSERT INTO identity_access.identity_scope_administration_group_policy_bindings
(identity_scope_id, application_key, group_id, policy_id)
SELECT
'34111111-1111-1111-1111-111111111111',
'authority-admin-test',
'34cccccc-cccc-cccc-cccc-cccccccccccc',
'34eeeeee-eeee-eeee-eeee-eeeeeeeeeeee'
FROM eligible;

DO $$
BEGIN
    IF NOT EXISTS
    (
        SELECT 1
        FROM identity_access.identity_scope_administration_group_policy_bindings
        WHERE identity_scope_id = '34111111-1111-1111-1111-111111111111'
          AND application_key = 'authority-admin-test'
          AND group_id = '34cccccc-cccc-cccc-cccc-cccccccccccc'
          AND policy_id = '34eeeeee-eeee-eeee-eeee-eeeeeeeeeeee'
    ) THEN
        RAISE EXCEPTION 'active authority group-policy binding was not created';
    END IF;
END
$$;

ROLLBACK;
