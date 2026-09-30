CREATE TABLE organisation_profile.organisation_profiles
(
    organisation_profile_id uuid NOT NULL,
    identity_scope_id uuid NOT NULL,
    tenant_id uuid NOT NULL,
    organization_id uuid NOT NULL,

    template_key varchar(64) NULL,
    template_version integer NULL,

    status smallint NOT NULL,
    row_version bigint NOT NULL DEFAULT 1,

    created_at timestamptz NOT NULL,
    updated_at timestamptz NOT NULL,

    CONSTRAINT pk_organisation_profiles
        PRIMARY KEY (organisation_profile_id),

    CONSTRAINT uq_organisation_profiles_organization
        UNIQUE (identity_scope_id, tenant_id, organization_id),

    CONSTRAINT fk_organisation_profiles_organization
        FOREIGN KEY (identity_scope_id, tenant_id, organization_id)
        REFERENCES organization_directory.organizations
            (identity_scope_id, tenant_id, organization_id)
        ON DELETE RESTRICT,

    CONSTRAINT ck_organisation_profiles_template_pin
        CHECK
        (
            (template_key IS NULL AND template_version IS NULL)
            OR
            (
                template_key IS NOT NULL
                AND template_version IS NOT NULL
                AND template_key ~ '^[a-z][a-z0-9-]{0,63}$'
                AND template_version > 0
            )
        ),

    CONSTRAINT ck_organisation_profiles_status
        CHECK (status IN (1, 2)),

    CONSTRAINT ck_organisation_profiles_row_version
        CHECK (row_version > 0),

    CONSTRAINT ck_organisation_profiles_timestamps
        CHECK (updated_at >= created_at)
);

CREATE INDEX ix_organisation_profiles_tenant
    ON organisation_profile.organisation_profiles
        (identity_scope_id, tenant_id, organisation_profile_id);
