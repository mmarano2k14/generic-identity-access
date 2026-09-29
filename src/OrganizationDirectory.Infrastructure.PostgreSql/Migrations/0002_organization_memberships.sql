CREATE TABLE organization_directory.organization_memberships
(
    identity_scope_id uuid NOT NULL,
    tenant_id uuid NOT NULL,
    organization_id uuid NOT NULL,
    tenant_membership_id uuid NOT NULL,
    status smallint NOT NULL,
    row_version bigint NOT NULL DEFAULT 1,
    created_at timestamptz NOT NULL,
    updated_at timestamptz NOT NULL,

    CONSTRAINT pk_organization_memberships
        PRIMARY KEY
        (identity_scope_id, tenant_id, organization_id, tenant_membership_id),

    CONSTRAINT fk_organization_memberships_organization
        FOREIGN KEY (identity_scope_id, tenant_id, organization_id)
        REFERENCES organization_directory.organizations
            (identity_scope_id, tenant_id, organization_id)
        ON DELETE RESTRICT,

    CONSTRAINT fk_organization_memberships_tenant_membership
        FOREIGN KEY (identity_scope_id, tenant_id, tenant_membership_id)
        REFERENCES identity_access.tenant_memberships
            (identity_scope_id, tenant_id, membership_id)
        ON DELETE RESTRICT,

    CONSTRAINT ck_organization_memberships_status
        CHECK (status IN (1, 2)),

    CONSTRAINT ck_organization_memberships_row_version
        CHECK (row_version > 0),

    CONSTRAINT ck_organization_memberships_timestamps
        CHECK (updated_at >= created_at)
);

CREATE INDEX ix_organization_memberships_member
    ON organization_directory.organization_memberships
        (identity_scope_id, tenant_id, tenant_membership_id, organization_id);

CREATE INDEX ix_organization_memberships_organization_status
    ON organization_directory.organization_memberships
        (identity_scope_id, tenant_id, organization_id, status, tenant_membership_id);
