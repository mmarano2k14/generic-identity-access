\set ON_ERROR_STOP on
BEGIN;

INSERT INTO identity_access.users
(identity_scope_id, user_id, display_name, status)
VALUES
(
    '38111111-1111-1111-1111-111111111111',
    '38aaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa',
    'OIDC Refresh User',
    1
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
VALUES
(
    '38111111-1111-1111-1111-111111111111',
    '38bbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb',
    '38aaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa',
    'web-client',
    'app-a',
    'app-a-primary',
    decode(repeat('51', 32), 'hex'),
    transaction_timestamp() - interval '1 minute',
    transaction_timestamp() + interval '1 hour',
    1,
    ARRAY['pwd']::text[],
    transaction_timestamp() - interval '1 minute'
);

INSERT INTO identity_access.oidc_refresh_tokens
(
    identity_scope_id,
    family_id,
    token_id,
    parent_token_id,
    sequence_number,
    token_hash,
    user_id,
    session_id,
    client_id,
    application_key,
    authentication_context_key,
    scope,
    authenticated_at,
    issued_at,
    expires_at,
    assurance_level,
    assurance_methods
)
SELECT
    '38111111-1111-1111-1111-111111111111',
    '38cccccc-cccc-cccc-cccc-cccccccccccc',
    '38dddddd-dddd-dddd-dddd-dddddddddddd',
    NULL,
    0,
    decode(repeat('52', 32), 'hex'),
    '38aaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa',
    '38bbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb',
    'web-client',
    'app-a',
    'app-a-primary',
    'openid',
    s.created_at,
    transaction_timestamp(),
    transaction_timestamp() + interval '30 days',
    s.assurance_level,
    s.assurance_methods
FROM identity_access.user_sessions AS s
JOIN identity_access.users AS u
  ON u.identity_scope_id = s.identity_scope_id
 AND u.user_id = s.user_id
WHERE s.identity_scope_id = '38111111-1111-1111-1111-111111111111'
  AND s.session_id = '38bbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb'
  AND s.revoked_at IS NULL
  AND s.expires_at > transaction_timestamp()
  AND u.status = 1;

DO $$
BEGIN
    IF NOT EXISTS
    (
        SELECT 1
        FROM identity_access.oidc_refresh_tokens
        WHERE identity_scope_id = '38111111-1111-1111-1111-111111111111'
          AND token_id = '38dddddd-dddd-dddd-dddd-dddddddddddd'
          AND octet_length(token_hash) = 32
          AND sequence_number = 0
          AND parent_token_id IS NULL
          AND consumed_at IS NULL
          AND revoked_at IS NULL
          AND assurance_level = 1
          AND assurance_methods = ARRAY['pwd']::text[]
    ) THEN
        RAISE EXCEPTION 'Initial OIDC refresh-token family member was not created';
    END IF;
END
$$;

UPDATE identity_access.oidc_refresh_tokens
SET consumed_at = transaction_timestamp()
WHERE identity_scope_id = '38111111-1111-1111-1111-111111111111'
  AND token_id = '38dddddd-dddd-dddd-dddd-dddddddddddd'
  AND consumed_at IS NULL
  AND revoked_at IS NULL;

INSERT INTO identity_access.oidc_refresh_tokens
(
    identity_scope_id,
    family_id,
    token_id,
    parent_token_id,
    sequence_number,
    token_hash,
    user_id,
    session_id,
    client_id,
    application_key,
    authentication_context_key,
    scope,
    authenticated_at,
    issued_at,
    expires_at,
    assurance_level,
    assurance_methods
)
SELECT
    identity_scope_id,
    family_id,
    '38eeeeee-eeee-eeee-eeee-eeeeeeeeeeee',
    token_id,
    sequence_number + 1,
    decode(repeat('53', 32), 'hex'),
    user_id,
    session_id,
    client_id,
    application_key,
    authentication_context_key,
    scope,
    authenticated_at,
    transaction_timestamp(),
    expires_at,
    assurance_level,
    assurance_methods
FROM identity_access.oidc_refresh_tokens
WHERE identity_scope_id = '38111111-1111-1111-1111-111111111111'
  AND token_id = '38dddddd-dddd-dddd-dddd-dddddddddddd'
  AND consumed_at IS NOT NULL
  AND revoked_at IS NULL;

DO $$
DECLARE
    initial_expiry timestamptz;
    replacement_expiry timestamptz;
BEGIN
    SELECT expires_at
    INTO initial_expiry
    FROM identity_access.oidc_refresh_tokens
    WHERE identity_scope_id = '38111111-1111-1111-1111-111111111111'
      AND token_id = '38dddddd-dddd-dddd-dddd-dddddddddddd';

    SELECT expires_at
    INTO replacement_expiry
    FROM identity_access.oidc_refresh_tokens
    WHERE identity_scope_id = '38111111-1111-1111-1111-111111111111'
      AND token_id = '38eeeeee-eeee-eeee-eeee-eeeeeeeeeeee'
      AND parent_token_id = '38dddddd-dddd-dddd-dddd-dddddddddddd'
      AND sequence_number = 1
      AND assurance_level = 1
      AND assurance_methods = ARRAY['pwd']::text[];

    IF replacement_expiry IS NULL OR replacement_expiry <> initial_expiry THEN
        RAISE EXCEPTION 'Refresh-token rotation did not preserve absolute family expiry';
    END IF;
END
$$;

UPDATE identity_access.oidc_refresh_tokens
SET revoked_at = transaction_timestamp(),
    revocation_reason = 'reuse_detected'
WHERE identity_scope_id = '38111111-1111-1111-1111-111111111111'
  AND family_id = '38cccccc-cccc-cccc-cccc-cccccccccccc'
  AND revoked_at IS NULL;

DO $$
BEGIN
    IF
    (
        SELECT count(*)
        FROM identity_access.oidc_refresh_tokens
        WHERE identity_scope_id = '38111111-1111-1111-1111-111111111111'
          AND family_id = '38cccccc-cccc-cccc-cccc-cccccccccccc'
          AND revoked_at IS NOT NULL
          AND revocation_reason = 'reuse_detected'
    ) <> 2 THEN
        RAISE EXCEPTION 'Consumed refresh-token replay did not revoke the complete family';
    END IF;

    IF EXISTS
    (
        SELECT 1
        FROM identity_access.security_mutation_events
        WHERE identity_scope_id = '38111111-1111-1111-1111-111111111111'
          AND table_name = 'oidc_refresh_tokens'
          AND
          (
              record_key ? 'token_hash'
              OR record_key ? 'family_id'
              OR record_key ? 'session_id'
          )
    ) THEN
        RAISE EXCEPTION 'Refresh-token transactional ledger key contains non-approved token metadata';
    END IF;

    IF
    (
        SELECT count(*)
        FROM identity_access.security_mutation_events
        WHERE identity_scope_id = '38111111-1111-1111-1111-111111111111'
          AND table_name = 'oidc_refresh_tokens'
          AND record_key ? 'token_id'
    ) < 4 THEN
        RAISE EXCEPTION 'Refresh-token transactional ledger events are missing';
    END IF;
END
$$;

UPDATE identity_access.user_sessions
SET revoked_at = transaction_timestamp()
WHERE identity_scope_id = '38111111-1111-1111-1111-111111111111'
  AND session_id = '38bbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb';

INSERT INTO identity_access.oidc_refresh_tokens
(
    identity_scope_id,
    family_id,
    token_id,
    parent_token_id,
    sequence_number,
    token_hash,
    user_id,
    session_id,
    client_id,
    application_key,
    authentication_context_key,
    scope,
    authenticated_at,
    issued_at,
    expires_at,
    assurance_level,
    assurance_methods
)
SELECT
    '38111111-1111-1111-1111-111111111111',
    '38ffffff-ffff-ffff-ffff-ffffffffffff',
    '38000000-0000-0000-0000-000000000001',
    NULL,
    0,
    decode(repeat('54', 32), 'hex'),
    '38aaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa',
    '38bbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb',
    'web-client',
    'app-a',
    'app-a-primary',
    'openid',
    s.created_at,
    transaction_timestamp(),
    transaction_timestamp() + interval '30 days',
    s.assurance_level,
    s.assurance_methods
FROM identity_access.user_sessions AS s
JOIN identity_access.users AS u
  ON u.identity_scope_id = s.identity_scope_id
 AND u.user_id = s.user_id
WHERE s.identity_scope_id = '38111111-1111-1111-1111-111111111111'
  AND s.session_id = '38bbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb'
  AND s.revoked_at IS NULL
  AND s.expires_at > transaction_timestamp()
  AND u.status = 1;

DO $$
BEGIN
    IF EXISTS
    (
        SELECT 1
        FROM identity_access.oidc_refresh_tokens
        WHERE identity_scope_id = '38111111-1111-1111-1111-111111111111'
          AND token_id = '38000000-0000-0000-0000-000000000001'
    ) THEN
        RAISE EXCEPTION 'Refresh-token family creation accepted a revoked local session';
    END IF;
END
$$;

ROLLBACK;