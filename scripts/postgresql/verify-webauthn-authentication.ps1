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
    challenge_hash_type text;
    challenge_trigger_count integer;
BEGIN
    IF to_regclass('identity_access.webauthn_authentication_challenges') IS NULL THEN
        RAISE EXCEPTION 'WebAuthn authentication challenge table is missing';
    END IF;

    SELECT data_type INTO challenge_hash_type
    FROM information_schema.columns
    WHERE table_schema = 'identity_access'
      AND table_name = 'webauthn_authentication_challenges'
      AND column_name = 'challenge_hash';

    IF challenge_hash_type <> 'bytea' THEN
        RAISE EXCEPTION 'WebAuthn authentication challenge hashes must use bytea storage';
    END IF;

    IF EXISTS
    (
        SELECT 1
        FROM information_schema.columns
        WHERE table_schema = 'identity_access'
          AND table_name = 'webauthn_authentication_challenges'
          AND column_name ILIKE '%raw%challenge%'
    ) THEN
        RAISE EXCEPTION 'WebAuthn authentication persistence contains a raw challenge column';
    END IF;

    SELECT count(*) INTO challenge_trigger_count
    FROM information_schema.triggers
    WHERE event_object_schema = 'identity_access'
      AND event_object_table = 'webauthn_authentication_challenges'
      AND trigger_name = 'trg_security_mutation_webauthn_authentication_challenges';

    IF challenge_trigger_count <> 3 THEN
        RAISE EXCEPTION 'WebAuthn authentication challenge mutation-ledger trigger coverage is incomplete';
    END IF;

    IF NOT EXISTS
    (
        SELECT 1
        FROM information_schema.table_constraints
        WHERE table_schema = 'identity_access'
          AND table_name = 'webauthn_authentication_challenges'
          AND constraint_name = 'fk_webauthn_authentication_challenges_user'
          AND constraint_type = 'FOREIGN KEY'
    ) THEN
        RAISE EXCEPTION 'WebAuthn authentication challenge user foreign key is missing';
    END IF;
END
`$verify`$;
"@

& psql -U $postgresUser -v ON_ERROR_STOP=1 -d $databaseName -c $sql
if ($LASTEXITCODE -ne 0) {
    throw "WebAuthn authentication validation failed with exit code $LASTEXITCODE."
}

Write-Host "WebAuthn authentication PostgreSQL validation passed."
