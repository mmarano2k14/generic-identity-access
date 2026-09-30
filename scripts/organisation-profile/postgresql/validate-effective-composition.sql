DO $$
BEGIN
    IF to_regclass(
        'organisation_profile.organisation_profile_domain_overrides') IS NULL THEN
        RAISE EXCEPTION 'organisation_profile.organisation_profile_domain_overrides is missing';
    END IF;

    IF to_regclass(
        'organisation_profile.organisation_profile_versions') IS NULL THEN
        RAISE EXCEPTION 'organisation_profile.organisation_profile_versions is missing';
    END IF;

    IF to_regclass(
        'organisation_profile.organisation_profile_version_domains') IS NULL THEN
        RAISE EXCEPTION 'organisation_profile.organisation_profile_version_domains is missing';
    END IF;

    IF NOT EXISTS
    (
        SELECT 1
        FROM pg_constraint
        WHERE conname =
            'fk_organisation_profile_domain_overrides_profile'
          AND conrelid =
            'organisation_profile.organisation_profile_domain_overrides'::regclass
    ) THEN
        RAISE EXCEPTION 'Override/profile FK is missing';
    END IF;

    IF NOT EXISTS
    (
        SELECT 1
        FROM pg_constraint
        WHERE conname =
            'fk_organisation_profile_versions_profile'
          AND conrelid =
            'organisation_profile.organisation_profile_versions'::regclass
    ) THEN
        RAISE EXCEPTION 'Effective-version/profile FK is missing';
    END IF;

    IF NOT EXISTS
    (
        SELECT 1
        FROM pg_constraint
        WHERE conname =
            'fk_organisation_profile_versions_template'
          AND conrelid =
            'organisation_profile.organisation_profile_versions'::regclass
    ) THEN
        RAISE EXCEPTION 'Effective-version/template FK is missing';
    END IF;

    IF NOT EXISTS
    (
        SELECT 1
        FROM pg_constraint
        WHERE conname =
            'fk_organisation_profile_version_domains_version'
          AND conrelid =
            'organisation_profile.organisation_profile_version_domains'::regclass
    ) THEN
        RAISE EXCEPTION 'Effective-version/domain FK is missing';
    END IF;

    IF NOT EXISTS
    (
        SELECT 1
        FROM pg_trigger
        WHERE tgname =
            'trg_organisation_profile_version_immutable'
          AND NOT tgisinternal
    ) THEN
        RAISE EXCEPTION 'Effective-version immutability trigger is missing';
    END IF;

    IF NOT EXISTS
    (
        SELECT 1
        FROM pg_trigger
        WHERE tgname =
            'trg_organisation_profile_version_domain_immutable'
          AND NOT tgisinternal
    ) THEN
        RAISE EXCEPTION 'Effective-version domain immutability trigger is missing';
    END IF;
END;
$$;
