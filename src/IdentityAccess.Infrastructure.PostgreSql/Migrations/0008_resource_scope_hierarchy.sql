-- Generic application-defined resource scopes and scope-aware policy bindings.

CREATE TABLE identity_access.application_scope_types
(
    identity_scope_id uuid NOT NULL,
    application_key varchar(64) NOT NULL,
    model_version integer NOT NULL,
    scope_type_key varchar(64) NOT NULL,
    display_name varchar(200) NOT NULL,
    parent_scope_type_key varchar(64) NULL,
    can_attach_to_tenant boolean NOT NULL,
    created_at timestamptz NOT NULL DEFAULT transaction_timestamp(),
    CONSTRAINT pk_application_scope_types PRIMARY KEY
        (identity_scope_id, application_key, model_version, scope_type_key),
    CONSTRAINT fk_application_scope_types_model FOREIGN KEY
        (identity_scope_id, application_key, model_version)
        REFERENCES identity_access.application_security_models
        (identity_scope_id, application_key, model_version) ON DELETE RESTRICT,
    CONSTRAINT fk_application_scope_types_parent FOREIGN KEY
        (identity_scope_id, application_key, model_version, parent_scope_type_key)
        REFERENCES identity_access.application_scope_types
        (identity_scope_id, application_key, model_version, scope_type_key) ON DELETE RESTRICT,
    CONSTRAINT ck_application_scope_types_key
        CHECK (scope_type_key ~ '^[a-z][a-z0-9-]{0,63}$'),
    CONSTRAINT ck_application_scope_types_parent_key
        CHECK (parent_scope_type_key IS NULL OR parent_scope_type_key ~ '^[a-z][a-z0-9-]{0,63}$'),
    CONSTRAINT ck_application_scope_types_not_self_parent
        CHECK (parent_scope_type_key IS NULL OR parent_scope_type_key <> scope_type_key),
    CONSTRAINT ck_application_scope_types_attachment
        CHECK ((parent_scope_type_key IS NULL AND can_attach_to_tenant = TRUE)
            OR (parent_scope_type_key IS NOT NULL AND can_attach_to_tenant = FALSE))
);

CREATE TABLE identity_access.resource_scopes
(
    identity_scope_id uuid NOT NULL,
    tenant_id uuid NOT NULL,
    application_key varchar(64) NOT NULL,
    resource_scope_id uuid NOT NULL,
    scope_model_version integer NOT NULL,
    scope_type_key varchar(64) NOT NULL,
    external_resource_id varchar(200) NOT NULL,
    display_name varchar(200) NOT NULL,
    parent_resource_scope_id uuid NULL,
    status smallint NOT NULL,
    row_version bigint NOT NULL DEFAULT 1,
    created_at timestamptz NOT NULL DEFAULT transaction_timestamp(),
    updated_at timestamptz NOT NULL DEFAULT transaction_timestamp(),
    CONSTRAINT pk_resource_scopes PRIMARY KEY
        (identity_scope_id, tenant_id, application_key, resource_scope_id),
    CONSTRAINT uq_resource_scopes_external UNIQUE
        (identity_scope_id, tenant_id, application_key, scope_type_key, external_resource_id),
    CONSTRAINT fk_resource_scopes_tenant FOREIGN KEY
        (identity_scope_id, tenant_id)
        REFERENCES identity_access.tenants (identity_scope_id, tenant_id) ON DELETE RESTRICT,
    CONSTRAINT fk_resource_scopes_type FOREIGN KEY
        (identity_scope_id, application_key, scope_model_version, scope_type_key)
        REFERENCES identity_access.application_scope_types
        (identity_scope_id, application_key, model_version, scope_type_key) ON DELETE RESTRICT,
    CONSTRAINT fk_resource_scopes_parent FOREIGN KEY
        (identity_scope_id, tenant_id, application_key, parent_resource_scope_id)
        REFERENCES identity_access.resource_scopes
        (identity_scope_id, tenant_id, application_key, resource_scope_id) ON DELETE RESTRICT,
    CONSTRAINT ck_resource_scopes_status CHECK (status IN (1, 2)),
    CONSTRAINT ck_resource_scopes_row_version CHECK (row_version > 0),
    CONSTRAINT ck_resource_scopes_not_self_parent
        CHECK (parent_resource_scope_id IS NULL OR parent_resource_scope_id <> resource_scope_id)
);

CREATE INDEX ix_resource_scopes_parent
    ON identity_access.resource_scopes
       (identity_scope_id, tenant_id, application_key, parent_resource_scope_id);

CREATE OR REPLACE FUNCTION identity_access.validate_resource_scope_hierarchy()
RETURNS trigger
LANGUAGE plpgsql
AS $$
DECLARE
    declared_parent_type varchar(64);
    attachable boolean;
    actual_parent_type varchar(64);
    actual_parent_model integer;
    actual_parent_status smallint;
BEGIN
    SELECT parent_scope_type_key, can_attach_to_tenant
      INTO declared_parent_type, attachable
    FROM identity_access.application_scope_types
    WHERE identity_scope_id = NEW.identity_scope_id
      AND application_key = NEW.application_key
      AND model_version = NEW.scope_model_version
      AND scope_type_key = NEW.scope_type_key;

    IF NOT FOUND THEN
        RAISE EXCEPTION USING ERRCODE = '23514', MESSAGE = 'resource scope type is not declared by the selected security model';
    END IF;

    IF NEW.parent_resource_scope_id IS NULL THEN
        IF declared_parent_type IS NOT NULL OR attachable IS NOT TRUE THEN
            RAISE EXCEPTION USING ERRCODE = '23514', MESSAGE = 'resource scope type cannot attach directly to the tenant';
        END IF;
    ELSE
        SELECT scope_type_key, scope_model_version, status
          INTO actual_parent_type, actual_parent_model, actual_parent_status
        FROM identity_access.resource_scopes
        WHERE identity_scope_id = NEW.identity_scope_id
          AND tenant_id = NEW.tenant_id
          AND application_key = NEW.application_key
          AND resource_scope_id = NEW.parent_resource_scope_id;

        IF NOT FOUND THEN
            RAISE EXCEPTION USING ERRCODE = '23514', MESSAGE = 'resource scope parent was not found';
        END IF;
        IF declared_parent_type IS NULL OR actual_parent_type <> declared_parent_type THEN
            RAISE EXCEPTION USING ERRCODE = '23514', MESSAGE = 'resource scope parent type does not match the declared hierarchy';
        END IF;
        IF actual_parent_model <> NEW.scope_model_version THEN
            RAISE EXCEPTION USING ERRCODE = '23514', MESSAGE = 'resource scope parent must use the same scope model version';
        END IF;
        IF actual_parent_status <> 1 THEN
            RAISE EXCEPTION USING ERRCODE = '23514', MESSAGE = 'resource scope parent must be active';
        END IF;
    END IF;

    RETURN NEW;
END
$$;

CREATE TRIGGER trg_validate_resource_scope_hierarchy
BEFORE INSERT OR UPDATE OF scope_model_version, scope_type_key, parent_resource_scope_id
ON identity_access.resource_scopes
FOR EACH ROW EXECUTE FUNCTION identity_access.validate_resource_scope_hierarchy();

ALTER TABLE identity_access.group_policy_bindings
    ADD COLUMN resource_scope_id uuid NULL,
    ADD COLUMN include_descendants boolean NOT NULL DEFAULT FALSE;

ALTER TABLE identity_access.group_policy_bindings
    DROP CONSTRAINT pk_group_policy_bindings;

ALTER TABLE identity_access.group_policy_bindings
    ADD COLUMN binding_target_key text GENERATED ALWAYS AS
        (COALESCE(resource_scope_id::text, 'tenant')) STORED;

ALTER TABLE identity_access.group_policy_bindings
    ADD CONSTRAINT pk_group_policy_bindings PRIMARY KEY
        (identity_scope_id, tenant_id, application_key, group_id, policy_id, binding_target_key);

ALTER TABLE identity_access.group_policy_bindings
    ADD CONSTRAINT fk_group_policy_bindings_resource_scope FOREIGN KEY
        (identity_scope_id, tenant_id, application_key, resource_scope_id)
        REFERENCES identity_access.resource_scopes
        (identity_scope_id, tenant_id, application_key, resource_scope_id) ON DELETE RESTRICT;

ALTER TABLE identity_access.group_policy_bindings
    ADD CONSTRAINT ck_group_policy_bindings_descendants
        CHECK (resource_scope_id IS NOT NULL OR include_descendants = FALSE);

CREATE INDEX ix_group_policy_bindings_resource_scope
    ON identity_access.group_policy_bindings
       (identity_scope_id, tenant_id, application_key, resource_scope_id)
    WHERE resource_scope_id IS NOT NULL;
