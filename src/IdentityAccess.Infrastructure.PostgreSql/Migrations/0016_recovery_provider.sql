CREATE TABLE identity_access.recovery_code_sets
(
    identity_scope_id uuid NOT NULL,
    authenticator_id uuid NOT NULL,
    hash_algorithm varchar(16) NOT NULL,
    code_count smallint NOT NULL,
    row_version bigint NOT NULL DEFAULT 1,
    created_at timestamptz NOT NULL DEFAULT transaction_timestamp(),
    updated_at timestamptz NOT NULL DEFAULT transaction_timestamp(),
    CONSTRAINT pk_recovery_code_sets PRIMARY KEY
        (identity_scope_id, authenticator_id),
    CONSTRAINT fk_recovery_code_sets_generic FOREIGN KEY
        (identity_scope_id, authenticator_id)
        REFERENCES identity_access.user_authenticators
        (identity_scope_id, authenticator_id)
        ON DELETE CASCADE,
    CONSTRAINT ck_recovery_code_sets_hash_algorithm
        CHECK (hash_algorithm = 'SHA256'),
    CONSTRAINT ck_recovery_code_sets_code_count
        CHECK (code_count BETWEEN 6 AND 20),
    CONSTRAINT ck_recovery_code_sets_row_version
        CHECK (row_version > 0)
);

CREATE TABLE identity_access.recovery_codes
(
    identity_scope_id uuid NOT NULL,
    authenticator_id uuid NOT NULL,
    code_id uuid NOT NULL,
    code_hash bytea NOT NULL,
    consumed_at timestamptz NULL,
    created_at timestamptz NOT NULL DEFAULT transaction_timestamp(),
    CONSTRAINT pk_recovery_codes PRIMARY KEY
        (identity_scope_id, authenticator_id, code_id),
    CONSTRAINT uq_recovery_codes_hash UNIQUE
        (identity_scope_id, authenticator_id, code_hash),
    CONSTRAINT fk_recovery_codes_set FOREIGN KEY
        (identity_scope_id, authenticator_id)
        REFERENCES identity_access.recovery_code_sets
        (identity_scope_id, authenticator_id)
        ON DELETE CASCADE,
    CONSTRAINT ck_recovery_codes_hash
        CHECK (octet_length(code_hash) = 32),
    CONSTRAINT ck_recovery_codes_consumed_at
        CHECK (consumed_at IS NULL OR consumed_at >= created_at)
);

CREATE UNIQUE INDEX uq_user_authenticators_active_recovery
    ON identity_access.user_authenticators
       (identity_scope_id, user_id)
    WHERE provider_key = 'recovery' AND status = 2;

CREATE INDEX ix_recovery_codes_available
    ON identity_access.recovery_codes
       (identity_scope_id, authenticator_id)
    WHERE consumed_at IS NULL;

CREATE TRIGGER trg_security_mutation_recovery_code_sets
AFTER INSERT OR UPDATE OR DELETE
ON identity_access.recovery_code_sets
FOR EACH ROW EXECUTE FUNCTION identity_access.capture_security_mutation(
    'identity_scope_id',
    'authenticator_id');

CREATE TRIGGER trg_security_mutation_recovery_codes
AFTER INSERT OR UPDATE OR DELETE
ON identity_access.recovery_codes
FOR EACH ROW EXECUTE FUNCTION identity_access.capture_security_mutation(
    'identity_scope_id',
    'authenticator_id',
    'code_id');
