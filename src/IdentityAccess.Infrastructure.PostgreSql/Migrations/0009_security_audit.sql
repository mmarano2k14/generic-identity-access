CREATE TABLE IF NOT EXISTS identity_access.security_events
(
    event_id uuid PRIMARY KEY,
    occurred_at timestamptz NOT NULL DEFAULT transaction_timestamp(),
    event_type varchar(80) NOT NULL,
    outcome varchar(32) NOT NULL,
    identity_scope_id uuid NOT NULL,
    tenant_id uuid NULL,
    user_id uuid NULL,
    application_key varchar(64) NULL,
    client_id varchar(128) NULL,
    target_id varchar(128) NULL,
    reason_code varchar(80) NULL,
    correlation_id varchar(64) NULL
);

CREATE INDEX IF NOT EXISTS ix_security_events_scope_time
    ON identity_access.security_events(identity_scope_id, occurred_at DESC);

CREATE INDEX IF NOT EXISTS ix_security_events_user_time
    ON identity_access.security_events(identity_scope_id, user_id, occurred_at DESC)
    WHERE user_id IS NOT NULL;

CREATE INDEX IF NOT EXISTS ix_security_events_correlation
    ON identity_access.security_events(correlation_id)
    WHERE correlation_id IS NOT NULL;
