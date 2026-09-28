-- Simplify reusable group templates into real tenant groups carrying an is_template marker.
-- Historical migration 0026 remains immutable for checksum compatibility.

-- Capture every tenant-local instance derived from the four artificial development templates.
-- Cleanup follows known relationship edges explicitly; no broad automatic delete propagation is used.
CREATE TEMP TABLE retired_seed_group_instances ON COMMIT DROP AS
SELECT identity_scope_id, tenant_id, application_key, group_id
FROM identity_access.user_groups
WHERE origin = 1
  AND template_id IN
  (
      '00000000-0000-0000-0000-000000000030',
      '00000000-0000-0000-0000-000000000031',
      '00000000-0000-0000-0000-000000000032',
      '00000000-0000-0000-0000-000000000033'
  );

DELETE FROM identity_access.group_memberships AS gm
USING retired_seed_group_instances AS retired
WHERE gm.identity_scope_id = retired.identity_scope_id
  AND gm.tenant_id = retired.tenant_id
  AND gm.application_key = retired.application_key
  AND gm.group_id = retired.group_id;

DELETE FROM identity_access.group_policy_bindings AS b
USING retired_seed_group_instances AS retired
WHERE b.identity_scope_id = retired.identity_scope_id
  AND b.tenant_id = retired.tenant_id
  AND b.application_key = retired.application_key
  AND b.group_id = retired.group_id;

DELETE FROM identity_access.managed_group_policy_bindings AS b
USING retired_seed_group_instances AS retired
WHERE b.identity_scope_id = retired.identity_scope_id
  AND b.tenant_id = retired.tenant_id
  AND b.application_key = retired.application_key
  AND b.group_id = retired.group_id;

DELETE FROM identity_access.user_groups AS g
USING retired_seed_group_instances AS retired
WHERE g.identity_scope_id = retired.identity_scope_id
  AND g.tenant_id = retired.tenant_id
  AND g.application_key = retired.application_key
  AND g.group_id = retired.group_id;

-- Remove the artificial catalogue entries introduced by 0026 before retiring the table.
DELETE FROM identity_access.group_templates
WHERE template_id IN
(
    '00000000-0000-0000-0000-000000000030',
    '00000000-0000-0000-0000-000000000031',
    '00000000-0000-0000-0000-000000000032',
    '00000000-0000-0000-0000-000000000033'
);

-- Do not silently discard non-development definitions from the retired global catalogue.
-- They cannot be converted automatically because the old model has no tenant_id. Operators must
-- first represent each intended reusable definition as a real tenant group, then rerun the migration.
DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM identity_access.group_templates) THEN
        RAISE EXCEPTION
            'Migration 0028 found non-development rows in identity_access.group_templates. Convert them to real user_groups before retrying.';
    END IF;
END
$$;

ALTER TABLE identity_access.user_groups
    DROP CONSTRAINT IF EXISTS fk_user_groups_template,
    DROP CONSTRAINT IF EXISTS ck_user_groups_template_origin,
    DROP CONSTRAINT IF EXISTS ck_user_groups_origin;

DROP INDEX IF EXISTS identity_access.uq_user_groups_template_instance;

ALTER TABLE identity_access.user_groups
    DROP COLUMN IF EXISTS template_id,
    DROP COLUMN IF EXISTS origin,
    ADD COLUMN IF NOT EXISTS is_template boolean NOT NULL DEFAULT FALSE;

CREATE INDEX IF NOT EXISTS ix_user_groups_reusable_templates
    ON identity_access.user_groups
        (identity_scope_id, application_key, is_template, status, lower(display_name), tenant_id, group_id);

-- user_groups already has the transactional security-mutation trigger installed by 0011;
-- changes to is_template therefore remain part of the durable mutation ledger.
DROP TRIGGER IF EXISTS trg_security_mutation_group_templates ON identity_access.group_templates;
DROP TABLE IF EXISTS identity_access.group_templates;

