-- Persisted whole-segment RBAC patterns for policy statements.
-- Authorization evaluation remains delegated to the external RBAC engine.

ALTER TABLE identity_access.policy_statements
    DROP CONSTRAINT IF EXISTS fk_policy_statements_capability;

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint c
        JOIN pg_class t ON t.oid = c.conrelid
        JOIN pg_namespace n ON n.oid = t.relnamespace
        WHERE n.nspname = 'identity_access'
          AND t.relname = 'policy_statements'
          AND c.conname = 'fk_policy_statements_model'
    ) THEN
        ALTER TABLE identity_access.policy_statements
            ADD CONSTRAINT fk_policy_statements_model FOREIGN KEY
                (identity_scope_id, application_key, model_version)
            REFERENCES identity_access.application_security_models
                (identity_scope_id, application_key, model_version)
            ON DELETE RESTRICT;
    END IF;
END
$$;

ALTER TABLE identity_access.policy_statements
    DROP CONSTRAINT IF EXISTS ck_policy_statements_pattern_resource;
ALTER TABLE identity_access.policy_statements
    ADD CONSTRAINT ck_policy_statements_pattern_resource
        CHECK (capability_resource = '*' OR capability_resource ~ '^[a-z][a-z0-9-]{0,63}$');

ALTER TABLE identity_access.policy_statements
    DROP CONSTRAINT IF EXISTS ck_policy_statements_pattern_feature;
ALTER TABLE identity_access.policy_statements
    ADD CONSTRAINT ck_policy_statements_pattern_feature
        CHECK (capability_feature = '*' OR capability_feature ~ '^[a-z][a-z0-9-]{0,63}$');

ALTER TABLE identity_access.policy_statements
    DROP CONSTRAINT IF EXISTS ck_policy_statements_pattern_action;
ALTER TABLE identity_access.policy_statements
    ADD CONSTRAINT ck_policy_statements_pattern_action
        CHECK (capability_action = '*' OR capability_action ~ '^[a-z][a-z0-9-]{0,63}$');

ALTER TABLE identity_access.policy_statements
    DROP CONSTRAINT IF EXISTS ck_policy_statements_supported_pattern;
ALTER TABLE identity_access.policy_statements
    ADD CONSTRAINT ck_policy_statements_supported_pattern CHECK (
        capability_resource <> '*'
        OR capability_feature = '*'
    );

CREATE OR REPLACE FUNCTION identity_access.validate_policy_statement_pattern()
RETURNS trigger
LANGUAGE plpgsql
AS $$
BEGIN
    -- Exact grants must still point at a concrete capability declared by the pinned model.
    IF NEW.capability_resource <> '*'
       AND NEW.capability_feature <> '*'
       AND NEW.capability_action <> '*' THEN
        IF NOT EXISTS (
            SELECT 1
            FROM identity_access.application_capabilities c
            WHERE c.identity_scope_id = NEW.identity_scope_id
              AND c.application_key = NEW.application_key
              AND c.model_version = NEW.model_version
              AND c.capability_resource = NEW.capability_resource
              AND c.capability_feature = NEW.capability_feature
              AND c.capability_action = NEW.capability_action
        ) THEN
            RAISE EXCEPTION USING
                ERRCODE = '23514',
                MESSAGE = 'policy statement exact capability is not declared by the pinned security model';
        END IF;
    ELSE
        -- A wildcard pattern must match at least one declared concrete capability in the pinned model.
        IF NOT EXISTS (
            SELECT 1
            FROM identity_access.application_capabilities c
            WHERE c.identity_scope_id = NEW.identity_scope_id
              AND c.application_key = NEW.application_key
              AND c.model_version = NEW.model_version
              AND (NEW.capability_resource = '*' OR c.capability_resource = NEW.capability_resource)
              AND (NEW.capability_feature = '*' OR c.capability_feature = NEW.capability_feature)
              AND (NEW.capability_action = '*' OR c.capability_action = NEW.capability_action)
        ) THEN
            RAISE EXCEPTION USING
                ERRCODE = '23514',
                MESSAGE = 'policy statement wildcard pattern matches no capability in the pinned security model';
        END IF;
    END IF;

    RETURN NEW;
END
$$;

DROP TRIGGER IF EXISTS trg_validate_policy_statement_pattern ON identity_access.policy_statements;
CREATE TRIGGER trg_validate_policy_statement_pattern
BEFORE INSERT OR UPDATE OF model_version, capability_resource, capability_feature, capability_action
ON identity_access.policy_statements
FOR EACH ROW
EXECUTE FUNCTION identity_access.validate_policy_statement_pattern();

CREATE INDEX IF NOT EXISTS ix_policy_statements_pattern_lookup
    ON identity_access.policy_statements
       (identity_scope_id, tenant_id, application_key, model_version,
        capability_resource, capability_feature, capability_action);
