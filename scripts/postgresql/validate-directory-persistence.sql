\set ON_ERROR_STOP on
BEGIN;

-- Isolated fixtures. The transaction is rolled back at the end.
WITH ids AS (
    SELECT
        '11111111-1111-1111-1111-111111111111'::uuid AS scope_a,
        '22222222-2222-2222-2222-222222222222'::uuid AS scope_b,
        'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa'::uuid AS user_id,
        'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb'::uuid AS tenant_id,
        'cccccccc-cccc-cccc-cccc-cccccccccccc'::uuid AS membership_id,
        'dddddddd-dddd-dddd-dddd-dddddddddddd'::uuid AS group_id
)
INSERT INTO identity_access.users(identity_scope_id, user_id, display_name, status)
SELECT scope_a, user_id, 'Scope A User', 1 FROM ids
UNION ALL
SELECT scope_b, user_id, 'Scope B User', 1 FROM ids;

INSERT INTO identity_access.tenants(identity_scope_id, tenant_id, display_name, status)
VALUES
('11111111-1111-1111-1111-111111111111', 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb', 'Tenant A', 1),
('22222222-2222-2222-2222-222222222222', 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb', 'Tenant B', 1);

INSERT INTO identity_access.tenant_memberships
(identity_scope_id, membership_id, tenant_id, user_id, status)
VALUES
('11111111-1111-1111-1111-111111111111', 'cccccccc-cccc-cccc-cccc-cccccccccccc',
 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb', 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa', 1),
('22222222-2222-2222-2222-222222222222', 'cccccccc-cccc-cccc-cccc-cccccccccccc',
 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb', 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa', 1);

INSERT INTO identity_access.user_groups
(identity_scope_id, tenant_id, application_key, group_id, display_name, status)
VALUES
('11111111-1111-1111-1111-111111111111', 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb',
 'app-a', 'dddddddd-dddd-dddd-dddd-dddddddddddd', 'Operators A', 1),
('22222222-2222-2222-2222-222222222222', 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb',
 'app-a', 'dddddddd-dddd-dddd-dddd-dddddddddddd', 'Operators B', 1);

INSERT INTO identity_access.group_memberships
(identity_scope_id, tenant_id, application_key, group_id, tenant_membership_id)
VALUES
('11111111-1111-1111-1111-111111111111', 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb',
 'app-a', 'dddddddd-dddd-dddd-dddd-dddddddddddd', 'cccccccc-cccc-cccc-cccc-cccccccccccc'),
('22222222-2222-2222-2222-222222222222', 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb',
 'app-a', 'dddddddd-dddd-dddd-dddd-dddddddddddd', 'cccccccc-cccc-cccc-cccc-cccccccccccc');

DO $$
DECLARE
    user_count integer;
    group_edge_count integer;
BEGIN
    SELECT count(*) INTO user_count
    FROM identity_access.users
    WHERE user_id = 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa';
    IF user_count <> 2 THEN
        RAISE EXCEPTION 'scope isolation validation failed for users';
    END IF;

    SELECT count(*) INTO group_edge_count
    FROM identity_access.group_memberships
    WHERE group_id = 'dddddddd-dddd-dddd-dddd-dddddddddddd';
    IF group_edge_count <> 2 THEN
        RAISE EXCEPTION 'scope isolation validation failed for group memberships';
    END IF;
END $$;

-- A scoped update must affect exactly one logical record even when local IDs are identical.
UPDATE identity_access.tenants
SET display_name = 'Tenant A Updated', row_version = row_version + 1
WHERE identity_scope_id = '11111111-1111-1111-1111-111111111111'
  AND tenant_id = 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb'
  AND row_version = 1;

DO $$
DECLARE
    a_name text;
    b_name text;
BEGIN
    SELECT display_name INTO a_name FROM identity_access.tenants
    WHERE identity_scope_id = '11111111-1111-1111-1111-111111111111'
      AND tenant_id = 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb';
    SELECT display_name INTO b_name FROM identity_access.tenants
    WHERE identity_scope_id = '22222222-2222-2222-2222-222222222222'
      AND tenant_id = 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb';
    IF a_name <> 'Tenant A Updated' OR b_name <> 'Tenant B' THEN
        RAISE EXCEPTION 'scoped update independence validation failed';
    END IF;
END $$;

ROLLBACK;
