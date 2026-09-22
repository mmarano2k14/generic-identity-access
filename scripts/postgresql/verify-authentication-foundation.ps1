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

# Use a single-quoted here-string so PostgreSQL dollar quoting ($$) is passed
# to psql unchanged. Backslash is not an escape character for '$' in PowerShell.
$sql = @'
DO $$
BEGIN
    IF to_regclass('identity_access.password_credentials') IS NULL THEN
        RAISE EXCEPTION 'password_credentials table is missing';
    END IF;

    IF to_regclass('identity_access.user_sessions') IS NULL THEN
        RAISE EXCEPTION 'user_sessions table is missing';
    END IF;

    IF NOT EXISTS (
        SELECT 1
        FROM information_schema.columns
        WHERE table_schema = 'identity_access'
          AND table_name = 'user_sessions'
          AND column_name = 'token_hash'
          AND data_type = 'bytea'
    ) THEN
        RAISE EXCEPTION 'user_sessions.token_hash bytea column is missing';
    END IF;

    IF EXISTS (
        SELECT 1
        FROM information_schema.columns
        WHERE table_schema = 'identity_access'
          AND table_name = 'user_sessions'
          AND column_name ILIKE '%token%'
          AND column_name <> 'token_hash'
    ) THEN
        RAISE EXCEPTION 'unexpected raw token column detected';
    END IF;
END
$$;
'@

& psql -U $postgresUser -v ON_ERROR_STOP=1 -d $databaseName -c $sql
if ($LASTEXITCODE -ne 0) {
    throw "Authentication schema verification failed."
}

Write-Host "Authentication schema verification passed."
