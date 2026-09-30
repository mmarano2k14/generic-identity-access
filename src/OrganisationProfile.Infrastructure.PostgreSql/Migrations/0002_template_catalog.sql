CREATE TABLE organisation_profile.organisation_profile_templates
(
    template_key varchar(64) NOT NULL,
    display_name varchar(200) NOT NULL,
    status smallint NOT NULL,
    row_version bigint NOT NULL DEFAULT 1,
    created_at timestamptz NOT NULL,
    updated_at timestamptz NOT NULL,

    CONSTRAINT pk_organisation_profile_templates
        PRIMARY KEY (template_key),

    CONSTRAINT ck_organisation_profile_templates_key
        CHECK (template_key ~ '^[a-z][a-z0-9-]{0,63}$'),

    CONSTRAINT ck_organisation_profile_templates_display_name
        CHECK (length(btrim(display_name)) BETWEEN 1 AND 200),

    CONSTRAINT ck_organisation_profile_templates_status
        CHECK (status IN (1, 2)),

    CONSTRAINT ck_organisation_profile_templates_row_version
        CHECK (row_version > 0),

    CONSTRAINT ck_organisation_profile_templates_timestamps
        CHECK (updated_at >= created_at)
);

CREATE TABLE organisation_profile.organisation_profile_template_versions
(
    template_key varchar(64) NOT NULL,
    template_version integer NOT NULL,
    status smallint NOT NULL,
    content_hash char(64) NULL,
    row_version bigint NOT NULL DEFAULT 1,
    created_at timestamptz NOT NULL,
    updated_at timestamptz NOT NULL,
    published_at timestamptz NULL,
    retired_at timestamptz NULL,

    CONSTRAINT pk_organisation_profile_template_versions
        PRIMARY KEY (template_key, template_version),

    CONSTRAINT fk_organisation_profile_template_versions_template
        FOREIGN KEY (template_key)
        REFERENCES organisation_profile.organisation_profile_templates
            (template_key)
        ON DELETE RESTRICT,

    CONSTRAINT ck_organisation_profile_template_versions_version
        CHECK (template_version > 0),

    CONSTRAINT ck_organisation_profile_template_versions_status
        CHECK (status IN (1, 2, 3)),

    CONSTRAINT ck_organisation_profile_template_versions_hash
        CHECK
        (
            content_hash IS NULL
            OR content_hash ~ '^[0-9a-f]{64}$'
        ),

    CONSTRAINT ck_organisation_profile_template_versions_publication
        CHECK
        (
            (
                status = 1
                AND content_hash IS NULL
                AND published_at IS NULL
                AND retired_at IS NULL
            )
            OR
            (
                status = 2
                AND content_hash IS NOT NULL
                AND published_at IS NOT NULL
                AND retired_at IS NULL
            )
            OR
            (
                status = 3
                AND content_hash IS NOT NULL
                AND published_at IS NOT NULL
                AND retired_at IS NOT NULL
                AND retired_at >= published_at
            )
        ),

    CONSTRAINT ck_organisation_profile_template_versions_row_version
        CHECK (row_version > 0),

    CONSTRAINT ck_organisation_profile_template_versions_timestamps
        CHECK (updated_at >= created_at)
);

CREATE TABLE organisation_profile.organisation_profile_template_domains
(
    template_key varchar(64) NOT NULL,
    template_version integer NOT NULL,
    domain_key varchar(64) NOT NULL,
    domain_version integer NOT NULL,

    CONSTRAINT pk_organisation_profile_template_domains
        PRIMARY KEY (template_key, template_version, domain_key),

    CONSTRAINT fk_organisation_profile_template_domains_version
        FOREIGN KEY (template_key, template_version)
        REFERENCES organisation_profile.organisation_profile_template_versions
            (template_key, template_version)
        ON DELETE CASCADE,

    CONSTRAINT ck_organisation_profile_template_domains_key
        CHECK (domain_key ~ '^[a-z][a-z0-9-]{0,63}$'),

    CONSTRAINT ck_organisation_profile_template_domains_version
        CHECK (domain_version > 0)
);

CREATE OR REPLACE FUNCTION
organisation_profile.enforce_template_version_immutability()
RETURNS trigger
LANGUAGE plpgsql
AS $$
BEGIN
    IF OLD.status = 3 THEN
        RAISE EXCEPTION
            'Retired OrganisationProfile template version is immutable'
            USING
                ERRCODE = '23514',
                CONSTRAINT = 'ck_organisation_profile_template_version_immutable';
    END IF;

    IF OLD.status = 2 THEN
        IF NEW.status <> 3
           OR NEW.template_key <> OLD.template_key
           OR NEW.template_version <> OLD.template_version
           OR NEW.content_hash IS DISTINCT FROM OLD.content_hash
           OR NEW.created_at IS DISTINCT FROM OLD.created_at
           OR NEW.published_at IS DISTINCT FROM OLD.published_at
           OR NEW.retired_at IS NULL
           OR NEW.retired_at < OLD.published_at THEN
            RAISE EXCEPTION
                'Published OrganisationProfile template content is immutable'
                USING
                    ERRCODE = '23514',
                    CONSTRAINT = 'ck_organisation_profile_template_version_immutable';
        END IF;
    END IF;

    IF OLD.status = 1 AND NEW.status = 3 THEN
        RAISE EXCEPTION
            'Draft OrganisationProfile template version cannot be retired'
            USING
                ERRCODE = '23514',
                CONSTRAINT = 'ck_organisation_profile_template_version_immutable';
    END IF;

    RETURN NEW;
END;
$$;

CREATE TRIGGER trg_organisation_profile_template_version_immutable
BEFORE UPDATE
ON organisation_profile.organisation_profile_template_versions
FOR EACH ROW
EXECUTE FUNCTION organisation_profile.enforce_template_version_immutability();

CREATE OR REPLACE FUNCTION
organisation_profile.enforce_template_domain_mutability()
RETURNS trigger
LANGUAGE plpgsql
AS $$
DECLARE
    resolved_status smallint;
BEGIN
    IF TG_OP = 'DELETE' THEN
        SELECT status
        INTO resolved_status
        FROM organisation_profile.organisation_profile_template_versions
        WHERE template_key = OLD.template_key
          AND template_version = OLD.template_version;

        IF resolved_status IS NULL THEN
            RETURN OLD;
        END IF;
    ELSE
        SELECT status
        INTO resolved_status
        FROM organisation_profile.organisation_profile_template_versions
        WHERE template_key = NEW.template_key
          AND template_version = NEW.template_version;

        IF resolved_status IS NULL THEN
            RAISE EXCEPTION
                'OrganisationProfile template version does not exist'
                USING
                    ERRCODE = '23503',
                    CONSTRAINT = 'fk_organisation_profile_template_domains_version';
        END IF;
    END IF;

    IF resolved_status <> 1 THEN
        RAISE EXCEPTION
            'Published or retired OrganisationProfile template domains are immutable'
            USING
                ERRCODE = '23514',
                CONSTRAINT = 'ck_organisation_profile_template_domain_immutable';
    END IF;

    RETURN COALESCE(NEW, OLD);
END;
$$;

CREATE TRIGGER trg_organisation_profile_template_domain_mutable
BEFORE INSERT OR UPDATE OR DELETE
ON organisation_profile.organisation_profile_template_domains
FOR EACH ROW
EXECUTE FUNCTION organisation_profile.enforce_template_domain_mutability();

DO $$
BEGIN
    IF EXISTS
    (
        SELECT 1
        FROM organisation_profile.organisation_profiles
        WHERE template_key IS NOT NULL
           OR template_version IS NOT NULL
    ) THEN
        RAISE EXCEPTION
            'Existing OrganisationProfile template pins must be cleared before applying template-catalog referential integrity';
    END IF;
END;
$$;

ALTER TABLE organisation_profile.organisation_profiles
    ADD CONSTRAINT fk_organisation_profiles_template_version
    FOREIGN KEY (template_key, template_version)
    REFERENCES organisation_profile.organisation_profile_template_versions
        (template_key, template_version)
    ON DELETE RESTRICT;

CREATE OR REPLACE FUNCTION
organisation_profile.enforce_profile_template_assignment_published()
RETURNS trigger
LANGUAGE plpgsql
AS $$
DECLARE
    referenced_status smallint;
    template_status smallint;
    pin_changed boolean;
BEGIN
    IF NEW.template_key IS NULL THEN
        RETURN NEW;
    END IF;

    IF TG_OP = 'INSERT' THEN
        pin_changed := TRUE;
    ELSE
        pin_changed :=
            OLD.template_key IS DISTINCT FROM NEW.template_key
            OR OLD.template_version IS DISTINCT FROM NEW.template_version;
    END IF;

    IF NOT pin_changed THEN
        RETURN NEW;
    END IF;

    SELECT version.status, template.status
    INTO referenced_status, template_status
    FROM organisation_profile.organisation_profile_template_versions version
    INNER JOIN organisation_profile.organisation_profile_templates template
        ON template.template_key = version.template_key
    WHERE version.template_key = NEW.template_key
      AND version.template_version = NEW.template_version;

    IF referenced_status IS NULL THEN
        RETURN NEW;
    END IF;

    IF referenced_status <> 2 OR template_status <> 1 THEN
        RAISE EXCEPTION
            'OrganisationProfile may only be newly pinned to a Published version of an active template'
            USING
                ERRCODE = '23514',
                CONSTRAINT = 'ck_organisation_profiles_template_published';
    END IF;

    RETURN NEW;
END;
$$;

CREATE TRIGGER trg_organisation_profile_template_assignment_published
BEFORE INSERT OR UPDATE OF template_key, template_version
ON organisation_profile.organisation_profiles
FOR EACH ROW
EXECUTE FUNCTION organisation_profile.enforce_profile_template_assignment_published();

CREATE INDEX ix_organisation_profile_template_versions_status
    ON organisation_profile.organisation_profile_template_versions
        (template_key, status, template_version);
