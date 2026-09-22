DO $$
BEGIN
    IF EXISTS (
        SELECT 1 FROM information_schema.columns
        WHERE table_schema = 'identity_access'
          AND table_name = 'application_capabilities'
          AND column_name = 'capability_namespace'
    ) AND NOT EXISTS (
        SELECT 1 FROM information_schema.columns
        WHERE table_schema = 'identity_access'
          AND table_name = 'application_capabilities'
          AND column_name = 'capability_feature'
    ) THEN
        ALTER TABLE identity_access.application_capabilities
            RENAME COLUMN capability_resource TO capability_feature;
        ALTER TABLE identity_access.application_capabilities
            RENAME COLUMN capability_namespace TO capability_resource;
    END IF;
END
$$;

DO $$
BEGIN
    IF EXISTS (
        SELECT 1 FROM information_schema.columns
        WHERE table_schema = 'identity_access'
          AND table_name = 'policy_statements'
          AND column_name = 'capability_namespace'
    ) AND NOT EXISTS (
        SELECT 1 FROM information_schema.columns
        WHERE table_schema = 'identity_access'
          AND table_name = 'policy_statements'
          AND column_name = 'capability_feature'
    ) THEN
        ALTER TABLE identity_access.policy_statements
            RENAME COLUMN capability_resource TO capability_feature;
        ALTER TABLE identity_access.policy_statements
            RENAME COLUMN capability_namespace TO capability_resource;
    END IF;
END
$$;

DO $$
BEGIN
    IF EXISTS (
        SELECT 1 FROM pg_constraint c
        JOIN pg_class t ON t.oid = c.conrelid
        JOIN pg_namespace n ON n.oid = t.relnamespace
        WHERE n.nspname = 'identity_access'
          AND t.relname = 'application_capabilities'
          AND c.conname = 'ck_application_capabilities_resource'
    ) AND NOT EXISTS (
        SELECT 1 FROM pg_constraint c
        JOIN pg_class t ON t.oid = c.conrelid
        JOIN pg_namespace n ON n.oid = t.relnamespace
        WHERE n.nspname = 'identity_access'
          AND t.relname = 'application_capabilities'
          AND c.conname = 'ck_application_capabilities_feature'
    ) THEN
        ALTER TABLE identity_access.application_capabilities
            RENAME CONSTRAINT ck_application_capabilities_resource TO ck_application_capabilities_feature;
    END IF;

    IF EXISTS (
        SELECT 1 FROM pg_constraint c
        JOIN pg_class t ON t.oid = c.conrelid
        JOIN pg_namespace n ON n.oid = t.relnamespace
        WHERE n.nspname = 'identity_access'
          AND t.relname = 'application_capabilities'
          AND c.conname = 'ck_application_capabilities_namespace'
    ) AND NOT EXISTS (
        SELECT 1 FROM pg_constraint c
        JOIN pg_class t ON t.oid = c.conrelid
        JOIN pg_namespace n ON n.oid = t.relnamespace
        WHERE n.nspname = 'identity_access'
          AND t.relname = 'application_capabilities'
          AND c.conname = 'ck_application_capabilities_resource'
    ) THEN
        ALTER TABLE identity_access.application_capabilities
            RENAME CONSTRAINT ck_application_capabilities_namespace TO ck_application_capabilities_resource;
    END IF;
END
$$;
