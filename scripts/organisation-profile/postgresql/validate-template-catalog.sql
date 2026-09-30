DO $$
BEGIN
    IF to_regclass(
        'organisation_profile.organisation_profile_templates') IS NULL THEN
        RAISE EXCEPTION 'organisation_profile.organisation_profile_templates is missing';
    END IF;

    IF to_regclass(
        'organisation_profile.organisation_profile_template_versions') IS NULL THEN
        RAISE EXCEPTION 'organisation_profile.organisation_profile_template_versions is missing';
    END IF;

    IF to_regclass(
        'organisation_profile.organisation_profile_template_domains') IS NULL THEN
        RAISE EXCEPTION 'organisation_profile.organisation_profile_template_domains is missing';
    END IF;

    IF NOT EXISTS
    (
        SELECT 1
        FROM pg_constraint
        WHERE conname = 'fk_organisation_profiles_template_version'
          AND conrelid =
            'organisation_profile.organisation_profiles'::regclass
    ) THEN
        RAISE EXCEPTION 'Profile/template-version FK is missing';
    END IF;

    IF NOT EXISTS
    (
        SELECT 1
        FROM pg_trigger
        WHERE tgname =
            'trg_organisation_profile_template_version_immutable'
          AND NOT tgisinternal
    ) THEN
        RAISE EXCEPTION 'Published-version immutability trigger is missing';
    END IF;

    IF NOT EXISTS
    (
        SELECT 1
        FROM pg_trigger
        WHERE tgname =
            'trg_organisation_profile_template_domain_mutable'
          AND NOT tgisinternal
    ) THEN
        RAISE EXCEPTION 'Published-domain immutability trigger is missing';
    END IF;

    IF NOT EXISTS
    (
        SELECT 1
        FROM pg_trigger
        WHERE tgname =
            'trg_organisation_profile_template_assignment_published'
          AND NOT tgisinternal
    ) THEN
        RAISE EXCEPTION 'Published-only profile assignment trigger is missing';
    END IF;
END;
$$;
