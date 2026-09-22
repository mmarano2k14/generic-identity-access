CREATE TABLE identity_access.security_mutation_events
(
    mutation_event_id bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    occurred_at timestamptz NOT NULL DEFAULT transaction_timestamp(),
    transaction_id text NOT NULL DEFAULT pg_current_xact_id()::text,
    identity_scope_id uuid NOT NULL,
    application_key varchar(64) NULL,
    table_name varchar(96) NOT NULL,
    operation varchar(8) NOT NULL,
    record_key jsonb NOT NULL,
    actor_identity_scope_id uuid NULL,
    actor_user_id uuid NULL,
    actor_session_id uuid NULL,
    actor_client_id varchar(128) NULL,
    actor_application_key varchar(64) NULL,
    authentication_context_key varchar(64) NULL,
    correlation_id varchar(64) NULL,
    database_role varchar(128) NOT NULL DEFAULT current_user,
    CONSTRAINT ck_security_mutation_events_operation
        CHECK (operation IN ('INSERT', 'UPDATE', 'DELETE')),
    CONSTRAINT ck_security_mutation_events_record_key_object
        CHECK (jsonb_typeof(record_key) = 'object')
);

CREATE INDEX ix_security_mutation_events_scope_time
    ON identity_access.security_mutation_events
       (identity_scope_id, occurred_at DESC);

CREATE INDEX ix_security_mutation_events_actor_time
    ON identity_access.security_mutation_events
       (actor_identity_scope_id, actor_user_id, occurred_at DESC)
    WHERE actor_user_id IS NOT NULL;

CREATE INDEX ix_security_mutation_events_correlation
    ON identity_access.security_mutation_events(correlation_id)
    WHERE correlation_id IS NOT NULL;

CREATE INDEX ix_security_mutation_events_transaction
    ON identity_access.security_mutation_events(transaction_id);

CREATE INDEX ix_security_mutation_events_table_time
    ON identity_access.security_mutation_events(table_name, occurred_at DESC);

CREATE OR REPLACE FUNCTION identity_access.capture_security_mutation()
RETURNS trigger
LANGUAGE plpgsql
AS $$
DECLARE
    row_data jsonb;
    key_data jsonb := '{}'::jsonb;
    key_name text;
    scope_text text;
    application_text text;
    actor_scope_text text;
    actor_user_text text;
    actor_session_text text;
BEGIN
    row_data :=
        CASE
            WHEN TG_OP = 'DELETE' THEN to_jsonb(OLD)
            ELSE to_jsonb(NEW)
        END;

    FOREACH key_name IN ARRAY TG_ARGV
    LOOP
        IF NOT (row_data ? key_name) THEN
            RAISE EXCEPTION USING
                ERRCODE = '23514',
                MESSAGE = format(
                    'security mutation trigger key column %s is unavailable on %.%',
                    key_name,
                    TG_TABLE_SCHEMA,
                    TG_TABLE_NAME);
        END IF;

        key_data :=
            key_data ||
            jsonb_build_object(
                key_name,
                row_data -> key_name);
    END LOOP;

    scope_text :=
        row_data ->> 'identity_scope_id';

    IF scope_text IS NULL OR scope_text = '' THEN
        RAISE EXCEPTION USING
            ERRCODE = '23514',
            MESSAGE = format(
                'security mutation source %.% has no identity_scope_id',
                TG_TABLE_SCHEMA,
                TG_TABLE_NAME);
    END IF;

    application_text :=
        NULLIF(
            row_data ->> 'application_key',
            '');

    actor_scope_text :=
        NULLIF(
            current_setting(
                'identity_access.actor_identity_scope_id',
                true),
            '');

    actor_user_text :=
        NULLIF(
            current_setting(
                'identity_access.actor_user_id',
                true),
            '');

    actor_session_text :=
        NULLIF(
            current_setting(
                'identity_access.actor_session_id',
                true),
            '');

    INSERT INTO identity_access.security_mutation_events
    (
        identity_scope_id,
        application_key,
        table_name,
        operation,
        record_key,
        actor_identity_scope_id,
        actor_user_id,
        actor_session_id,
        actor_client_id,
        actor_application_key,
        authentication_context_key,
        correlation_id
    )
    VALUES
    (
        scope_text::uuid,
        application_text,
        TG_TABLE_NAME,
        TG_OP,
        key_data,
        actor_scope_text::uuid,
        actor_user_text::uuid,
        actor_session_text::uuid,
        NULLIF(
            current_setting(
                'identity_access.actor_client_id',
                true),
            ''),
        NULLIF(
            current_setting(
                'identity_access.actor_application_key',
                true),
            ''),
        NULLIF(
            current_setting(
                'identity_access.authentication_context_key',
                true),
            ''),
        NULLIF(
            current_setting(
                'identity_access.correlation_id',
                true),
            '')
    );

    IF TG_OP = 'DELETE' THEN
        RETURN OLD;
    END IF;

    RETURN NEW;
END
$$;

CREATE OR REPLACE FUNCTION identity_access.reject_security_mutation_event_change()
RETURNS trigger
LANGUAGE plpgsql
AS $$
BEGIN
    RAISE EXCEPTION USING
        ERRCODE = '55000',
        MESSAGE = 'security mutation events are append-only';
END
$$;

CREATE TRIGGER trg_security_mutation_events_append_only
BEFORE UPDATE OR DELETE
ON identity_access.security_mutation_events
FOR EACH ROW
EXECUTE FUNCTION identity_access.reject_security_mutation_event_change();

CREATE TRIGGER trg_security_mutation_users
AFTER INSERT OR UPDATE OR DELETE ON identity_access.users
FOR EACH ROW EXECUTE FUNCTION identity_access.capture_security_mutation(
    'identity_scope_id',
    'user_id');

CREATE TRIGGER trg_security_mutation_tenants
AFTER INSERT OR UPDATE OR DELETE ON identity_access.tenants
FOR EACH ROW EXECUTE FUNCTION identity_access.capture_security_mutation(
    'identity_scope_id',
    'tenant_id');

CREATE TRIGGER trg_security_mutation_tenant_memberships
AFTER INSERT OR UPDATE OR DELETE ON identity_access.tenant_memberships
FOR EACH ROW EXECUTE FUNCTION identity_access.capture_security_mutation(
    'identity_scope_id',
    'membership_id');

CREATE TRIGGER trg_security_mutation_user_groups
AFTER INSERT OR UPDATE OR DELETE ON identity_access.user_groups
FOR EACH ROW EXECUTE FUNCTION identity_access.capture_security_mutation(
    'identity_scope_id',
    'tenant_id',
    'application_key',
    'group_id');

CREATE TRIGGER trg_security_mutation_group_memberships
AFTER INSERT OR UPDATE OR DELETE ON identity_access.group_memberships
FOR EACH ROW EXECUTE FUNCTION identity_access.capture_security_mutation(
    'identity_scope_id',
    'tenant_id',
    'application_key',
    'group_id',
    'tenant_membership_id');

CREATE TRIGGER trg_security_mutation_application_security_models
AFTER INSERT OR UPDATE OR DELETE ON identity_access.application_security_models
FOR EACH ROW EXECUTE FUNCTION identity_access.capture_security_mutation(
    'identity_scope_id',
    'application_key',
    'model_version');

CREATE TRIGGER trg_security_mutation_application_capabilities
AFTER INSERT OR UPDATE OR DELETE ON identity_access.application_capabilities
FOR EACH ROW EXECUTE FUNCTION identity_access.capture_security_mutation(
    'identity_scope_id',
    'application_key',
    'model_version',
    'capability_resource',
    'capability_feature',
    'capability_action');

CREATE TRIGGER trg_security_mutation_permission_policies
AFTER INSERT OR UPDATE OR DELETE ON identity_access.permission_policies
FOR EACH ROW EXECUTE FUNCTION identity_access.capture_security_mutation(
    'identity_scope_id',
    'tenant_id',
    'application_key',
    'policy_id');

CREATE TRIGGER trg_security_mutation_policy_statements
AFTER INSERT OR UPDATE OR DELETE ON identity_access.policy_statements
FOR EACH ROW EXECUTE FUNCTION identity_access.capture_security_mutation(
    'identity_scope_id',
    'tenant_id',
    'application_key',
    'policy_id',
    'statement_id');

CREATE TRIGGER trg_security_mutation_group_policy_bindings
AFTER INSERT OR UPDATE OR DELETE ON identity_access.group_policy_bindings
FOR EACH ROW EXECUTE FUNCTION identity_access.capture_security_mutation(
    'identity_scope_id',
    'tenant_id',
    'application_key',
    'group_id',
    'policy_id',
    'binding_target_key');

CREATE TRIGGER trg_security_mutation_password_credentials
AFTER INSERT OR UPDATE OR DELETE ON identity_access.password_credentials
FOR EACH ROW EXECUTE FUNCTION identity_access.capture_security_mutation(
    'identity_scope_id',
    'user_id');

CREATE TRIGGER trg_security_mutation_user_sessions
AFTER INSERT OR UPDATE OR DELETE ON identity_access.user_sessions
FOR EACH ROW EXECUTE FUNCTION identity_access.capture_security_mutation(
    'identity_scope_id',
    'session_id');

CREATE TRIGGER trg_security_mutation_application_scope_types
AFTER INSERT OR UPDATE OR DELETE ON identity_access.application_scope_types
FOR EACH ROW EXECUTE FUNCTION identity_access.capture_security_mutation(
    'identity_scope_id',
    'application_key',
    'model_version',
    'scope_type_key');

CREATE TRIGGER trg_security_mutation_resource_scopes
AFTER INSERT OR UPDATE OR DELETE ON identity_access.resource_scopes
FOR EACH ROW EXECUTE FUNCTION identity_access.capture_security_mutation(
    'identity_scope_id',
    'tenant_id',
    'application_key',
    'resource_scope_id');

CREATE TRIGGER trg_security_mutation_scope_administration_groups
AFTER INSERT OR UPDATE OR DELETE
ON identity_access.identity_scope_administration_groups
FOR EACH ROW EXECUTE FUNCTION identity_access.capture_security_mutation(
    'identity_scope_id',
    'application_key',
    'group_id');

CREATE TRIGGER trg_security_mutation_scope_administration_memberships
AFTER INSERT OR UPDATE OR DELETE
ON identity_access.identity_scope_administration_group_memberships
FOR EACH ROW EXECUTE FUNCTION identity_access.capture_security_mutation(
    'identity_scope_id',
    'application_key',
    'group_id',
    'user_id');

CREATE TRIGGER trg_security_mutation_scope_administration_policies
AFTER INSERT OR UPDATE OR DELETE
ON identity_access.identity_scope_administration_policies
FOR EACH ROW EXECUTE FUNCTION identity_access.capture_security_mutation(
    'identity_scope_id',
    'application_key',
    'policy_id');

CREATE TRIGGER trg_security_mutation_scope_administration_statements
AFTER INSERT OR UPDATE OR DELETE
ON identity_access.identity_scope_administration_policy_statements
FOR EACH ROW EXECUTE FUNCTION identity_access.capture_security_mutation(
    'identity_scope_id',
    'application_key',
    'policy_id',
    'statement_id');

CREATE TRIGGER trg_security_mutation_scope_administration_bindings
AFTER INSERT OR UPDATE OR DELETE
ON identity_access.identity_scope_administration_group_policy_bindings
FOR EACH ROW EXECUTE FUNCTION identity_access.capture_security_mutation(
    'identity_scope_id',
    'application_key',
    'group_id',
    'policy_id');
