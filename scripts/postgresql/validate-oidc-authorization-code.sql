\set ON_ERROR_STOP on
BEGIN;

INSERT INTO identity_access.users
(identity_scope_id, user_id, display_name, status)
VALUES
('37111111-1111-1111-1111-111111111111',
 '37aaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa',
 'OIDC User',
 1);

INSERT INTO identity_access.user_sessions
(identity_scope_id, session_id, user_id, client_id, application_key,
 authentication_context_key, token_hash, created_at, expires_at)
VALUES
('37111111-1111-1111-1111-111111111111',
 '37bbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb',
 '37aaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa',
 'web-client',
 'app-a',
 'app-a-primary',
 decode(repeat('11', 32), 'hex'),
 transaction_timestamp() - interval '1 minute',
 transaction_timestamp() + interval '1 hour');

INSERT INTO identity_access.oidc_authorization_codes
(identity_scope_id, code_id, code_hash, user_id, session_id, client_id,
 application_key, authentication_context_key, redirect_uri, scope,
 code_challenge, code_challenge_method, nonce, authenticated_at, issued_at, expires_at)
SELECT
'37111111-1111-1111-1111-111111111111',
'37cccccc-cccc-cccc-cccc-cccccccccccc',
decode(repeat('22', 32), 'hex'),
'37aaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa',
'37bbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb',
'web-client',
'app-a',
'app-a-primary',
'https://client.example.test/callback',
'openid',
repeat('A', 43),
'S256',
'nonce-12345678',
s.created_at,
transaction_timestamp(),
transaction_timestamp() + interval '2 minutes'
FROM identity_access.user_sessions AS s
JOIN identity_access.users AS u
  ON u.identity_scope_id = s.identity_scope_id
 AND u.user_id = s.user_id
WHERE s.identity_scope_id = '37111111-1111-1111-1111-111111111111'
  AND s.session_id = '37bbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb'
  AND s.revoked_at IS NULL
  AND s.expires_at > transaction_timestamp()
  AND u.status = 1;

WITH consumed AS
(
    UPDATE identity_access.oidc_authorization_codes AS c
    SET consumed_at = transaction_timestamp()
    FROM identity_access.user_sessions AS s
    JOIN identity_access.users AS u
      ON u.identity_scope_id = s.identity_scope_id
     AND u.user_id = s.user_id
    WHERE c.identity_scope_id = '37111111-1111-1111-1111-111111111111'
      AND c.code_hash = decode(repeat('22', 32), 'hex')
      AND c.client_id = 'web-client'
      AND c.redirect_uri = 'https://client.example.test/callback'
      AND c.code_challenge = repeat('A', 43)
      AND c.code_challenge_method = 'S256'
      AND c.application_key = 'app-a'
      AND c.consumed_at IS NULL
      AND c.expires_at > transaction_timestamp()
      AND s.identity_scope_id = c.identity_scope_id
      AND s.session_id = c.session_id
      AND s.user_id = c.user_id
      AND s.client_id = c.client_id
      AND s.application_key = c.application_key
      AND s.authentication_context_key = c.authentication_context_key
      AND s.revoked_at IS NULL
      AND s.expires_at > transaction_timestamp()
      AND u.status = 1
    RETURNING c.code_id
)
SELECT count(*) AS first_consume_count
FROM consumed;

DO $$
BEGIN
    IF NOT EXISTS
    (
        SELECT 1
        FROM identity_access.oidc_authorization_codes
        WHERE identity_scope_id = '37111111-1111-1111-1111-111111111111'
          AND code_id = '37cccccc-cccc-cccc-cccc-cccccccccccc'
          AND consumed_at IS NOT NULL
    ) THEN
        RAISE EXCEPTION 'OIDC authorization code was not consumed';
    END IF;
END
$$;

WITH replay AS
(
    UPDATE identity_access.oidc_authorization_codes
    SET consumed_at = transaction_timestamp()
    WHERE identity_scope_id = '37111111-1111-1111-1111-111111111111'
      AND code_id = '37cccccc-cccc-cccc-cccc-cccccccccccc'
      AND consumed_at IS NULL
    RETURNING code_id
)
SELECT count(*) AS replay_count
FROM replay;

DO $$
BEGIN
    IF
    (
        SELECT count(*)
        FROM identity_access.security_mutation_events
        WHERE identity_scope_id = '37111111-1111-1111-1111-111111111111'
          AND table_name = 'oidc_authorization_codes'
          AND record_key @> '{"code_id":"37cccccc-cccc-cccc-cccc-cccccccccccc"}'::jsonb
    ) < 2 THEN
        RAISE EXCEPTION 'OIDC authorization-code transactional ledger events are missing';
    END IF;

    IF EXISTS
    (
        SELECT 1
        FROM identity_access.security_mutation_events
        WHERE identity_scope_id = '37111111-1111-1111-1111-111111111111'
          AND table_name = 'oidc_authorization_codes'
          AND
          (
              record_key ? 'code_hash'
              OR record_key ? 'code_challenge'
              OR record_key ? 'nonce'
          )
    ) THEN
        RAISE EXCEPTION 'OIDC authorization-code ledger key contains secret or protocol payload';
    END IF;
END
$$;

INSERT INTO identity_access.oidc_authorization_codes
(identity_scope_id, code_id, code_hash, user_id, session_id, client_id,
 application_key, authentication_context_key, redirect_uri, scope,
 code_challenge, code_challenge_method, nonce, authenticated_at, issued_at, expires_at)
SELECT
'37111111-1111-1111-1111-111111111111',
'37dddddd-dddd-dddd-dddd-dddddddddddd',
decode(repeat('33', 32), 'hex'),
'37aaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa',
'37bbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb',
'web-client',
'app-a',
'app-a-primary',
'https://client.example.test/callback',
'openid',
repeat('B', 43),
'S256',
'nonce-87654321',
s.created_at,
transaction_timestamp(),
transaction_timestamp() + interval '2 minutes'
FROM identity_access.user_sessions AS s
JOIN identity_access.users AS u
  ON u.identity_scope_id = s.identity_scope_id
 AND u.user_id = s.user_id
WHERE s.identity_scope_id = '37111111-1111-1111-1111-111111111111'
  AND s.session_id = '37bbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb'
  AND s.revoked_at IS NULL
  AND s.expires_at > transaction_timestamp()
  AND u.status = 1;

UPDATE identity_access.user_sessions
SET revoked_at = transaction_timestamp()
WHERE identity_scope_id = '37111111-1111-1111-1111-111111111111'
  AND session_id = '37bbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb';


INSERT INTO identity_access.oidc_authorization_codes
(identity_scope_id, code_id, code_hash, user_id, session_id, client_id,
 application_key, authentication_context_key, redirect_uri, scope,
 code_challenge, code_challenge_method, nonce, authenticated_at, issued_at, expires_at)
SELECT
'37111111-1111-1111-1111-111111111111',
'37eeeeee-eeee-eeee-eeee-eeeeeeeeeeee',
decode(repeat('44', 32), 'hex'),
'37aaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa',
'37bbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb',
'web-client',
'app-a',
'app-a-primary',
'https://client.example.test/callback',
'openid',
repeat('C', 43),
'S256',
'nonce-11223344',
s.created_at,
transaction_timestamp(),
transaction_timestamp() + interval '2 minutes'
FROM identity_access.user_sessions AS s
JOIN identity_access.users AS u
  ON u.identity_scope_id = s.identity_scope_id
 AND u.user_id = s.user_id
WHERE s.identity_scope_id = '37111111-1111-1111-1111-111111111111'
  AND s.session_id = '37bbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb'
  AND s.revoked_at IS NULL
  AND s.expires_at > transaction_timestamp()
  AND u.status = 1;

DO $$
BEGIN
    IF EXISTS
    (
        SELECT 1
        FROM identity_access.oidc_authorization_codes
        WHERE identity_scope_id = '37111111-1111-1111-1111-111111111111'
          AND code_id = '37eeeeee-eeee-eeee-eeee-eeeeeeeeeeee'
    ) THEN
        RAISE EXCEPTION 'OIDC code issuance accepted a revoked source session';
    END IF;
END
$$;

WITH consumed_revoked AS
(
    UPDATE identity_access.oidc_authorization_codes AS c
    SET consumed_at = transaction_timestamp()
    FROM identity_access.user_sessions AS s
    JOIN identity_access.users AS u
      ON u.identity_scope_id = s.identity_scope_id
     AND u.user_id = s.user_id
    WHERE c.identity_scope_id = '37111111-1111-1111-1111-111111111111'
      AND c.code_id = '37dddddd-dddd-dddd-dddd-dddddddddddd'
      AND c.code_challenge = repeat('B', 43)
      AND c.consumed_at IS NULL
      AND s.identity_scope_id = c.identity_scope_id
      AND s.session_id = c.session_id
      AND s.revoked_at IS NULL
      AND s.expires_at > transaction_timestamp()
      AND u.status = 1
    RETURNING c.code_id
)
SELECT count(*) AS revoked_session_consume_count
FROM consumed_revoked;

DO $$
BEGIN
    IF EXISTS
    (
        SELECT 1
        FROM identity_access.oidc_authorization_codes
        WHERE identity_scope_id = '37111111-1111-1111-1111-111111111111'
          AND code_id = '37dddddd-dddd-dddd-dddd-dddddddddddd'
          AND consumed_at IS NOT NULL
    ) THEN
        RAISE EXCEPTION 'OIDC code was consumed after source session revocation';
    END IF;
END
$$;

ROLLBACK;
