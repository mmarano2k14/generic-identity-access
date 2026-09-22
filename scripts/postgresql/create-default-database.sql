\set ON_ERROR_STOP on

SELECT 'CREATE DATABASE generic_identity_access_default'
WHERE NOT EXISTS (
    SELECT 1 FROM pg_database WHERE datname = 'generic_identity_access_default'
)\gexec
