BEGIN;

-- Fixed rollback-scoped fixture identities.
INSERT INTO identity_access.users
    (identity_scope_id, user_id, display_name, status)
VALUES
    ('71000000-0000-4000-8000-000000000001', '71000000-0000-4000-8000-000000000011', 'Projection User', 1);

INSERT INTO identity_access.tenants
    (identity_scope_id, tenant_id, display_name, status)
VALUES
    ('71000000-0000-4000-8000-000000000001', '71000000-0000-4000-8000-000000000021', 'Projection Tenant', 1);

INSERT INTO identity_access.tenant_memberships
    (identity_scope_id, membership_id, tenant_id, user_id, status)
VALUES
    ('71000000-0000-4000-8000-000000000001', '71000000-0000-4000-8000-000000000031',
     '71000000-0000-4000-8000-000000000021', '71000000-0000-4000-8000-000000000011', 1);

INSERT INTO identity_access.user_groups
    (identity_scope_id, tenant_id, application_key, group_id, display_name, status)
VALUES
    ('71000000-0000-4000-8000-000000000001', '71000000-0000-4000-8000-000000000021',
     'app-a', '71000000-0000-4000-8000-000000000041', 'Active Group', 1),
    ('71000000-0000-4000-8000-000000000001', '71000000-0000-4000-8000-000000000021',
     'app-a', '71000000-0000-4000-8000-000000000042', 'Suspended Group', 2);

INSERT INTO identity_access.group_memberships
    (identity_scope_id, tenant_id, application_key, group_id, tenant_membership_id)
VALUES
    ('71000000-0000-4000-8000-000000000001', '71000000-0000-4000-8000-000000000021',
     'app-a', '71000000-0000-4000-8000-000000000041', '71000000-0000-4000-8000-000000000031'),
    ('71000000-0000-4000-8000-000000000001', '71000000-0000-4000-8000-000000000021',
     'app-a', '71000000-0000-4000-8000-000000000042', '71000000-0000-4000-8000-000000000031');

INSERT INTO identity_access.application_security_models
    (identity_scope_id, application_key, model_version)
VALUES
    ('71000000-0000-4000-8000-000000000001', 'app-a', 1);

INSERT INTO identity_access.application_capabilities
    (identity_scope_id, application_key, model_version,
     capability_resource, capability_feature, capability_action, display_name)
VALUES
    ('71000000-0000-4000-8000-000000000001', 'app-a', 1,
     'billing', 'invoice', 'read', 'Read invoices'),
    ('71000000-0000-4000-8000-000000000001', 'app-a', 1,
     'inventory', 'stock', 'read', 'Read stock'),
    ('71000000-0000-4000-8000-000000000001', 'app-a', 1,
     'analytics', 'report', 'read', 'Read reports'),
    ('71000000-0000-4000-8000-000000000001', 'app-a', 1,
     'support', 'ticket', 'read', 'Read tickets');

INSERT INTO identity_access.application_scope_types
    (identity_scope_id, application_key, model_version, scope_type_key, display_name,
     parent_scope_type_key, can_attach_to_tenant)
VALUES
    ('71000000-0000-4000-8000-000000000001', 'app-a', 1,
     'organization', 'Organization', NULL, TRUE),
    ('71000000-0000-4000-8000-000000000001', 'app-a', 1,
     'business', 'Business', 'organization', FALSE);

INSERT INTO identity_access.resource_scopes
    (identity_scope_id, tenant_id, application_key, resource_scope_id, scope_model_version,
     scope_type_key, external_resource_id, display_name, parent_resource_scope_id, status)
VALUES
    ('71000000-0000-4000-8000-000000000001', '71000000-0000-4000-8000-000000000021',
     'app-a', '71000000-0000-4000-8000-000000000101', 1,
     'organization', 'holding', 'Holding', NULL, 1),
    ('71000000-0000-4000-8000-000000000001', '71000000-0000-4000-8000-000000000021',
     'app-a', '71000000-0000-4000-8000-000000000102', 1,
     'business', 'unit-a', 'Unit A', '71000000-0000-4000-8000-000000000101', 1),
    ('71000000-0000-4000-8000-000000000001', '71000000-0000-4000-8000-000000000021',
     'app-a', '71000000-0000-4000-8000-000000000103', 1,
     'business', 'unit-b', 'Unit B', '71000000-0000-4000-8000-000000000101', 1);

-- Historical legacy rows intentionally remain present. They are negative evidence only:
-- the managed-only reader must never project them.
INSERT INTO identity_access.permission_policies
    (identity_scope_id, tenant_id, application_key, policy_id, display_name, status)
VALUES
    ('71000000-0000-4000-8000-000000000001', '71000000-0000-4000-8000-000000000021',
     'app-a', '71000000-0000-4000-8000-000000000051', 'Historical Legacy Policy', 1);

INSERT INTO identity_access.policy_statements
    (identity_scope_id, tenant_id, application_key, policy_id, statement_id, model_version,
     capability_resource, capability_feature, capability_action)
VALUES
    ('71000000-0000-4000-8000-000000000001', '71000000-0000-4000-8000-000000000021',
     'app-a', '71000000-0000-4000-8000-000000000051', '71000000-0000-4000-8000-000000000061', 1,
     'billing', 'invoice', 'read');

INSERT INTO identity_access.group_policy_bindings
    (identity_scope_id, tenant_id, application_key, group_id, policy_id)
VALUES
    ('71000000-0000-4000-8000-000000000001', '71000000-0000-4000-8000-000000000021',
     'app-a', '71000000-0000-4000-8000-000000000041', '71000000-0000-4000-8000-000000000051');

-- Managed policy lifecycle: shared definitions -> draft versions -> catalog statements -> publish -> default.
INSERT INTO identity_access.managed_policies
    (identity_scope_id, application_key, policy_id, policy_key, display_name, status)
VALUES
    ('71000000-0000-4000-8000-000000000001', 'app-a',
     '71000000-0000-4000-8000-000000000071', 'tenant-reader', 'Tenant Reader', 1),
    ('71000000-0000-4000-8000-000000000001', 'app-a',
     '71000000-0000-4000-8000-000000000072', 'exact-stock-reader', 'Exact Stock Reader', 1),
    ('71000000-0000-4000-8000-000000000001', 'app-a',
     '71000000-0000-4000-8000-000000000073', 'descendant-report-reader', 'Descendant Report Reader', 1),
    ('71000000-0000-4000-8000-000000000001', 'app-a',
     '71000000-0000-4000-8000-000000000074', 'suspended-ticket-reader', 'Suspended Ticket Reader', 2);

INSERT INTO identity_access.managed_policy_versions
    (identity_scope_id, application_key, policy_id, policy_version, model_version)
VALUES
    ('71000000-0000-4000-8000-000000000001', 'app-a', '71000000-0000-4000-8000-000000000071', 1, 1),
    ('71000000-0000-4000-8000-000000000001', 'app-a', '71000000-0000-4000-8000-000000000072', 1, 1),
    ('71000000-0000-4000-8000-000000000001', 'app-a', '71000000-0000-4000-8000-000000000073', 1, 1),
    ('71000000-0000-4000-8000-000000000001', 'app-a', '71000000-0000-4000-8000-000000000074', 1, 1);

INSERT INTO identity_access.managed_policy_statements
    (identity_scope_id, application_key, policy_id, policy_version, model_version, statement_id,
     capability_resource, capability_feature, capability_action)
VALUES
    ('71000000-0000-4000-8000-000000000001', 'app-a', '71000000-0000-4000-8000-000000000071', 1, 1,
     '71000000-0000-4000-8000-000000000081', 'billing', 'invoice', 'read'),
    ('71000000-0000-4000-8000-000000000001', 'app-a', '71000000-0000-4000-8000-000000000072', 1, 1,
     '71000000-0000-4000-8000-000000000082', 'inventory', 'stock', 'read'),
    ('71000000-0000-4000-8000-000000000001', 'app-a', '71000000-0000-4000-8000-000000000073', 1, 1,
     '71000000-0000-4000-8000-000000000083', 'analytics', 'report', 'read'),
    ('71000000-0000-4000-8000-000000000001', 'app-a', '71000000-0000-4000-8000-000000000074', 1, 1,
     '71000000-0000-4000-8000-000000000084', 'support', 'ticket', 'read');

UPDATE identity_access.managed_policy_versions
SET published_at = transaction_timestamp()
WHERE identity_scope_id = '71000000-0000-4000-8000-000000000001'
  AND application_key = 'app-a';

UPDATE identity_access.managed_policies
SET default_version = 1,
    row_version = row_version + 1,
    updated_at = transaction_timestamp()
WHERE identity_scope_id = '71000000-0000-4000-8000-000000000001'
  AND application_key = 'app-a';

-- Active group: tenant-wide, exact target, descendant target, plus a suspended policy.
INSERT INTO identity_access.managed_group_policy_bindings
    (identity_scope_id, tenant_id, application_key, group_id, policy_id, policy_version,
     resource_scope_id, include_descendants)
VALUES
    ('71000000-0000-4000-8000-000000000001', '71000000-0000-4000-8000-000000000021',
     'app-a', '71000000-0000-4000-8000-000000000041', '71000000-0000-4000-8000-000000000071', 1,
     NULL, FALSE),
    ('71000000-0000-4000-8000-000000000001', '71000000-0000-4000-8000-000000000021',
     'app-a', '71000000-0000-4000-8000-000000000041', '71000000-0000-4000-8000-000000000072', 1,
     '71000000-0000-4000-8000-000000000102', FALSE),
    ('71000000-0000-4000-8000-000000000001', '71000000-0000-4000-8000-000000000021',
     'app-a', '71000000-0000-4000-8000-000000000041', '71000000-0000-4000-8000-000000000073', 1,
     '71000000-0000-4000-8000-000000000101', TRUE),
    ('71000000-0000-4000-8000-000000000001', '71000000-0000-4000-8000-000000000021',
     'app-a', '71000000-0000-4000-8000-000000000041', '71000000-0000-4000-8000-000000000074', 1,
     NULL, FALSE),
    -- Same active policy on a suspended group must not add a grant.
    ('71000000-0000-4000-8000-000000000001', '71000000-0000-4000-8000-000000000021',
     'app-a', '71000000-0000-4000-8000-000000000042', '71000000-0000-4000-8000-000000000071', 1,
     NULL, FALSE);

-- Mirrors the managed-only SQL shape used by PostgreSqlAssignedCapabilityReader.
CREATE OR REPLACE FUNCTION pg_temp.managed_projection_count(target_scope uuid)
RETURNS integer
LANGUAGE sql
AS $$
    WITH RECURSIVE scope_ancestry AS
    (
        SELECT rs.resource_scope_id, rs.parent_resource_scope_id
        FROM identity_access.resource_scopes rs
        WHERE rs.identity_scope_id = '71000000-0000-4000-8000-000000000001'
          AND rs.tenant_id = '71000000-0000-4000-8000-000000000021'
          AND rs.application_key = 'app-a'
          AND rs.resource_scope_id = target_scope
          AND rs.status = 1

        UNION ALL

        SELECT parent.resource_scope_id, parent.parent_resource_scope_id
        FROM identity_access.resource_scopes parent
        JOIN scope_ancestry child
          ON child.parent_resource_scope_id = parent.resource_scope_id
        WHERE parent.identity_scope_id = '71000000-0000-4000-8000-000000000001'
          AND parent.tenant_id = '71000000-0000-4000-8000-000000000021'
          AND parent.application_key = 'app-a'
          AND parent.status = 1
    ),
    eligible_groups AS
    (
        SELECT ug.group_id
        FROM identity_access.users u
        JOIN identity_access.tenants t
          ON t.identity_scope_id = u.identity_scope_id
         AND t.tenant_id = '71000000-0000-4000-8000-000000000021'
        JOIN identity_access.tenant_memberships tm
          ON tm.identity_scope_id = u.identity_scope_id
         AND tm.tenant_id = t.tenant_id
         AND tm.user_id = u.user_id
        JOIN identity_access.group_memberships gm
          ON gm.identity_scope_id = tm.identity_scope_id
         AND gm.tenant_id = tm.tenant_id
         AND gm.tenant_membership_id = tm.membership_id
         AND gm.application_key = 'app-a'
        JOIN identity_access.user_groups ug
          ON ug.identity_scope_id = gm.identity_scope_id
         AND ug.tenant_id = gm.tenant_id
         AND ug.application_key = gm.application_key
         AND ug.group_id = gm.group_id
        WHERE u.identity_scope_id = '71000000-0000-4000-8000-000000000001'
          AND u.user_id = '71000000-0000-4000-8000-000000000011'
          AND u.status = 1
          AND t.status = 1
          AND tm.status = 1
          AND ug.status = 1
    )
    SELECT count(*)::integer
    FROM eligible_groups eg
    JOIN identity_access.managed_group_policy_bindings mgb
      ON mgb.identity_scope_id = '71000000-0000-4000-8000-000000000001'
     AND mgb.tenant_id = '71000000-0000-4000-8000-000000000021'
     AND mgb.application_key = 'app-a'
     AND mgb.group_id = eg.group_id
    JOIN identity_access.managed_policies mp
      ON mp.identity_scope_id = mgb.identity_scope_id
     AND mp.application_key = mgb.application_key
     AND mp.policy_id = mgb.policy_id
    JOIN identity_access.managed_policy_versions mpv
      ON mpv.identity_scope_id = mp.identity_scope_id
     AND mpv.application_key = mp.application_key
     AND mpv.policy_id = mp.policy_id
     AND mpv.policy_version = mgb.policy_version
    JOIN identity_access.managed_policy_statements mps
      ON mps.identity_scope_id = mpv.identity_scope_id
     AND mps.application_key = mpv.application_key
     AND mps.policy_id = mpv.policy_id
     AND mps.policy_version = mpv.policy_version
     AND mps.model_version = mpv.model_version
    WHERE mp.status = 1
      AND mpv.published_at IS NOT NULL
      AND
      (
          (target_scope IS NULL AND mgb.resource_scope_id IS NULL)
          OR
          (target_scope IS NOT NULL
           AND EXISTS (SELECT 1 FROM scope_ancestry a WHERE a.resource_scope_id = target_scope)
           AND
           (
               mgb.resource_scope_id IS NULL
               OR mgb.resource_scope_id = target_scope
               OR (mgb.include_descendants = TRUE AND EXISTS
                   (SELECT 1 FROM scope_ancestry a WHERE a.resource_scope_id = mgb.resource_scope_id))
           ))
      );
$$;

DO $$
DECLARE
    legacy_count integer;
BEGIN
    IF pg_temp.managed_projection_count(NULL) <> 1 THEN
        RAISE EXCEPTION 'Tenant-wide projection must contain exactly the active tenant-wide managed grant.';
    END IF;

    IF pg_temp.managed_projection_count('71000000-0000-4000-8000-000000000102') <> 3 THEN
        RAISE EXCEPTION 'Exact Unit A projection must include tenant-wide, exact, and ancestor-descendant managed grants.';
    END IF;

    IF pg_temp.managed_projection_count('71000000-0000-4000-8000-000000000103') <> 2 THEN
        RAISE EXCEPTION 'Sibling Unit B projection must include tenant-wide and ancestor-descendant grants but not Unit A exact grant.';
    END IF;

    IF pg_temp.managed_projection_count('71000000-0000-4000-8000-000000000101') <> 2 THEN
        RAISE EXCEPTION 'Organization projection must include tenant-wide and the exact ancestor binding.';
    END IF;

    SELECT count(*) INTO legacy_count
    FROM identity_access.group_policy_bindings gpb
    JOIN identity_access.permission_policies pp
      ON pp.identity_scope_id = gpb.identity_scope_id
     AND pp.tenant_id = gpb.tenant_id
     AND pp.application_key = gpb.application_key
     AND pp.policy_id = gpb.policy_id
    JOIN identity_access.policy_statements ps
      ON ps.identity_scope_id = pp.identity_scope_id
     AND ps.tenant_id = pp.tenant_id
     AND ps.application_key = pp.application_key
     AND ps.policy_id = pp.policy_id
    WHERE gpb.identity_scope_id = '71000000-0000-4000-8000-000000000001'
      AND gpb.tenant_id = '71000000-0000-4000-8000-000000000021'
      AND gpb.application_key = 'app-a'
      AND gpb.group_id = '71000000-0000-4000-8000-000000000041'
      AND pp.status = 1;

    IF legacy_count <> 1 THEN
        RAISE EXCEPTION 'Historical legacy evidence must remain present for the negative fallback proof.';
    END IF;
END $$;

-- Remove every managed binding. Historical legacy rows stay present and must still project zero grants.
DELETE FROM identity_access.managed_group_policy_bindings
WHERE identity_scope_id = '71000000-0000-4000-8000-000000000001'
  AND tenant_id = '71000000-0000-4000-8000-000000000021'
  AND application_key = 'app-a';

DO $$
DECLARE
    legacy_count integer;
BEGIN
    SELECT count(*) INTO legacy_count
    FROM identity_access.group_policy_bindings
    WHERE identity_scope_id = '71000000-0000-4000-8000-000000000001'
      AND tenant_id = '71000000-0000-4000-8000-000000000021'
      AND application_key = 'app-a'
      AND group_id = '71000000-0000-4000-8000-000000000041';

    IF legacy_count <> 1 THEN
        RAISE EXCEPTION 'Expected the historical legacy binding to remain present for the fallback proof.';
    END IF;

    IF pg_temp.managed_projection_count(NULL) <> 0
       OR pg_temp.managed_projection_count('71000000-0000-4000-8000-000000000102') <> 0
       OR pg_temp.managed_projection_count('71000000-0000-4000-8000-000000000103') <> 0 THEN
        RAISE EXCEPTION 'Legacy-only state must project zero managed authorization grants for every target shape.';
    END IF;
END $$;

ROLLBACK;
