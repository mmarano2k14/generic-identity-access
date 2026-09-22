CREATE TABLE IF NOT EXISTS identity_access.password_credentials
(
    identity_scope_id uuid NOT NULL,
    user_id uuid NOT NULL,
    login_identifier varchar(320) NOT NULL,
    normalized_login_identifier varchar(320) NOT NULL,
    password_hash text NOT NULL,
    failed_access_count integer NOT NULL DEFAULT 0,
    lockout_until timestamptz NULL,
    row_version bigint NOT NULL DEFAULT 1,
    created_at timestamptz NOT NULL DEFAULT transaction_timestamp(),
    updated_at timestamptz NOT NULL DEFAULT transaction_timestamp(),
    CONSTRAINT pk_password_credentials PRIMARY KEY (identity_scope_id, user_id),
    CONSTRAINT uq_password_credentials_login UNIQUE (identity_scope_id, normalized_login_identifier),
    CONSTRAINT fk_password_credentials_user FOREIGN KEY (identity_scope_id, user_id)
        REFERENCES identity_access.users (identity_scope_id, user_id) ON DELETE RESTRICT,
    CONSTRAINT ck_password_credentials_failed_access CHECK (failed_access_count >= 0),
    CONSTRAINT ck_password_credentials_row_version CHECK (row_version > 0),
    CONSTRAINT ck_password_credentials_hash_not_blank CHECK (length(password_hash) > 0)
);

CREATE TABLE IF NOT EXISTS identity_access.user_sessions
(
    identity_scope_id uuid NOT NULL,
    session_id uuid NOT NULL,
    user_id uuid NOT NULL,
    client_id varchar(128) NOT NULL,
    application_key varchar(64) NOT NULL,
    authentication_context_key varchar(64) NOT NULL,
    token_hash bytea NOT NULL,
    created_at timestamptz NOT NULL,
    expires_at timestamptz NOT NULL,
    revoked_at timestamptz NULL,
    CONSTRAINT pk_user_sessions PRIMARY KEY (identity_scope_id, session_id),
    CONSTRAINT uq_user_sessions_token_hash UNIQUE (token_hash),
    CONSTRAINT fk_user_sessions_user FOREIGN KEY (identity_scope_id, user_id)
        REFERENCES identity_access.users (identity_scope_id, user_id) ON DELETE RESTRICT,
    CONSTRAINT ck_user_sessions_application_key CHECK (application_key ~ '^[a-z][a-z0-9-]{0,63}$'),
    CONSTRAINT ck_user_sessions_client_id CHECK (client_id ~ '^[a-z][a-z0-9_-]{0,127}$'),
    CONSTRAINT ck_user_sessions_context_key CHECK (authentication_context_key ~ '^[a-z][a-z0-9-]{0,63}$'),
    CONSTRAINT ck_user_sessions_token_hash CHECK (octet_length(token_hash) = 32),
    CONSTRAINT ck_user_sessions_expiry CHECK (expires_at > created_at)
);

CREATE INDEX IF NOT EXISTS ix_user_sessions_subject_active
    ON identity_access.user_sessions (identity_scope_id, user_id, expires_at)
    WHERE revoked_at IS NULL;

CREATE INDEX IF NOT EXISTS ix_user_sessions_client_active
    ON identity_access.user_sessions (identity_scope_id, client_id, expires_at)
    WHERE revoked_at IS NULL;
