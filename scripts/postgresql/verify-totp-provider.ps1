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
    protected_secret_type text;
    trigger_count integer;
BEGIN
    IF to_regclass('identity_access.totp_authenticators') IS NULL THEN
        RAISE EXCEPTION 'TOTP provider table is missing';
    END IF;

    SELECT data_type INTO protected_secret_type
    FROM information_schema.columns
    WHERE table_schema = 'identity_access'
      AND table_name = 'totp_authenticators'
      AND column_name = 'protected_secret';

    IF protected_secret_type <> 'bytea' THEN
        RAISE EXCEPTION 'TOTP protected secret must use bytea storage';
    END IF;

    IF EXISTS
    (
        SELECT 1
        FROM information_schema.columns
        WHERE table_schema = 'identity_access'
          AND table_name = 'user_authenticators'
          AND column_name ILIKE '%totp%'
    ) THEN
        RAISE EXCEPTION 'Generic authenticator metadata contains TOTP-specific columns';
    END IF;

    SELECT count(*) INTO trigger_count
    FROM information_schema.triggers
    WHERE event_object_schema = 'identity_access'
      AND event_object_table = 'totp_authenticators'
      AND trigger_name = 'trg_security_mutation_totp_authenticators';

    IF trigger_count <> 3 THEN
        RAISE EXCEPTION 'TOTP mutation-ledger trigger coverage is incomplete';
    END IF;
END
`$verify`$;
"@

& psql -U $postgresUser -v ON_ERROR_STOP=1 -d $databaseName -c $sql
if ($LASTEXITCODE -ne 0) {
    throw "TOTP provider validation failed with exit code $LASTEXITCODE."
}

Write-Host "TOTP provider PostgreSQL validation passed."
