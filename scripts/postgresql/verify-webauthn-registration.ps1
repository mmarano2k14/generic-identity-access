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
    public_key_type text;
    challenge_hash_type text;
    credential_trigger_count integer;
    challenge_trigger_count integer;
BEGIN
    IF to_regclass('identity_access.webauthn_registration_challenges') IS NULL THEN
        RAISE EXCEPTION 'WebAuthn registration challenge table is missing';
    END IF;

    IF to_regclass('identity_access.webauthn_credentials') IS NULL THEN
        RAISE EXCEPTION 'WebAuthn credential table is missing';
    END IF;

    SELECT data_type INTO challenge_hash_type
    FROM information_schema.columns
    WHERE table_schema = 'identity_access'
      AND table_name = 'webauthn_registration_challenges'
      AND column_name = 'challenge_hash';

    IF challenge_hash_type <> 'bytea' THEN
        RAISE EXCEPTION 'WebAuthn registration challenge hashes must use bytea storage';
    END IF;

    SELECT data_type INTO public_key_type
    FROM information_schema.columns
    WHERE table_schema = 'identity_access'
      AND table_name = 'webauthn_credentials'
      AND column_name = 'cose_public_key';

    IF public_key_type <> 'bytea' THEN
        RAISE EXCEPTION 'WebAuthn public credential keys must use bytea storage';
    END IF;

    IF EXISTS
    (
        SELECT 1
        FROM information_schema.columns
        WHERE table_schema = 'identity_access'
          AND table_name = 'webauthn_credentials'
          AND column_name ILIKE '%private%key%'
    ) THEN
        RAISE EXCEPTION 'WebAuthn provider persistence contains private-key material';
    END IF;

    IF EXISTS
    (
        SELECT 1
        FROM information_schema.columns
        WHERE table_schema = 'identity_access'
          AND table_name = 'user_authenticators'
          AND column_name ILIKE '%webauthn%'
    ) THEN
        RAISE EXCEPTION 'Generic authenticator metadata contains WebAuthn-specific columns';
    END IF;

    SELECT count(*) INTO challenge_trigger_count
    FROM information_schema.triggers
    WHERE event_object_schema = 'identity_access'
      AND event_object_table = 'webauthn_registration_challenges'
      AND trigger_name = 'trg_security_mutation_webauthn_registration_challenges';

    SELECT count(*) INTO credential_trigger_count
    FROM information_schema.triggers
    WHERE event_object_schema = 'identity_access'
      AND event_object_table = 'webauthn_credentials'
      AND trigger_name = 'trg_security_mutation_webauthn_credentials';

    IF challenge_trigger_count <> 3 OR credential_trigger_count <> 3 THEN
        RAISE EXCEPTION 'WebAuthn mutation-ledger trigger coverage is incomplete';
    END IF;
END
`$verify`$;
"@

& psql -U $postgresUser -v ON_ERROR_STOP=1 -d $databaseName -c $sql
if ($LASTEXITCODE -ne 0) {
    throw "WebAuthn registration validation failed with exit code $LASTEXITCODE."
}

Write-Host "WebAuthn registration PostgreSQL validation passed."
