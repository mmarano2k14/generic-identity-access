CREATE TABLE IF NOT EXISTS identity_access.group_templates
(
    identity_scope_id uuid NOT NULL,
    application_key varchar(64) NOT NULL,
    template_id uuid NOT NULL,
    display_name varchar(256) NOT NULL,
    status smallint NOT NULL,
    row_version bigint NOT NULL DEFAULT 1,
    created_at timestamptz NOT NULL DEFAULT transaction_timestamp(),
    updated_at timestamptz NOT NULL DEFAULT transaction_timestamp(),
    CONSTRAINT pk_group_templates PRIMARY KEY (identity_scope_id, application_key, template_id),
    CONSTRAINT ck_group_templates_application_key CHECK (application_key ~ '^[a-z][a-z0-9-]{0,63}$'),
    CONSTRAINT ck_group_templates_status CHECK (status IN (1, 2)),
    CONSTRAINT ck_group_templates_row_version CHECK (row_version > 0)
);

CREATE INDEX IF NOT EXISTS ix_group_templates_display_name_search
    ON identity_access.group_templates (identity_scope_id, application_key, lower(display_name), template_id);

ALTER TABLE identity_access.user_groups
    ADD COLUMN IF NOT EXISTS origin smallint NOT NULL DEFAULT 2,
    ADD COLUMN IF NOT EXISTS template_id uuid NULL;

ALTER TABLE identity_access.user_groups
    DROP CONSTRAINT IF EXISTS ck_user_groups_origin;
ALTER TABLE identity_access.user_groups
    ADD CONSTRAINT ck_user_groups_origin CHECK (origin IN (1, 2));

ALTER TABLE identity_access.user_groups
    DROP CONSTRAINT IF EXISTS ck_user_groups_template_origin;
ALTER TABLE identity_access.user_groups
    ADD CONSTRAINT ck_user_groups_template_origin CHECK
    (
        (origin = 1 AND template_id IS NOT NULL)
        OR
        (origin = 2 AND template_id IS NULL)
    );

DO $$
BEGIN
    IF NOT EXISTS
    (
        SELECT 1
        FROM pg_constraint
        WHERE conname = 'fk_user_groups_template'
          AND conrelid = 'identity_access.user_groups'::regclass
    ) THEN
        ALTER TABLE identity_access.user_groups
            ADD CONSTRAINT fk_user_groups_template FOREIGN KEY
            (identity_scope_id, application_key, template_id)
            REFERENCES identity_access.group_templates
            (identity_scope_id, application_key, template_id)
            ON DELETE RESTRICT;
    END IF;
END
$$;

CREATE UNIQUE INDEX IF NOT EXISTS uq_user_groups_template_instance
    ON identity_access.user_groups (identity_scope_id, tenant_id, application_key, template_id)
    WHERE template_id IS NOT NULL;

DROP TRIGGER IF EXISTS trg_security_mutation_group_templates ON identity_access.group_templates;
CREATE TRIGGER trg_security_mutation_group_templates
AFTER INSERT OR UPDATE OR DELETE ON identity_access.group_templates
FOR EACH ROW EXECUTE FUNCTION identity_access.capture_security_mutation('identity_scope_id', 'template_id');
