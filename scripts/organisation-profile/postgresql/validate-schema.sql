DO $$
BEGIN
    IF to_regclass('organisation_profile.organisation_profiles') IS NULL THEN
        RAISE EXCEPTION 'organisation_profile.organisation_profiles is missing';
    END IF;

    IF NOT EXISTS
    (
        SELECT 1
        FROM pg_constraint
        WHERE conname = 'pk_organisation_profiles'
          AND conrelid = 'organisation_profile.organisation_profiles'::regclass
    ) THEN
        RAISE EXCEPTION 'pk_organisation_profiles is missing';
    END IF;

    IF NOT EXISTS
    (
        SELECT 1
        FROM pg_constraint
        WHERE conname = 'uq_organisation_profiles_organization'
          AND conrelid = 'organisation_profile.organisation_profiles'::regclass
    ) THEN
        RAISE EXCEPTION 'Organization uniqueness constraint is missing';
    END IF;

    IF NOT EXISTS
    (
        SELECT 1
        FROM pg_constraint
        WHERE conname = 'fk_organisation_profiles_organization'
          AND conrelid = 'organisation_profile.organisation_profiles'::regclass
    ) THEN
        RAISE EXCEPTION 'Organization Directory foreign key is missing';
    END IF;

    IF NOT EXISTS
    (
        SELECT 1
        FROM pg_constraint
        WHERE conname = 'ck_organisation_profiles_template_pin'
          AND conrelid = 'organisation_profile.organisation_profiles'::regclass
    ) THEN
        RAISE EXCEPTION 'Template pin consistency constraint is missing';
    END IF;

    IF NOT EXISTS
    (
        SELECT 1
        FROM pg_constraint
        WHERE conname = 'ck_organisation_profiles_row_version'
          AND conrelid = 'organisation_profile.organisation_profiles'::regclass
    ) THEN
        RAISE EXCEPTION 'Row-version constraint is missing';
    END IF;
END;
$$;
