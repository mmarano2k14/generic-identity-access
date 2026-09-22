\set ON_ERROR_STOP on
BEGIN;

INSERT INTO identity_access.users(identity_scope_id, user_id, display_name, status)
VALUES
('25111111-1111-1111-1111-111111111111', '25aaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa',
 'Atomic User', 1);

INSERT INTO identity_access.tenants(identity_scope_id, tenant_id, display_name, status)
VALUES
('25111111-1111-1111-1111-111111111111', '25bbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb',
 'Atomic Tenant', 1);

INSERT INTO identity_access.tenant_memberships
(identity_scope_id, membership_id, tenant_id, user_id, status)
VALUES
('25111111-1111-1111-1111-111111111111', '25cccccc-cccc-cccc-cccc-cccccccccccc',
 '25bbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb', '25aaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa', 2);

INSERT INTO identity_access.user_groups
(identity_scope_id, tenant_id, application_key, group_id, display_name, status)
VALUES
('25111111-1111-1111-1111-111111111111', '25bbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb',
 'app-a', '25dddddd-dddd-dddd-dddd-dddddddddddd', 'Atomic Group', 1);

-- Suspended membership must not be inserted by the atomic group-membership statement.
WITH eligible AS
(
    SELECT tm.user_id
    FROM identity_access.user_groups AS g
    INNER JOIN identity_access.tenant_memberships AS tm
        ON tm.identity_scope_id = g.identity_scope_id
       AND tm.tenant_id = g.tenant_id
    WHERE g.identity_scope_id = '25111111-1111-1111-1111-111111111111'
      AND g.tenant_id = '25bbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb'
      AND g.application_key = 'app-a'
      AND g.group_id = '25dddddd-dddd-dddd-dddd-dddddddddddd'
      AND g.status = 1
      AND tm.membership_id = '25cccccc-cccc-cccc-cccc-cccccccccccc'
      AND tm.status = 1
)
INSERT INTO identity_access.group_memberships
(identity_scope_id, tenant_id, application_key, group_id, tenant_membership_id)
SELECT
'25111111-1111-1111-1111-111111111111',
'25bbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb',
'app-a',
'25dddddd-dddd-dddd-dddd-dddddddddddd',
'25cccccc-cccc-cccc-cccc-cccccccccccc'
FROM eligible;

DO $$
BEGIN
    IF EXISTS (
        SELECT 1
        FROM identity_access.group_memberships
        WHERE identity_scope_id = '25111111-1111-1111-1111-111111111111'
          AND tenant_membership_id = '25cccccc-cccc-cccc-cccc-cccccccccccc'
    ) THEN
        RAISE EXCEPTION 'atomic group-membership validation accepted a suspended membership';
    END IF;
END
$$;

UPDATE identity_access.tenant_memberships
SET status = 1, row_version = row_version + 1
WHERE identity_scope_id = '25111111-1111-1111-1111-111111111111'
  AND membership_id = '25cccccc-cccc-cccc-cccc-cccccccccccc';

WITH eligible AS
(
    SELECT tm.user_id
    FROM identity_access.user_groups AS g
    INNER JOIN identity_access.tenant_memberships AS tm
        ON tm.identity_scope_id = g.identity_scope_id
       AND tm.tenant_id = g.tenant_id
    WHERE g.identity_scope_id = '25111111-1111-1111-1111-111111111111'
      AND g.tenant_id = '25bbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb'
      AND g.application_key = 'app-a'
      AND g.group_id = '25dddddd-dddd-dddd-dddd-dddddddddddd'
      AND g.status = 1
      AND tm.membership_id = '25cccccc-cccc-cccc-cccc-cccccccccccc'
      AND tm.status = 1
)
INSERT INTO identity_access.group_memberships
(identity_scope_id, tenant_id, application_key, group_id, tenant_membership_id)
SELECT
'25111111-1111-1111-1111-111111111111',
'25bbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb',
'app-a',
'25dddddd-dddd-dddd-dddd-dddddddddddd',
'25cccccc-cccc-cccc-cccc-cccccccccccc'
FROM eligible;

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1
        FROM identity_access.group_memberships
        WHERE identity_scope_id = '25111111-1111-1111-1111-111111111111'
          AND tenant_membership_id = '25cccccc-cccc-cccc-cccc-cccccccccccc'
    ) THEN
        RAISE EXCEPTION 'atomic group-membership validation did not insert an active membership';
    END IF;
END
$$;

-- Credential creation must not create a row for a missing user.
WITH inserted AS
(
    INSERT INTO identity_access.password_credentials
    (identity_scope_id, user_id, login_identifier, normalized_login_identifier, password_hash)
    SELECT
        '25111111-1111-1111-1111-111111111111',
        '25eeeeee-eeee-eeee-eeee-eeeeeeeeeeee',
        'missing@example.test',
        'missing@example.test',
        'test-hash'
    FROM identity_access.users AS u
    WHERE u.identity_scope_id = '25111111-1111-1111-1111-111111111111'
      AND u.user_id = '25eeeeee-eeee-eeee-eeee-eeeeeeeeeeee'
    RETURNING row_version
)
SELECT count(*) FROM inserted;

DO $$
BEGIN
    IF EXISTS (
        SELECT 1
        FROM identity_access.password_credentials
        WHERE identity_scope_id = '25111111-1111-1111-1111-111111111111'
          AND user_id = '25eeeeee-eeee-eeee-eeee-eeeeeeeeeeee'
    ) THEN
        RAISE EXCEPTION 'atomic credential mutation created a credential for a missing user';
    END IF;
END
$$;

ROLLBACK;
