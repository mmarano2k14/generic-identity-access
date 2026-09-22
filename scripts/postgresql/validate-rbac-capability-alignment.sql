DO $$
BEGIN
    IF EXISTS (
        SELECT 1 FROM information_schema.columns
        WHERE table_schema = 'identity_access'
          AND table_name IN ('application_capabilities', 'policy_statements')
          AND column_name = 'capability_namespace'
    ) THEN
        RAISE EXCEPTION 'Legacy capability_namespace column is still present.';
    END IF;

    IF (SELECT count(*) FROM information_schema.columns
        WHERE table_schema = 'identity_access'
          AND table_name = 'application_capabilities'
          AND column_name IN ('capability_resource', 'capability_feature', 'capability_action')) <> 3 THEN
        RAISE EXCEPTION 'application_capabilities is not aligned to resource/feature/action.';
    END IF;

    IF (SELECT count(*) FROM information_schema.columns
        WHERE table_schema = 'identity_access'
          AND table_name = 'policy_statements'
          AND column_name IN ('capability_resource', 'capability_feature', 'capability_action')) <> 3 THEN
        RAISE EXCEPTION 'policy_statements is not aligned to resource/feature/action.';
    END IF;
END
$$;

SELECT 'rbac-capability-alignment-ok' AS validation_result;
