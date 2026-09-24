CREATE TABLE identity_access.webauthn_authentication_challenges
(
    identity_scope_id uuid NOT NULL,
    challenge_id uuid NOT NULL,
    user_id uuid NOT NULL,
    application_key varchar(64) NOT NULL,
    challenge_hash bytea NOT NULL,
    expires_at timestamptz NOT NULL,
    consumed_at timestamptz NULL,
    created_at timestamptz NOT NULL DEFAULT transaction_timestamp(),
    updated_at timestamptz NOT NULL DEFAULT transaction_timestamp(),
    CONSTRAINT pk_webauthn_authentication_challenges PRIMARY KEY
        (identity_scope_id, challenge_id),
    CONSTRAINT fk_webauthn_authentication_challenges_user FOREIGN KEY
        (identity_scope_id, user_id)
        REFERENCES identity_access.users
        (identity_scope_id, user_id)
        ON DELETE CASCADE,
    CONSTRAINT ck_webauthn_authentication_application_key
        CHECK (application_key ~ '^[a-z][a-z0-9-]{0,63}$'),
    CONSTRAINT ck_webauthn_authentication_challenge_hash
        CHECK (octet_length(challenge_hash) = 32),
    CONSTRAINT ck_webauthn_authentication_expiry
        CHECK (expires_at > created_at),
    CONSTRAINT ck_webauthn_authentication_consumed
        CHECK (consumed_at IS NULL OR consumed_at >= created_at)
);

CREATE INDEX ix_webauthn_authentication_user
    ON identity_access.webauthn_authentication_challenges
       (identity_scope_id, user_id, expires_at)
    WHERE consumed_at IS NULL;

CREATE TRIGGER trg_security_mutation_webauthn_authentication_challenges
AFTER INSERT OR UPDATE OR DELETE
ON identity_access.webauthn_authentication_challenges
FOR EACH ROW EXECUTE FUNCTION identity_access.capture_security_mutation(
    'identity_scope_id',
    'challenge_id');
