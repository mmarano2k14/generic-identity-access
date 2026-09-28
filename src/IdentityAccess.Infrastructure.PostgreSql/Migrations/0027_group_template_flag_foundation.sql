ALTER TABLE identity_access.user_groups
    ADD COLUMN IF NOT EXISTS is_template boolean NOT NULL DEFAULT FALSE;

CREATE INDEX IF NOT EXISTS ix_user_groups_template_catalog
    ON identity_access.user_groups
       (identity_scope_id, application_key, status, lower(display_name), group_id)
    WHERE is_template = TRUE;

CREATE UNIQUE INDEX IF NOT EXISTS uq_user_groups_template_group_id
    ON identity_access.user_groups
       (identity_scope_id, application_key, group_id)
    WHERE is_template = TRUE;
