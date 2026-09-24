ALTER TABLE identity_access.user_sessions
    ADD COLUMN assurance_level smallint NULL,
    ADD COLUMN assurance_methods text[] NULL,
    ADD COLUMN assurance_verified_at timestamptz NULL;

UPDATE identity_access.user_sessions
SET assurance_level = 1,
    assurance_methods = ARRAY['pwd']::text[],
    assurance_verified_at = created_at;

ALTER TABLE identity_access.user_sessions
    ALTER COLUMN assurance_level SET NOT NULL,
    ALTER COLUMN assurance_methods SET NOT NULL,
    ALTER COLUMN assurance_verified_at SET NOT NULL;

ALTER TABLE identity_access.user_sessions
    ADD CONSTRAINT ck_user_sessions_assurance_level
        CHECK (assurance_level IN (1, 2)),
    ADD CONSTRAINT ck_user_sessions_assurance_methods
        CHECK
        (
            cardinality(assurance_methods) > 0
            AND array_position(assurance_methods, NULL) IS NULL
            AND
            (
                (assurance_level = 1 AND assurance_methods = ARRAY['pwd']::text[])
                OR
                (
                    assurance_level = 2
                    AND assurance_methods @> ARRAY['pwd', 'mfa']::text[]
                    AND cardinality(assurance_methods) >= 3
                )
            )
        ),
    ADD CONSTRAINT ck_user_sessions_assurance_time
        CHECK
        (
            assurance_verified_at >= created_at
            AND assurance_verified_at < expires_at
        );

ALTER TABLE identity_access.oidc_authorization_codes
    ADD COLUMN assurance_level smallint NULL,
    ADD COLUMN assurance_methods text[] NULL;

UPDATE identity_access.oidc_authorization_codes
SET assurance_level = 1,
    assurance_methods = ARRAY['pwd']::text[];

ALTER TABLE identity_access.oidc_authorization_codes
    ALTER COLUMN assurance_level SET NOT NULL,
    ALTER COLUMN assurance_methods SET NOT NULL;

ALTER TABLE identity_access.oidc_authorization_codes
    ADD CONSTRAINT ck_oidc_authorization_codes_assurance_level
        CHECK (assurance_level IN (1, 2)),
    ADD CONSTRAINT ck_oidc_authorization_codes_assurance_methods
        CHECK
        (
            cardinality(assurance_methods) > 0
            AND array_position(assurance_methods, NULL) IS NULL
            AND
            (
                (assurance_level = 1 AND assurance_methods = ARRAY['pwd']::text[])
                OR
                (
                    assurance_level = 2
                    AND assurance_methods @> ARRAY['pwd', 'mfa']::text[]
                    AND cardinality(assurance_methods) >= 3
                )
            )
        );

ALTER TABLE identity_access.oidc_refresh_tokens
    ADD COLUMN assurance_level smallint NULL,
    ADD COLUMN assurance_methods text[] NULL;

UPDATE identity_access.oidc_refresh_tokens
SET assurance_level = 1,
    assurance_methods = ARRAY['pwd']::text[];

ALTER TABLE identity_access.oidc_refresh_tokens
    ALTER COLUMN assurance_level SET NOT NULL,
    ALTER COLUMN assurance_methods SET NOT NULL;

ALTER TABLE identity_access.oidc_refresh_tokens
    ADD CONSTRAINT ck_oidc_refresh_tokens_assurance_level
        CHECK (assurance_level IN (1, 2)),
    ADD CONSTRAINT ck_oidc_refresh_tokens_assurance_methods
        CHECK
        (
            cardinality(assurance_methods) > 0
            AND array_position(assurance_methods, NULL) IS NULL
            AND
            (
                (assurance_level = 1 AND assurance_methods = ARRAY['pwd']::text[])
                OR
                (
                    assurance_level = 2
                    AND assurance_methods @> ARRAY['pwd', 'mfa']::text[]
                    AND cardinality(assurance_methods) >= 3
                )
            )
        );
