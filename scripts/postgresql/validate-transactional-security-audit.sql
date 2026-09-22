\set ON_ERROR_STOP on
BEGIN;

SELECT set_config(
    'identity_access.correlation_id',
    'transactional-audit-fixture',
    true);

SELECT set_config(
    'identity_access.actor_identity_scope_id',
    '35111111-1111-1111-1111-111111111111',
    true);

SELECT set_config(
    'identity_access.actor_user_id',
    '35aaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa',
    true);

SELECT set_config(
    'identity_access.actor_session_id',
    '35bbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb',
    true);

SELECT set_config(
    'identity_access.actor_client_id',
    'audit-test-client',
    true);

SELECT set_config(
    'identity_access.actor_application_key',
    'audit-test',
    true);

SELECT set_config(
    'identity_access.authentication_context_key',
    'primary',
    true);

INSERT INTO identity_access.users
(identity_scope_id, user_id, display_name, status)
VALUES
('35111111-1111-1111-1111-111111111111',
 '35cccccc-cccc-cccc-cccc-cccccccccccc',
 'Transactional Audit User',
 1);

UPDATE identity_access.users
SET display_name = 'Transactional Audit User Updated',
    row_version = row_version + 1,
    updated_at = transaction_timestamp()
WHERE identity_scope_id = '35111111-1111-1111-1111-111111111111'
  AND user_id = '35cccccc-cccc-cccc-cccc-cccccccccccc';

INSERT INTO identity_access.password_credentials
(identity_scope_id, user_id, login_identifier, normalized_login_identifier, password_hash)
VALUES
('35111111-1111-1111-1111-111111111111',
 '35cccccc-cccc-cccc-cccc-cccccccccccc',
 'transactional-audit@example.test',
 'TRANSACTIONAL-AUDIT@EXAMPLE.TEST',
 'fixture-password-hash-that-must-never-enter-ledger');

DO $$
DECLARE
    user_insert_count integer;
    user_update_count integer;
    credential_count integer;
BEGIN
    SELECT count(*)
    INTO user_insert_count
    FROM identity_access.security_mutation_events
    WHERE identity_scope_id = '35111111-1111-1111-1111-111111111111'
      AND table_name = 'users'
      AND operation = 'INSERT'
      AND record_key @> '{"user_id":"35cccccc-cccc-cccc-cccc-cccccccccccc"}'::jsonb
      AND correlation_id = 'transactional-audit-fixture'
      AND actor_user_id = '35aaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa'
      AND actor_session_id = '35bbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb'
      AND actor_client_id = 'audit-test-client';

    IF user_insert_count <> 1 THEN
        RAISE EXCEPTION 'transactional user INSERT audit event was not captured with actor provenance';
    END IF;

    SELECT count(*)
    INTO user_update_count
    FROM identity_access.security_mutation_events
    WHERE identity_scope_id = '35111111-1111-1111-1111-111111111111'
      AND table_name = 'users'
      AND operation = 'UPDATE'
      AND record_key @> '{"user_id":"35cccccc-cccc-cccc-cccc-cccccccccccc"}'::jsonb;

    IF user_update_count <> 1 THEN
        RAISE EXCEPTION 'transactional user UPDATE audit event was not captured';
    END IF;

    SELECT count(*)
    INTO credential_count
    FROM identity_access.security_mutation_events
    WHERE identity_scope_id = '35111111-1111-1111-1111-111111111111'
      AND table_name = 'password_credentials'
      AND operation = 'INSERT'
      AND record_key @> '{"user_id":"35cccccc-cccc-cccc-cccc-cccccccccccc"}'::jsonb
      AND NOT (record_key ? 'password_hash')
      AND NOT (record_key ? 'login_identifier')
      AND NOT (record_key ? 'normalized_login_identifier');

    IF credential_count <> 1 THEN
        RAISE EXCEPTION 'credential mutation ledger record is missing or contains non-key credential data';
    END IF;
END
$$;

SAVEPOINT rollback_proof;

INSERT INTO identity_access.tenants
(identity_scope_id, tenant_id, display_name, status)
VALUES
('35111111-1111-1111-1111-111111111111',
 '35dddddd-dddd-dddd-dddd-dddddddddddd',
 'Rolled Back Tenant',
 1);

DO $$
BEGIN
    IF NOT EXISTS
    (
        SELECT 1
        FROM identity_access.security_mutation_events
        WHERE identity_scope_id = '35111111-1111-1111-1111-111111111111'
          AND table_name = 'tenants'
          AND operation = 'INSERT'
          AND record_key @> '{"tenant_id":"35dddddd-dddd-dddd-dddd-dddddddddddd"}'::jsonb
    ) THEN
        RAISE EXCEPTION 'tenant mutation audit event was not created before rollback proof';
    END IF;
END
$$;

ROLLBACK TO SAVEPOINT rollback_proof;

DO $$
BEGIN
    IF EXISTS
    (
        SELECT 1
        FROM identity_access.security_mutation_events
        WHERE identity_scope_id = '35111111-1111-1111-1111-111111111111'
          AND table_name = 'tenants'
          AND operation = 'INSERT'
          AND record_key @> '{"tenant_id":"35dddddd-dddd-dddd-dddd-dddddddddddd"}'::jsonb
    ) THEN
        RAISE EXCEPTION 'rolled-back tenant mutation retained an audit event';
    END IF;
END
$$;

DO $$
BEGIN
    BEGIN
        UPDATE identity_access.security_mutation_events
        SET correlation_id = 'tampered'
        WHERE identity_scope_id = '35111111-1111-1111-1111-111111111111'
          AND table_name = 'users'
          AND operation = 'INSERT';

        RAISE EXCEPTION 'append-only ledger unexpectedly accepted an update';
    EXCEPTION
        WHEN SQLSTATE '55000' THEN
            NULL;
    END;
END
$$;

ROLLBACK;
