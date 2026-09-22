$ErrorActionPreference = "Stop"

if (-not (Get-Command psql -ErrorAction SilentlyContinue)) {
    throw "psql was not found on PATH."
}

$database = "generic_identity_access_default"
$user = "postgres"

$sql = @'
BEGIN;

DO $$
DECLARE
    s uuid := gen_random_uuid();
    t uuid := gen_random_uuid();
    p uuid := gen_random_uuid();
    st uuid := gen_random_uuid();
BEGIN
    INSERT INTO identity_access.tenants(identity_scope_id, tenant_id, display_name, status)
    VALUES (s, t, 'Wildcard test tenant', 1);

    INSERT INTO identity_access.application_security_models(identity_scope_id, application_key, model_version)
    VALUES (s, 'app-wildcard-test', 1);

    INSERT INTO identity_access.application_capabilities
        (identity_scope_id, application_key, model_version,
         capability_resource, capability_feature, capability_action, display_name)
    VALUES
        (s, 'app-wildcard-test', 1, 'billing', 'invoice', 'read', 'Read invoice'),
        (s, 'app-wildcard-test', 1, 'billing', 'invoice', 'refund', 'Refund invoice');

    INSERT INTO identity_access.permission_policies
        (identity_scope_id, tenant_id, application_key, policy_id, display_name, status)
    VALUES (s, t, 'app-wildcard-test', p, 'Wildcard policy', 1);

    INSERT INTO identity_access.policy_statements
        (identity_scope_id, tenant_id, application_key, policy_id, statement_id,
         model_version, capability_resource, capability_feature, capability_action)
    VALUES (s, t, 'app-wildcard-test', p, st, 1, 'billing', 'invoice', '*');

    BEGIN
        INSERT INTO identity_access.policy_statements
            (identity_scope_id, tenant_id, application_key, policy_id, statement_id,
             model_version, capability_resource, capability_feature, capability_action)
        VALUES (s, t, 'app-wildcard-test', p, gen_random_uuid(), 1, '*', 'invoice', 'read');
        RAISE EXCEPTION 'unsupported wildcard shape unexpectedly succeeded';
    EXCEPTION WHEN check_violation THEN
        NULL;
    END;

    BEGIN
        INSERT INTO identity_access.policy_statements
            (identity_scope_id, tenant_id, application_key, policy_id, statement_id,
             model_version, capability_resource, capability_feature, capability_action)
        VALUES (s, t, 'app-wildcard-test', p, gen_random_uuid(), 1, 'inventory', '*', '*');
        RAISE EXCEPTION 'non-matching wildcard unexpectedly succeeded';
    EXCEPTION WHEN check_violation THEN
        NULL;
    END;
END
$$;

ROLLBACK;
'@

$sql | & psql -U $user -d $database -v ON_ERROR_STOP=1
if ($LASTEXITCODE -ne 0) {
    throw "Wildcard policy persistence verification failed with exit code $LASTEXITCODE."
}

Write-Host "Wildcard policy persistence verification passed."
