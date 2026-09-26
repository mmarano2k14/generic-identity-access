\set ON_ERROR_STOP on
BEGIN;

INSERT INTO identity_access.users
(identity_scope_id, user_id, display_name, status)
VALUES
(
    '30111111-1111-1111-1111-111111111111',
    '30aaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa',
    'Session User',
    1
);

INSERT INTO identity_access.password_credentials
(identity_scope_id, user_id, login_identifier, normalized_login_identifier, password_hash)
VALUES
(
    '30111111-1111-1111-1111-111111111111',
    '30aaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa',
    'session@example.test',
    'SESSION@EXAMPLE.TEST',
    'fixture-hash'
);

INSERT INTO identity_access.user_sessions
(
    identity_scope_id,
    session_id,
    user_id,
    client_id,
    application_key,
    authentication_context_key,
    token_hash,
    created_at,
    expires_at,
    assurance_level,
    assurance_methods,
    assurance_verified_at
)
SELECT
    '30111111-1111-1111-1111-111111111111',
    '30bbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb',
    '30aaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa',
    'web-client',
    'app-a',
    'app-a-primary',
    decode(repeat('11', 32), 'hex'),
    transaction_timestamp(),
    transaction_timestamp() + interval '1 hour',
    1,
    ARRAY['pwd']::text[],
    transaction_timestamp()
FROM identity_access.users
WHERE identity_scope_id = '30111111-1111-1111-1111-111111111111'
  AND user_id = '30aaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa'
  AND status = 1;

DO $$
BEGIN
    IF NOT EXISTS
    (
        SELECT 1
        FROM identity_access.user_sessions AS s
        INNER JOIN identity_access.users AS u
            ON u.identity_scope_id = s.identity_scope_id
           AND u.user_id = s.user_id
        WHERE s.identity_scope_id = '30111111-1111-1111-1111-111111111111'
          AND s.session_id = '30bbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb'
          AND s.revoked_at IS NULL
          AND u.status = 1
    ) THEN
        RAISE EXCEPTION 'active-user session was not visible';
    END IF;
END
$$;

WITH updated AS
(
    UPDATE identity_access.users
    SET status = 2,
        row_version = row_version + 1,
        updated_at = transaction_timestamp()
    WHERE identity_scope_id = '30111111-1111-1111-1111-111111111111'
      AND user_id = '30aaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa'
    RETURNING 1
),
revoked AS
(
    UPDATE identity_access.user_sessions
    SET revoked_at = transaction_timestamp()
    WHERE identity_scope_id = '30111111-1111-1111-1111-111111111111'
      AND user_id = '30aaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa'
      AND revoked_at IS NULL
      AND EXISTS (SELECT 1 FROM updated)
    RETURNING 1
)
SELECT count(*) FROM revoked;

DO $$
BEGIN
    IF EXISTS
    (
        SELECT 1
        FROM identity_access.user_sessions AS s
        INNER JOIN identity_access.users AS u
            ON u.identity_scope_id = s.identity_scope_id
           AND u.user_id = s.user_id
        WHERE s.identity_scope_id = '30111111-1111-1111-1111-111111111111'
          AND s.session_id = '30bbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb'
          AND s.revoked_at IS NULL
          AND u.status = 1
    ) THEN
        RAISE EXCEPTION 'suspended user retained a valid session';
    END IF;
END
$$;

INSERT INTO identity_access.user_sessions
(
    identity_scope_id,
    session_id,
    user_id,
    client_id,
    application_key,
    authentication_context_key,
    token_hash,
    created_at,
    expires_at,
    assurance_level,
    assurance_methods,
    assurance_verified_at
)
SELECT
    '30111111-1111-1111-1111-111111111111',
    '30cccccc-cccc-cccc-cccc-cccccccccccc',
    '30aaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa',
    'web-client',
    'app-a',
    'app-a-primary',
    decode(repeat('22', 32), 'hex'),
    transaction_timestamp(),
    transaction_timestamp() + interval '1 hour',
    1,
    ARRAY['pwd']::text[],
    transaction_timestamp()
FROM identity_access.users
WHERE identity_scope_id = '30111111-1111-1111-1111-111111111111'
  AND user_id = '30aaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa'
  AND status = 1;

DO $$
BEGIN
    IF EXISTS
    (
        SELECT 1
        FROM identity_access.user_sessions
        WHERE identity_scope_id = '30111111-1111-1111-1111-111111111111'
          AND session_id = '30cccccc-cccc-cccc-cccc-cccccccccccc'
    ) THEN
        RAISE EXCEPTION 'session issuance accepted a suspended user';
    END IF;
END
$$;

UPDATE identity_access.users
SET status = 1,
    row_version = row_version + 1,
    updated_at = transaction_timestamp()
WHERE identity_scope_id = '30111111-1111-1111-1111-111111111111'
  AND user_id = '30aaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa';

INSERT INTO identity_access.user_sessions
(
    identity_scope_id,
    session_id,
    user_id,
    client_id,
    application_key,
    authentication_context_key,
    token_hash,
    created_at,
    expires_at,
    assurance_level,
    assurance_methods,
    assurance_verified_at
)
VALUES
(
    '30111111-1111-1111-1111-111111111111',
    '30dddddd-dddd-dddd-dddd-dddddddddddd',
    '30aaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa',
    'web-client',
    'app-a',
    'app-a-primary',
    decode(repeat('33', 32), 'hex'),
    transaction_timestamp(),
    transaction_timestamp() + interval '1 hour',
    1,
    ARRAY['pwd']::text[],
    transaction_timestamp()
);

WITH subject AS
(
    SELECT 1
    FROM identity_access.users
    WHERE identity_scope_id = '30111111-1111-1111-1111-111111111111'
      AND user_id = '30aaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa'
),
updated AS
(
    UPDATE identity_access.password_credentials
    SET password_hash = 'changed-hash',
        row_version = row_version + 1,
        updated_at = transaction_timestamp()
    WHERE identity_scope_id = '30111111-1111-1111-1111-111111111111'
      AND user_id = '30aaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa'
      AND row_version = 1
      AND EXISTS (SELECT 1 FROM subject)
    RETURNING row_version
),
revoked AS
(
    UPDATE identity_access.user_sessions
    SET revoked_at = transaction_timestamp()
    WHERE identity_scope_id = '30111111-1111-1111-1111-111111111111'
      AND user_id = '30aaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa'
      AND revoked_at IS NULL
      AND EXISTS (SELECT 1 FROM updated)
    RETURNING 1
)
SELECT count(*) FROM revoked;

DO $$
BEGIN
    IF EXISTS
    (
        SELECT 1
        FROM identity_access.user_sessions
        WHERE identity_scope_id = '30111111-1111-1111-1111-111111111111'
          AND user_id = '30aaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa'
          AND revoked_at IS NULL
    ) THEN
        RAISE EXCEPTION 'password change did not revoke subject sessions';
    END IF;
END
$$;

ROLLBACK;