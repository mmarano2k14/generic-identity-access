DO $$
DECLARE
    definition text;
BEGIN
    SELECT pg_get_constraintdef(c.oid)
    INTO definition
    FROM pg_constraint AS c
    INNER JOIN pg_class AS t ON t.oid = c.conrelid
    INNER JOIN pg_namespace AS n ON n.oid = t.relnamespace
    WHERE n.nspname = 'identity_access'
      AND t.relname = 'oidc_refresh_tokens'
      AND c.conname = 'ck_oidc_refresh_tokens_revocation_reason';

    IF definition IS NULL THEN
        RAISE EXCEPTION 'Refresh-token revocation-reason constraint is missing.';
    END IF;

    IF position('reuse_detected' in definition) = 0 OR
       position('credential_changed' in definition) = 0 OR
       position('account_recovery' in definition) = 0 THEN
        RAISE EXCEPTION 'Credential-security revocation reasons are incomplete: %', definition;
    END IF;
END
$$;

SELECT 'credential-security-hardening-ok' AS validation_result;
