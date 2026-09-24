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
    nullable_count integer;
BEGIN
    IF NOT EXISTS
    (
        SELECT 1 FROM information_schema.columns
        WHERE table_schema = 'identity_access'
          AND table_name = 'user_sessions'
          AND column_name = 'assurance_level'
          AND data_type = 'smallint'
    ) THEN
        RAISE EXCEPTION 'user_sessions assurance_level is missing or invalid';
    END IF;

    IF NOT EXISTS
    (
        SELECT 1 FROM information_schema.columns
        WHERE table_schema = 'identity_access'
          AND table_name = 'user_sessions'
          AND column_name = 'assurance_methods'
          AND data_type = 'ARRAY'
    ) THEN
        RAISE EXCEPTION 'user_sessions assurance_methods is missing or invalid';
    END IF;

    IF NOT EXISTS
    (
        SELECT 1 FROM information_schema.columns
        WHERE table_schema = 'identity_access'
          AND table_name = 'user_sessions'
          AND column_name = 'assurance_verified_at'
          AND data_type = 'timestamp with time zone'
    ) THEN
        RAISE EXCEPTION 'user_sessions assurance_verified_at is missing or invalid';
    END IF;

    SELECT count(*) INTO nullable_count
    FROM information_schema.columns
    WHERE table_schema = 'identity_access'
      AND (
          (table_name = 'user_sessions' AND column_name IN ('assurance_level', 'assurance_methods', 'assurance_verified_at'))
          OR (table_name IN ('oidc_authorization_codes', 'oidc_refresh_tokens') AND column_name IN ('assurance_level', 'assurance_methods'))
      )
      AND is_nullable <> 'NO';

    IF nullable_count <> 0 THEN
        RAISE EXCEPTION 'Authentication assurance columns must be non-nullable after migration';
    END IF;

    IF NOT EXISTS
    (
        SELECT 1 FROM information_schema.table_constraints
        WHERE table_schema = 'identity_access'
          AND table_name = 'user_sessions'
          AND constraint_name = 'ck_user_sessions_assurance_methods'
          AND constraint_type = 'CHECK'
    ) THEN
        RAISE EXCEPTION 'user_sessions assurance method constraint is missing';
    END IF;
END
`$verify`$;
"@

& psql -U $postgresUser -v ON_ERROR_STOP=1 -d $databaseName -c $sql
if ($LASTEXITCODE -ne 0) {
    throw "Session assurance PostgreSQL validation failed with exit code $LASTEXITCODE."
}

Write-Host "Session assurance PostgreSQL validation passed."
