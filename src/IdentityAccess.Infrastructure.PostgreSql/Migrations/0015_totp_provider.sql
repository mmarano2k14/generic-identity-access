CREATE TABLE identity_access.totp_authenticators
(
    identity_scope_id uuid NOT NULL,
    authenticator_id uuid NOT NULL,
    protected_secret bytea NOT NULL,
    algorithm varchar(16) NOT NULL,
    digits smallint NOT NULL,
    period_seconds smallint NOT NULL,
    last_accepted_time_step bigint NULL,
    row_version bigint NOT NULL DEFAULT 1,
    created_at timestamptz NOT NULL DEFAULT transaction_timestamp(),
    updated_at timestamptz NOT NULL DEFAULT transaction_timestamp(),
    CONSTRAINT pk_totp_authenticators PRIMARY KEY
        (identity_scope_id, authenticator_id),
    CONSTRAINT fk_totp_authenticators_generic FOREIGN KEY
        (identity_scope_id, authenticator_id)
        REFERENCES identity_access.user_authenticators
        (identity_scope_id, authenticator_id)
        ON DELETE CASCADE,
    CONSTRAINT ck_totp_authenticators_protected_secret
        CHECK (octet_length(protected_secret) > 0),
    CONSTRAINT ck_totp_authenticators_algorithm
        CHECK (algorithm = 'SHA1'),
    CONSTRAINT ck_totp_authenticators_digits
        CHECK (digits = 6),
    CONSTRAINT ck_totp_authenticators_period
        CHECK (period_seconds = 30),
    CONSTRAINT ck_totp_authenticators_last_time_step
        CHECK (last_accepted_time_step IS NULL OR last_accepted_time_step >= 0),
    CONSTRAINT ck_totp_authenticators_row_version
        CHECK (row_version > 0)
);

CREATE INDEX ix_totp_authenticators_last_accepted_time_step
    ON identity_access.totp_authenticators
       (identity_scope_id, authenticator_id, last_accepted_time_step);

CREATE TRIGGER trg_security_mutation_totp_authenticators
AFTER INSERT OR UPDATE OR DELETE
ON identity_access.totp_authenticators
FOR EACH ROW EXECUTE FUNCTION identity_access.capture_security_mutation(
    'identity_scope_id',
    'authenticator_id');
