$ErrorActionPreference = "Stop"

if (-not (Get-Command psql -ErrorAction SilentlyContinue)) {
    throw "psql was not found on PATH."
}

$databaseName = if ($env:IDENTITY_ACCESS_POSTGRES_DATABASE) {
    $env:IDENTITY_ACCESS_POSTGRES_DATABASE
} else {
    "generic_identity_access_default"
}

$postgresUser = if ($env:IDENTITY_ACCESS_POSTGRES_USER) {
    $env:IDENTITY_ACCESS_POSTGRES_USER
} else {
    "postgres"
}

$sql = @"
DO `$verify`$
DECLARE
    missing_tables integer;
    forbidden_columns integer;
    mutation_triggers integer;
BEGIN
    SELECT count(*) INTO missing_tables
    FROM (VALUES
        ('mfa_policies'),
        ('mfa_policy_providers'),
        ('user_authenticators')
    ) AS required(table_name)
    WHERE to_regclass('identity_access.' || required.table_name) IS NULL;

    IF missing_tables <> 0 THEN
        RAISE EXCEPTION 'MFA provider foundation tables are incomplete';
    END IF;

    SELECT count(*) INTO forbidden_columns
    FROM information_schema.columns
    WHERE table_schema = 'identity_access'
      AND table_name IN ('mfa_policies', 'mfa_policy_providers', 'user_authenticators')
      AND (
          column_name ILIKE '%secret%'
          OR column_name ILIKE '%payload%'
          OR column_name ILIKE '%private_key%'
          OR column_name ILIKE '%recovery_code%'
      );

    IF forbidden_columns <> 0 THEN
        RAISE EXCEPTION 'Generic MFA schema contains provider-owned secret material';
    END IF;

    SELECT count(*) INTO mutation_triggers
    FROM information_schema.triggers
    WHERE event_object_schema = 'identity_access'
      AND trigger_name IN (
          'trg_security_mutation_mfa_policies',
          'trg_security_mutation_mfa_policy_providers',
          'trg_security_mutation_user_authenticators');

    IF mutation_triggers <> 9 THEN
        RAISE EXCEPTION 'MFA transactional mutation trigger coverage is incomplete';
    END IF;
END
`$verify`$;
"@

& psql -U $postgresUser -v ON_ERROR_STOP=1 -d $databaseName -c $sql
if ($LASTEXITCODE -ne 0) {
    throw "MFA provider foundation validation failed with exit code $LASTEXITCODE."
}

Write-Host "MFA provider foundation PostgreSQL validation passed."
