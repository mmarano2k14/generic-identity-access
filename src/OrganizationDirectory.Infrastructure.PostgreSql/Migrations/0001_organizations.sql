CREATE TABLE organization_directory.organizations
(
    identity_scope_id uuid NOT NULL,
    tenant_id uuid NOT NULL,
    organization_id uuid NOT NULL,
    organization_key varchar(64) NOT NULL,
    display_name varchar(200) NOT NULL,
    organization_type varchar(64) NOT NULL,
    parent_organization_id uuid NULL,
    status smallint NOT NULL,
    row_version bigint NOT NULL DEFAULT 1,
    created_at timestamptz NOT NULL,
    updated_at timestamptz NOT NULL,

    CONSTRAINT pk_organizations
        PRIMARY KEY (identity_scope_id, tenant_id, organization_id),

    CONSTRAINT uq_organizations_key
        UNIQUE (identity_scope_id, tenant_id, organization_key),

    CONSTRAINT fk_organizations_tenant
        FOREIGN KEY (identity_scope_id, tenant_id)
        REFERENCES identity_access.tenants
            (identity_scope_id, tenant_id)
        ON DELETE RESTRICT,

    CONSTRAINT fk_organizations_parent
        FOREIGN KEY (identity_scope_id, tenant_id, parent_organization_id)
        REFERENCES organization_directory.organizations
            (identity_scope_id, tenant_id, organization_id)
        ON DELETE RESTRICT,

    CONSTRAINT ck_organizations_key
        CHECK (organization_key ~ '^[a-z][a-z0-9-]{0,63}$'),

    CONSTRAINT ck_organizations_type
        CHECK (organization_type ~ '^[a-z][a-z0-9-]{0,63}$'),

    CONSTRAINT ck_organizations_display_name
        CHECK (length(btrim(display_name)) BETWEEN 1 AND 200),

    CONSTRAINT ck_organizations_status
        CHECK (status IN (1, 2)),

    CONSTRAINT ck_organizations_row_version
        CHECK (row_version > 0),

    CONSTRAINT ck_organizations_timestamps
        CHECK (updated_at >= created_at),

    CONSTRAINT ck_organizations_not_self_parent
        CHECK (parent_organization_id IS NULL OR parent_organization_id <> organization_id)
);

CREATE INDEX ix_organizations_parent
    ON organization_directory.organizations
        (identity_scope_id, tenant_id, parent_organization_id)
    WHERE parent_organization_id IS NOT NULL;

CREATE OR REPLACE FUNCTION organization_directory.reject_organization_cycle()
RETURNS trigger
LANGUAGE plpgsql
AS $$
BEGIN
    IF NEW.parent_organization_id IS NULL THEN
        RETURN NEW;
    END IF;

    IF NEW.parent_organization_id = NEW.organization_id THEN
        RAISE EXCEPTION 'Organization cannot parent itself.'
            USING ERRCODE = '23514';
    END IF;

    IF EXISTS
    (
        WITH RECURSIVE ancestors AS
        (
            SELECT o.organization_id, o.parent_organization_id
            FROM organization_directory.organizations o
            WHERE o.identity_scope_id = NEW.identity_scope_id
              AND o.tenant_id = NEW.tenant_id
              AND o.organization_id = NEW.parent_organization_id

            UNION

            SELECT p.organization_id, p.parent_organization_id
            FROM organization_directory.organizations p
            INNER JOIN ancestors a
                ON p.organization_id = a.parent_organization_id
               AND p.identity_scope_id = NEW.identity_scope_id
               AND p.tenant_id = NEW.tenant_id
        )
        SELECT 1 FROM ancestors WHERE organization_id = NEW.organization_id
    ) THEN
        RAISE EXCEPTION 'Organization hierarchy cycle detected.'
            USING ERRCODE = '23514';
    END IF;

    RETURN NEW;
END;
$$;

CREATE TRIGGER trg_organizations_reject_cycle
BEFORE INSERT OR UPDATE
ON organization_directory.organizations
FOR EACH ROW
EXECUTE FUNCTION organization_directory.reject_organization_cycle();
