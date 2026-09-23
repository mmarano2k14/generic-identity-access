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
    code_hash_type text;
    set_trigger_count integer;
    code_trigger_count integer;
    active_index_count integer;
BEGIN
    IF to_regclass('identity_access.recovery_code_sets') IS NULL THEN
        RAISE EXCEPTION 'Recovery-code set table is missing';
    END IF;

    IF to_regclass('identity_access.recovery_codes') IS NULL THEN
        RAISE EXCEPTION 'Recovery-code table is missing';
    END IF;

    SELECT data_type INTO code_hash_type
    FROM information_schema.columns
    WHERE table_schema = 'identity_access'
      AND table_name = 'recovery_codes'
      AND column_name = 'code_hash';

    IF code_hash_type <> 'bytea' THEN
        RAISE EXCEPTION 'Recovery-code hashes must use bytea storage';
    END IF;

    IF EXISTS
    (
        SELECT 1
        FROM information_schema.columns
        WHERE table_schema = 'identity_access'
          AND table_name = 'recovery_codes'
          AND column_name IN ('code', 'raw_code', 'protected_code')
    ) THEN
        RAISE EXCEPTION 'Raw or reversibly protected recovery codes must not be persisted';
    END IF;

    IF EXISTS
    (
        SELECT 1
        FROM information_schema.columns
        WHERE table_schema = 'identity_access'
          AND table_name = 'user_authenticators'
          AND column_name ILIKE '%recovery%'
    ) THEN
        RAISE EXCEPTION 'Generic authenticator metadata contains recovery-provider-specific columns';
    END IF;

    SELECT count(*) INTO set_trigger_count
    FROM information_schema.triggers
    WHERE event_object_schema = 'identity_access'
      AND event_object_table = 'recovery_code_sets'
      AND trigger_name = 'trg_security_mutation_recovery_code_sets';

    SELECT count(*) INTO code_trigger_count
    FROM information_schema.triggers
    WHERE event_object_schema = 'identity_access'
      AND event_object_table = 'recovery_codes'
      AND trigger_name = 'trg_security_mutation_recovery_codes';

    IF set_trigger_count <> 3 OR code_trigger_count <> 3 THEN
        RAISE EXCEPTION 'Recovery-code mutation-ledger trigger coverage is incomplete';
    END IF;

    SELECT count(*) INTO active_index_count
    FROM pg_indexes
    WHERE schemaname = 'identity_access'
      AND tablename = 'user_authenticators'
      AND indexname = 'uq_user_authenticators_active_recovery'
      AND indexdef ILIKE '%recovery%'
      AND indexdef ILIKE '%status%'
      AND indexdef ILIKE '%2%';

    IF active_index_count <> 1 THEN
        RAISE EXCEPTION 'Active recovery-code set uniqueness index is missing';
    END IF;
END
`$verify`$;
"@

& psql -U $postgresUser -v ON_ERROR_STOP=1 -d $databaseName -c $sql
if ($LASTEXITCODE -ne 0) {
    throw "Recovery-code provider validation failed with exit code $LASTEXITCODE."
}

Write-Host "Recovery-code provider PostgreSQL validation passed."
