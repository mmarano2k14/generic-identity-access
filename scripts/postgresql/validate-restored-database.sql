\set ON_ERROR_STOP on

DO $$
DECLARE
    required_table_name text;
BEGIN
    IF to_regnamespace('identity_access') IS NULL THEN
        RAISE EXCEPTION 'identity_access schema is missing';
    END IF;

    FOREACH required_table_name IN ARRAY ARRAY[
        'users',
        'tenants',
        'tenant_memberships',
        'user_groups',
        'group_memberships',
        'application_security_models',
        'application_capabilities',
        'permission_policies',
        'policy_statements',
        'group_policy_bindings',
        'managed_policies',
        'managed_policy_versions',
        'managed_policy_statements',
        'managed_group_policy_bindings',
        'application_scope_types',
        'resource_scopes',
        'password_credentials',
        'user_sessions',
        'security_events',
        'security_mutation_events',
        'identity_scope_administration_groups',
        'identity_scope_administration_group_memberships',
        'identity_scope_administration_policies',
        'identity_scope_administration_policy_statements',
        'identity_scope_administration_group_policy_bindings',
        'oidc_authorization_codes',
        'oidc_refresh_tokens',
        'mfa_policies',
        'mfa_policy_providers',
        'user_authenticators',
        'schema_migrations'
    ]
    LOOP
        IF to_regclass(format('identity_access.%I', required_table_name)) IS NULL THEN
            RAISE EXCEPTION 'required restored table is missing: %', required_table_name;
        END IF;
    END LOOP;


    IF to_regclass('identity_access.group_templates') IS NOT NULL THEN
        RAISE EXCEPTION 'retired group_templates table exists in restored database';
    END IF;

    IF NOT EXISTS
    (
        SELECT 1
        FROM information_schema.columns AS cols
        WHERE cols.table_schema = 'identity_access'
          AND cols.table_name = 'user_groups'
          AND cols.column_name = 'is_template'
          AND cols.data_type = 'boolean'
          AND cols.is_nullable = 'NO'
    ) THEN
        RAISE EXCEPTION 'restored user_groups.is_template is missing or nullable';
    END IF;

    IF EXISTS
    (
        SELECT 1
        FROM information_schema.columns AS cols
        WHERE cols.table_schema = 'identity_access'
          AND cols.table_name = 'user_groups'
          AND cols.column_name IN ('origin', 'template_id')
    ) THEN
        RAISE EXCEPTION 'retired group-template provenance columns exist in restored database';
    END IF;

    IF EXISTS
    (
        SELECT 1
        FROM identity_access.schema_migrations
        WHERE checksum IS NULL
           OR checksum !~ '^[0-9a-f]{64}$'
    ) THEN
        RAISE EXCEPTION 'restored migration metadata contains an invalid checksum';
    END IF;

    IF EXISTS
    (
        SELECT 1
        FROM pg_constraint c
        JOIN pg_namespace n ON n.oid = c.connamespace
        WHERE n.nspname = 'identity_access'
          AND NOT c.convalidated
    ) THEN
        RAISE EXCEPTION 'restored identity_access schema contains an unvalidated constraint';
    END IF;

    IF EXISTS
    (
        SELECT 1
        FROM pg_index i
        JOIN pg_class t ON t.oid = i.indrelid
        JOIN pg_namespace n ON n.oid = t.relnamespace
        WHERE n.nspname = 'identity_access'
          AND NOT i.indisvalid
    ) THEN
        RAISE EXCEPTION 'restored identity_access schema contains an invalid index';
    END IF;

    IF EXISTS
    (
        SELECT 1
        FROM information_schema.columns AS cols
        WHERE cols.table_schema = 'identity_access'
          AND cols.column_name IN
              ('password', 'session_token', 'refresh_token', 'authorization_code', 'connection_string', 'totp_secret', 'provider_payload', 'private_key', 'recovery_code')
    ) THEN
        RAISE EXCEPTION 'restored identity_access schema contains a forbidden raw-secret column';
    END IF;
END
$$;
