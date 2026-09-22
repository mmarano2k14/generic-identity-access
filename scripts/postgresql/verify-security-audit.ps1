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

$sql = @'
DO $$
BEGIN
    IF to_regclass('identity_access.security_events') IS NULL THEN
        RAISE EXCEPTION 'security_events table is missing';
    END IF;

    IF EXISTS
    (
        SELECT 1
        FROM information_schema.columns
        WHERE table_schema = 'identity_access'
          AND table_name = 'security_events'
          AND column_name IN
              ('password', 'password_hash', 'session_token', 'connection_string', 'secret_reference')
    ) THEN
        RAISE EXCEPTION 'security_events contains a forbidden sensitive column';
    END IF;
END
$$;
'@

& psql -U $postgresUser -v ON_ERROR_STOP=1 -d $databaseName -c $sql

if ($LASTEXITCODE -ne 0) {
    throw "Security audit schema verification failed."
}

Write-Host "Security audit schema verification passed."
