CREATE TABLE IF NOT EXISTS identity_access.users
(
    identity_scope_id uuid NOT NULL,
    user_id uuid NOT NULL,
    display_name varchar(256) NOT NULL,
    status smallint NOT NULL,
    row_version bigint NOT NULL DEFAULT 1,
    created_at timestamptz NOT NULL DEFAULT transaction_timestamp(),
    updated_at timestamptz NOT NULL DEFAULT transaction_timestamp(),
    CONSTRAINT pk_users PRIMARY KEY (identity_scope_id, user_id),
    CONSTRAINT ck_users_status CHECK (status IN (1, 2)),
    CONSTRAINT ck_users_row_version CHECK (row_version > 0)
);

CREATE TABLE IF NOT EXISTS identity_access.tenants
(
    identity_scope_id uuid NOT NULL,
    tenant_id uuid NOT NULL,
    display_name varchar(256) NOT NULL,
    status smallint NOT NULL,
    row_version bigint NOT NULL DEFAULT 1,
    created_at timestamptz NOT NULL DEFAULT transaction_timestamp(),
    updated_at timestamptz NOT NULL DEFAULT transaction_timestamp(),
    CONSTRAINT pk_tenants PRIMARY KEY (identity_scope_id, tenant_id),
    CONSTRAINT ck_tenants_status CHECK (status IN (1, 2)),
    CONSTRAINT ck_tenants_row_version CHECK (row_version > 0)
);

CREATE TABLE IF NOT EXISTS identity_access.tenant_memberships
(
    identity_scope_id uuid NOT NULL,
    membership_id uuid NOT NULL,
    tenant_id uuid NOT NULL,
    user_id uuid NOT NULL,
    status smallint NOT NULL,
    row_version bigint NOT NULL DEFAULT 1,
    created_at timestamptz NOT NULL DEFAULT transaction_timestamp(),
    updated_at timestamptz NOT NULL DEFAULT transaction_timestamp(),
    CONSTRAINT pk_tenant_memberships PRIMARY KEY (identity_scope_id, membership_id),
    CONSTRAINT uq_tenant_membership_subject UNIQUE (identity_scope_id, tenant_id, user_id),
    CONSTRAINT uq_tenant_membership_tenant_key UNIQUE (identity_scope_id, tenant_id, membership_id),
    CONSTRAINT fk_tenant_memberships_tenant FOREIGN KEY (identity_scope_id, tenant_id)
        REFERENCES identity_access.tenants (identity_scope_id, tenant_id) ON DELETE RESTRICT,
    CONSTRAINT fk_tenant_memberships_user FOREIGN KEY (identity_scope_id, user_id)
        REFERENCES identity_access.users (identity_scope_id, user_id) ON DELETE RESTRICT,
    CONSTRAINT ck_tenant_memberships_status CHECK (status IN (1, 2)),
    CONSTRAINT ck_tenant_memberships_row_version CHECK (row_version > 0)
);

CREATE TABLE IF NOT EXISTS identity_access.user_groups
(
    identity_scope_id uuid NOT NULL,
    tenant_id uuid NOT NULL,
    application_key varchar(64) NOT NULL,
    group_id uuid NOT NULL,
    display_name varchar(256) NOT NULL,
    status smallint NOT NULL,
    row_version bigint NOT NULL DEFAULT 1,
    created_at timestamptz NOT NULL DEFAULT transaction_timestamp(),
    updated_at timestamptz NOT NULL DEFAULT transaction_timestamp(),
    CONSTRAINT pk_user_groups PRIMARY KEY (identity_scope_id, tenant_id, application_key, group_id),
    CONSTRAINT fk_user_groups_tenant FOREIGN KEY (identity_scope_id, tenant_id)
        REFERENCES identity_access.tenants (identity_scope_id, tenant_id) ON DELETE RESTRICT,
    CONSTRAINT ck_user_groups_application_key CHECK (application_key ~ '^[a-z][a-z0-9-]{0,63}$'),
    CONSTRAINT ck_user_groups_status CHECK (status IN (1, 2)),
    CONSTRAINT ck_user_groups_row_version CHECK (row_version > 0)
);

CREATE TABLE IF NOT EXISTS identity_access.group_memberships
(
    identity_scope_id uuid NOT NULL,
    tenant_id uuid NOT NULL,
    application_key varchar(64) NOT NULL,
    group_id uuid NOT NULL,
    tenant_membership_id uuid NOT NULL,
    created_at timestamptz NOT NULL DEFAULT transaction_timestamp(),
    CONSTRAINT pk_group_memberships PRIMARY KEY
        (identity_scope_id, tenant_id, application_key, group_id, tenant_membership_id),
    CONSTRAINT fk_group_memberships_group FOREIGN KEY
        (identity_scope_id, tenant_id, application_key, group_id)
        REFERENCES identity_access.user_groups
        (identity_scope_id, tenant_id, application_key, group_id) ON DELETE RESTRICT,
    CONSTRAINT fk_group_memberships_membership FOREIGN KEY
        (identity_scope_id, tenant_id, tenant_membership_id)
        REFERENCES identity_access.tenant_memberships
        (identity_scope_id, tenant_id, membership_id) ON DELETE RESTRICT
);

CREATE INDEX IF NOT EXISTS ix_tenant_memberships_subject
    ON identity_access.tenant_memberships (identity_scope_id, user_id);

CREATE INDEX IF NOT EXISTS ix_group_memberships_member
    ON identity_access.group_memberships (identity_scope_id, tenant_membership_id);
