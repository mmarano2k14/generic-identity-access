CREATE TABLE organisation_profile.organisation_profile_domain_overrides
(
    organisation_profile_id uuid NOT NULL,
    domain_key varchar(64) NOT NULL,
    operation smallint NOT NULL,
    domain_version integer NULL,

    CONSTRAINT pk_organisation_profile_domain_overrides
        PRIMARY KEY (organisation_profile_id, domain_key),

    CONSTRAINT fk_organisation_profile_domain_overrides_profile
        FOREIGN KEY (organisation_profile_id)
        REFERENCES organisation_profile.organisation_profiles
            (organisation_profile_id)
        ON DELETE CASCADE,

    CONSTRAINT ck_organisation_profile_domain_overrides_key
        CHECK (domain_key ~ '^[a-z][a-z0-9-]{0,63}$'),

    CONSTRAINT ck_organisation_profile_domain_overrides_operation
        CHECK (operation IN (1, 2)),

    CONSTRAINT ck_organisation_profile_domain_overrides_version
        CHECK
        (
            (operation = 1 AND domain_version IS NOT NULL AND domain_version > 0)
            OR
            (operation = 2 AND domain_version IS NULL)
        )
);

CREATE TABLE organisation_profile.organisation_profile_versions
(
    organisation_profile_id uuid NOT NULL,
    profile_version bigint NOT NULL,

    template_key varchar(64) NULL,
    template_version integer NULL,

    content_hash char(64) NOT NULL,
    source_profile_row_version bigint NOT NULL,
    resolved_at timestamptz NOT NULL,

    CONSTRAINT pk_organisation_profile_versions
        PRIMARY KEY (organisation_profile_id, profile_version),

    CONSTRAINT fk_organisation_profile_versions_profile
        FOREIGN KEY (organisation_profile_id)
        REFERENCES organisation_profile.organisation_profiles
            (organisation_profile_id)
        ON DELETE CASCADE,

    CONSTRAINT fk_organisation_profile_versions_template
        FOREIGN KEY (template_key, template_version)
        REFERENCES organisation_profile.organisation_profile_template_versions
            (template_key, template_version)
        ON DELETE RESTRICT,

    CONSTRAINT ck_organisation_profile_versions_version
        CHECK (profile_version > 0),

    CONSTRAINT ck_organisation_profile_versions_template_pin
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

    CONSTRAINT ck_organisation_profile_versions_hash
        CHECK (content_hash ~ '^[0-9a-f]{64}$'),

    CONSTRAINT ck_organisation_profile_versions_source_row
        CHECK (source_profile_row_version > 0)
);

CREATE TABLE organisation_profile.organisation_profile_version_domains
(
    organisation_profile_id uuid NOT NULL,
    profile_version bigint NOT NULL,
    domain_key varchar(64) NOT NULL,
    domain_version integer NOT NULL,

    CONSTRAINT pk_organisation_profile_version_domains
        PRIMARY KEY
        (
            organisation_profile_id,
            profile_version,
            domain_key
        ),

    CONSTRAINT fk_organisation_profile_version_domains_version
        FOREIGN KEY (organisation_profile_id, profile_version)
        REFERENCES organisation_profile.organisation_profile_versions
            (organisation_profile_id, profile_version)
        ON DELETE CASCADE,

    CONSTRAINT ck_organisation_profile_version_domains_key
        CHECK (domain_key ~ '^[a-z][a-z0-9-]{0,63}$'),

    CONSTRAINT ck_organisation_profile_version_domains_version
        CHECK (domain_version > 0)
);

CREATE OR REPLACE FUNCTION
organisation_profile.reject_effective_profile_version_update()
RETURNS trigger
LANGUAGE plpgsql
AS $$
BEGIN
    RAISE EXCEPTION
        'Effective OrganisationProfile versions are immutable'
        USING
            ERRCODE = '23514',
            CONSTRAINT = 'ck_organisation_profile_version_immutable';
END;
$$;

CREATE TRIGGER trg_organisation_profile_version_immutable
BEFORE UPDATE
ON organisation_profile.organisation_profile_versions
FOR EACH ROW
EXECUTE FUNCTION
organisation_profile.reject_effective_profile_version_update();

CREATE OR REPLACE FUNCTION
organisation_profile.reject_effective_profile_domain_update()
RETURNS trigger
LANGUAGE plpgsql
AS $$
BEGIN
    RAISE EXCEPTION
        'Effective OrganisationProfile version domains are immutable'
        USING
            ERRCODE = '23514',
            CONSTRAINT = 'ck_organisation_profile_version_domain_immutable';
END;
$$;

CREATE TRIGGER trg_organisation_profile_version_domain_immutable
BEFORE UPDATE
ON organisation_profile.organisation_profile_version_domains
FOR EACH ROW
EXECUTE FUNCTION
organisation_profile.reject_effective_profile_domain_update();

CREATE INDEX ix_organisation_profile_domain_overrides_profile
    ON organisation_profile.organisation_profile_domain_overrides
        (organisation_profile_id, domain_key);

CREATE INDEX ix_organisation_profile_versions_latest
    ON organisation_profile.organisation_profile_versions
        (organisation_profile_id, profile_version DESC);
