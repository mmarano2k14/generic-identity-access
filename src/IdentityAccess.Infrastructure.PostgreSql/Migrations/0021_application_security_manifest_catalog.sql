-- Versioned application-owned security manifests and their RBAC context projection.
-- Capability authorization remains delegated to the external RBAC engine.

CREATE TABLE identity_access.application_security_model_registrations
(
    identity_scope_id uuid NOT NULL,
    application_key varchar(64) NOT NULL,
    model_version integer NOT NULL,
    manifest_schema_version integer NOT NULL,
    rbac_project varchar(128) NOT NULL,
    manifest_sha256 char(64) NOT NULL,
    registered_at timestamptz NOT NULL DEFAULT transaction_timestamp(),
    CONSTRAINT pk_application_security_model_registrations PRIMARY KEY
        (identity_scope_id, application_key, model_version),
    CONSTRAINT fk_application_security_model_registrations_model FOREIGN KEY
        (identity_scope_id, application_key, model_version)
        REFERENCES identity_access.application_security_models
        (identity_scope_id, application_key, model_version) ON DELETE RESTRICT,
    CONSTRAINT ck_application_security_model_registrations_schema_version
        CHECK (manifest_schema_version > 0),
    CONSTRAINT ck_application_security_model_registrations_rbac_project
        CHECK (
            length(btrim(rbac_project)) BETWEEN 1 AND 128
            AND rbac_project = lower(btrim(rbac_project))
            AND position(':' in rbac_project) = 0
            AND position('*' in rbac_project) = 0),
    CONSTRAINT ck_application_security_model_registrations_sha256
        CHECK (manifest_sha256 ~ '^[0-9a-f]{64}$')
);

CREATE TABLE identity_access.application_security_namespaces
(
    identity_scope_id uuid NOT NULL,
    application_key varchar(64) NOT NULL,
    model_version integer NOT NULL,
    rbac_namespace varchar(128) NOT NULL,
    registered_at timestamptz NOT NULL DEFAULT transaction_timestamp(),
    CONSTRAINT pk_application_security_namespaces PRIMARY KEY
        (identity_scope_id, application_key, model_version, rbac_namespace),
    CONSTRAINT fk_application_security_namespaces_registration FOREIGN KEY
        (identity_scope_id, application_key, model_version)
        REFERENCES identity_access.application_security_model_registrations
        (identity_scope_id, application_key, model_version) ON DELETE RESTRICT,
    CONSTRAINT ck_application_security_namespaces_value
        CHECK (
            length(btrim(rbac_namespace)) BETWEEN 1 AND 128
            AND rbac_namespace = lower(btrim(rbac_namespace))
            AND position(':' in rbac_namespace) = 0
            AND position('*' in rbac_namespace) = 0)
);

CREATE INDEX ix_application_security_model_registrations_application
    ON identity_access.application_security_model_registrations
       (identity_scope_id, application_key, model_version DESC);

CREATE TRIGGER trg_security_mutation_application_security_model_registrations
AFTER INSERT OR UPDATE OR DELETE
ON identity_access.application_security_model_registrations
FOR EACH ROW EXECUTE FUNCTION identity_access.capture_security_mutation(
    'identity_scope_id',
    'application_key',
    'model_version');

CREATE TRIGGER trg_security_mutation_application_security_namespaces
AFTER INSERT OR UPDATE OR DELETE
ON identity_access.application_security_namespaces
FOR EACH ROW EXECUTE FUNCTION identity_access.capture_security_mutation(
    'identity_scope_id',
    'application_key',
    'model_version',
    'rbac_namespace');
