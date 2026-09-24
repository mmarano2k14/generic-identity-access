ALTER TABLE identity_access.oidc_refresh_tokens
    DROP CONSTRAINT ck_oidc_refresh_tokens_revocation_reason;

ALTER TABLE identity_access.oidc_refresh_tokens
    ADD CONSTRAINT ck_oidc_refresh_tokens_revocation_reason
        CHECK
        (
            revocation_reason IS NULL
            OR revocation_reason IN
            (
                'reuse_detected',
                'credential_changed',
                'account_recovery'
            )
        );
