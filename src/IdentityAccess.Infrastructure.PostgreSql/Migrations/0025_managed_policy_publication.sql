ALTER TABLE identity_access.managed_policy_versions
    ADD COLUMN published_at timestamptz NULL;

-- Existing versions already selected as defaults or referenced by managed bindings carried
-- active intent before explicit publication state existed. Preserve that intent by publishing
-- those concrete versions at their original creation timestamp before immutability guards apply.
UPDATE identity_access.managed_policy_versions AS pv
SET published_at = pv.created_at
WHERE pv.published_at IS NULL
  AND
  (
      EXISTS
      (
          SELECT 1
          FROM identity_access.managed_policies AS p
          WHERE p.identity_scope_id = pv.identity_scope_id
            AND p.application_key = pv.application_key
            AND p.policy_id = pv.policy_id
            AND p.default_version = pv.policy_version
      )
      OR EXISTS
      (
          SELECT 1
          FROM identity_access.managed_group_policy_bindings AS b
          WHERE b.identity_scope_id = pv.identity_scope_id
            AND b.application_key = pv.application_key
            AND b.policy_id = pv.policy_id
            AND b.policy_version = pv.policy_version
      )
  );

CREATE OR REPLACE FUNCTION identity_access.guard_managed_policy_version_mutation()
RETURNS trigger
LANGUAGE plpgsql
AS $$
BEGIN
    IF TG_OP = 'DELETE' THEN
        IF OLD.published_at IS NOT NULL THEN
            RAISE EXCEPTION 'Published managed policy versions are immutable.';
        END IF;
        RETURN OLD;
    END IF;

    IF OLD.published_at IS NOT NULL THEN
        RAISE EXCEPTION 'Published managed policy versions are immutable.';
    END IF;

    IF NEW.identity_scope_id IS DISTINCT FROM OLD.identity_scope_id
       OR NEW.application_key IS DISTINCT FROM OLD.application_key
       OR NEW.policy_id IS DISTINCT FROM OLD.policy_id
       OR NEW.policy_version IS DISTINCT FROM OLD.policy_version
       OR NEW.model_version IS DISTINCT FROM OLD.model_version
       OR NEW.created_at IS DISTINCT FROM OLD.created_at THEN
        RAISE EXCEPTION 'Managed policy version identity and model pinning are immutable.';
    END IF;

    IF NEW.published_at IS NULL THEN
        RAISE EXCEPTION 'Managed policy version updates may only publish an unpublished version.';
    END IF;

    RETURN NEW;
END;
$$;

CREATE TRIGGER trg_guard_managed_policy_version_mutation
BEFORE UPDATE OR DELETE
ON identity_access.managed_policy_versions
FOR EACH ROW EXECUTE FUNCTION identity_access.guard_managed_policy_version_mutation();

CREATE OR REPLACE FUNCTION identity_access.guard_managed_policy_statement_mutation()
RETURNS trigger
LANGUAGE plpgsql
AS $$
BEGIN
    IF TG_OP = 'UPDATE' OR TG_OP = 'DELETE' THEN
        IF EXISTS
        (
            SELECT 1
            FROM identity_access.managed_policy_versions AS pv
            WHERE pv.identity_scope_id = OLD.identity_scope_id
              AND pv.application_key = OLD.application_key
              AND pv.policy_id = OLD.policy_id
              AND pv.policy_version = OLD.policy_version
              AND pv.published_at IS NOT NULL
        ) THEN
            RAISE EXCEPTION 'Statements of a published managed policy version are immutable.';
        END IF;
    END IF;

    IF TG_OP = 'INSERT' OR TG_OP = 'UPDATE' THEN
        IF EXISTS
        (
            SELECT 1
            FROM identity_access.managed_policy_versions AS pv
            WHERE pv.identity_scope_id = NEW.identity_scope_id
              AND pv.application_key = NEW.application_key
              AND pv.policy_id = NEW.policy_id
              AND pv.policy_version = NEW.policy_version
              AND pv.published_at IS NOT NULL
        ) THEN
            RAISE EXCEPTION 'Statements of a published managed policy version are immutable.';
        END IF;
    END IF;

    IF TG_OP = 'DELETE' THEN
        RETURN OLD;
    END IF;

    RETURN NEW;
END;
$$;

CREATE TRIGGER trg_guard_managed_policy_statement_mutation
BEFORE INSERT OR UPDATE OR DELETE
ON identity_access.managed_policy_statements
FOR EACH ROW EXECUTE FUNCTION identity_access.guard_managed_policy_statement_mutation();

CREATE OR REPLACE FUNCTION identity_access.guard_managed_policy_default_version()
RETURNS trigger
LANGUAGE plpgsql
AS $$
BEGIN
    IF NEW.default_version IS NOT NULL AND NOT EXISTS
    (
        SELECT 1
        FROM identity_access.managed_policy_versions AS pv
        WHERE pv.identity_scope_id = NEW.identity_scope_id
          AND pv.application_key = NEW.application_key
          AND pv.policy_id = NEW.policy_id
          AND pv.policy_version = NEW.default_version
          AND pv.published_at IS NOT NULL
    ) THEN
        RAISE EXCEPTION 'Managed policy default version must reference a published version.';
    END IF;

    RETURN NEW;
END;
$$;

CREATE TRIGGER trg_guard_managed_policy_default_version
BEFORE INSERT OR UPDATE
ON identity_access.managed_policies
FOR EACH ROW EXECUTE FUNCTION identity_access.guard_managed_policy_default_version();

CREATE OR REPLACE FUNCTION identity_access.guard_managed_policy_binding_publication()
RETURNS trigger
LANGUAGE plpgsql
AS $$
BEGIN
    IF NOT EXISTS
    (
        SELECT 1
        FROM identity_access.managed_policy_versions AS pv
        WHERE pv.identity_scope_id = NEW.identity_scope_id
          AND pv.application_key = NEW.application_key
          AND pv.policy_id = NEW.policy_id
          AND pv.policy_version = NEW.policy_version
          AND pv.published_at IS NOT NULL
    ) THEN
        RAISE EXCEPTION 'Managed policy bindings require a published policy version.';
    END IF;

    RETURN NEW;
END;
$$;

CREATE TRIGGER trg_guard_managed_policy_binding_publication
BEFORE INSERT OR UPDATE
ON identity_access.managed_group_policy_bindings
FOR EACH ROW EXECUTE FUNCTION identity_access.guard_managed_policy_binding_publication();

CREATE INDEX ix_managed_policy_versions_published
    ON identity_access.managed_policy_versions
       (identity_scope_id, application_key, policy_id, policy_version)
    WHERE published_at IS NOT NULL;
