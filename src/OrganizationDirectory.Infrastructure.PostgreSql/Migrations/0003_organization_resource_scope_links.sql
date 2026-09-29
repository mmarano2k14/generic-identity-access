CREATE TABLE organization_directory.organization_resource_scope_links
(
    identity_scope_id uuid NOT NULL,
    tenant_id uuid NOT NULL,
    organization_id uuid NOT NULL,
    application_key varchar(64) NOT NULL,
    resource_scope_id uuid NOT NULL,
    status smallint NOT NULL,
    row_version bigint NOT NULL DEFAULT 1,
    created_at timestamptz NOT NULL,
    updated_at timestamptz NOT NULL,

    CONSTRAINT pk_organization_resource_scope_links
        PRIMARY KEY
        (identity_scope_id, tenant_id, organization_id, application_key),

    CONSTRAINT uq_organization_resource_scope_links_scope
        UNIQUE
        (identity_scope_id, tenant_id, application_key, resource_scope_id),

    CONSTRAINT fk_organization_resource_scope_links_organization
        FOREIGN KEY (identity_scope_id, tenant_id, organization_id)
        REFERENCES organization_directory.organizations
            (identity_scope_id, tenant_id, organization_id)
        ON DELETE RESTRICT,

    CONSTRAINT fk_organization_resource_scope_links_resource_scope
        FOREIGN KEY
        (identity_scope_id, tenant_id, application_key, resource_scope_id)
        REFERENCES identity_access.resource_scopes
            (identity_scope_id, tenant_id, application_key, resource_scope_id)
        ON DELETE RESTRICT,

    CONSTRAINT ck_organization_resource_scope_links_application_key
        CHECK (application_key ~ '^[a-z][a-z0-9-]{0,63}$'),

    CONSTRAINT ck_organization_resource_scope_links_status
        CHECK (status IN (1, 2)),

    CONSTRAINT ck_organization_resource_scope_links_row_version
        CHECK (row_version > 0),

    CONSTRAINT ck_organization_resource_scope_links_timestamps
        CHECK (updated_at >= created_at)
);

CREATE INDEX ix_organization_resource_scope_links_scope
    ON organization_directory.organization_resource_scope_links
        (identity_scope_id, tenant_id, application_key, resource_scope_id);

CREATE INDEX ix_organization_resource_scope_links_organization
    ON organization_directory.organization_resource_scope_links
        (identity_scope_id, tenant_id, organization_id, status);
