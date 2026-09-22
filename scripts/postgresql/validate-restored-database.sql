\set ON_ERROR_STOP on

DO $$
DECLARE
    table_name text;
BEGIN
    IF to_regnamespace('identity_access') IS NULL THEN
        RAISE EXCEPTION 'identity_access schema is missing';
    END IF;

    FOREACH table_name IN ARRAY ARRAY[
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
        'schema_migrations'
    ]
    LOOP
        IF to_regclass(format('identity_access.%I', table_name)) IS NULL THEN
            RAISE EXCEPTION 'required restored table is missing: %', table_name;
        END IF;
    END LOOP;

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
        FROM information_schema.columns
        WHERE table_schema = 'identity_access'
          AND column_name IN
              ('password', 'session_token', 'refresh_token', 'authorization_code', 'connection_string')
    ) THEN
        RAISE EXCEPTION 'restored identity_access schema contains a forbidden raw-secret column';
    END IF;
END
$$;
