$ErrorActionPreference = "Stop"

if (-not (Get-Command psql -ErrorAction SilentlyContinue)) {
    throw "psql was not found on PATH."
}

$database = if ($env:IDENTITY_ACCESS_POSTGRES_DATABASE) { $env:IDENTITY_ACCESS_POSTGRES_DATABASE } else { "generic_identity_access_default" }
$user = if ($env:IDENTITY_ACCESS_POSTGRES_USER) { $env:IDENTITY_ACCESS_POSTGRES_USER } else { "postgres" }

$sql = @'
BEGIN;

DO $$
DECLARE
    s uuid := gen_random_uuid();
    t uuid := gen_random_uuid();
    g uuid := gen_random_uuid();
    p uuid := gen_random_uuid();
    org uuid := gen_random_uuid();
    unit_a uuid := gen_random_uuid();
    unit_b uuid := gen_random_uuid();
BEGIN
    INSERT INTO identity_access.tenants(identity_scope_id, tenant_id, display_name, status)
    VALUES (s, t, 'Scope hierarchy tenant', 1);

    INSERT INTO identity_access.application_security_models(identity_scope_id, application_key, model_version)
    VALUES (s, 'app-scope-test', 1);

    INSERT INTO identity_access.application_scope_types
        (identity_scope_id, application_key, model_version, scope_type_key, display_name, parent_scope_type_key, can_attach_to_tenant)
    VALUES
        (s, 'app-scope-test', 1, 'organization', 'Organization', NULL, TRUE),
        (s, 'app-scope-test', 1, 'business', 'Business', 'organization', FALSE);

    INSERT INTO identity_access.resource_scopes
        (identity_scope_id, tenant_id, application_key, resource_scope_id, scope_model_version,
         scope_type_key, external_resource_id, display_name, parent_resource_scope_id, status)
    VALUES (s, t, 'app-scope-test', org, 1, 'organization', 'holding', 'Holding', NULL, 1);

    INSERT INTO identity_access.resource_scopes
        (identity_scope_id, tenant_id, application_key, resource_scope_id, scope_model_version,
         scope_type_key, external_resource_id, display_name, parent_resource_scope_id, status)
    VALUES
        (s, t, 'app-scope-test', unit_a, 1, 'business', 'unit-a', 'Unit A', org, 1),
        (s, t, 'app-scope-test', unit_b, 1, 'business', 'unit-b', 'Unit B', org, 1);

    INSERT INTO identity_access.user_groups(identity_scope_id, tenant_id, application_key, group_id, display_name, status)
    VALUES (s, t, 'app-scope-test', g, 'Scoped managers', 1);

    INSERT INTO identity_access.permission_policies(identity_scope_id, tenant_id, application_key, policy_id, display_name, status)
    VALUES (s, t, 'app-scope-test', p, 'Scoped policy', 1);

    INSERT INTO identity_access.group_policy_bindings
        (identity_scope_id, tenant_id, application_key, group_id, policy_id, resource_scope_id, include_descendants)
    VALUES
        (s, t, 'app-scope-test', g, p, unit_a, FALSE),
        (s, t, 'app-scope-test', g, p, org, TRUE),
        (s, t, 'app-scope-test', g, p, NULL, FALSE);

    IF (SELECT count(*) FROM identity_access.group_policy_bindings
        WHERE identity_scope_id = s AND tenant_id = t AND application_key = 'app-scope-test'
          AND group_id = g AND policy_id = p) <> 3 THEN
        RAISE EXCEPTION 'same group and policy were not persisted independently across binding targets';
    END IF;

    BEGIN
        INSERT INTO identity_access.resource_scopes
            (identity_scope_id, tenant_id, application_key, resource_scope_id, scope_model_version,
             scope_type_key, external_resource_id, display_name, parent_resource_scope_id, status)
        VALUES (s, t, 'app-scope-test', gen_random_uuid(), 1, 'business', 'bad-root', 'Bad root', NULL, 1);
        RAISE EXCEPTION 'child scope type unexpectedly attached directly to tenant';
    EXCEPTION WHEN check_violation THEN
        NULL;
    END;
END
$$;

ROLLBACK;
'@

$sql | & psql -U $user -d $database -v ON_ERROR_STOP=1
if ($LASTEXITCODE -ne 0) { throw "Resource scope hierarchy verification failed." }
Write-Host "Resource scope hierarchy verification passed."
