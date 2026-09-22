\set ON_ERROR_STOP on

BEGIN;

INSERT INTO identity_access.users(identity_scope_id, user_id, display_name, status)
VALUES
('aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa', 'cccccccc-cccc-4ccc-8ccc-cccccccccccc', 'Scope A', 1),
('bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbbb', 'cccccccc-cccc-4ccc-8ccc-cccccccccccc', 'Scope B', 1)
ON CONFLICT (identity_scope_id, user_id) DO UPDATE
SET display_name = EXCLUDED.display_name, status = EXCLUDED.status, row_version = 1;

DO $$
DECLARE affected integer;
BEGIN
    UPDATE identity_access.users
    SET display_name = 'Scope A first writer', row_version = row_version + 1
    WHERE identity_scope_id = 'aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa'
      AND user_id = 'cccccccc-cccc-4ccc-8ccc-cccccccccccc'
      AND row_version = 1;
    GET DIAGNOSTICS affected = ROW_COUNT;
    IF affected <> 1 THEN
        RAISE EXCEPTION 'first optimistic update should affect exactly one row';
    END IF;

    UPDATE identity_access.users
    SET display_name = 'Scope A stale writer', row_version = row_version + 1
    WHERE identity_scope_id = 'aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa'
      AND user_id = 'cccccccc-cccc-4ccc-8ccc-cccccccccccc'
      AND row_version = 1;
    GET DIAGNOSTICS affected = ROW_COUNT;
    IF affected <> 0 THEN
        RAISE EXCEPTION 'stale optimistic update must affect zero rows';
    END IF;

    UPDATE identity_access.users
    SET display_name = 'Scope B independent writer', row_version = row_version + 1
    WHERE identity_scope_id = 'bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbbb'
      AND user_id = 'cccccccc-cccc-4ccc-8ccc-cccccccccccc'
      AND row_version = 1;
    GET DIAGNOSTICS affected = ROW_COUNT;
    IF affected <> 1 THEN
        RAISE EXCEPTION 'different identity scope must update independently';
    END IF;
END $$;

ROLLBACK;
