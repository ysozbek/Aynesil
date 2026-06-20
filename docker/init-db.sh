#!/bin/bash
# Creates the least-privilege application role (subject to RLS).
# Run by the PostgreSQL Docker container on first startup.
# The owner role (POSTGRES_USER) is created automatically by the postgres image.
set -e

psql -v ON_ERROR_STOP=1 --username "$POSTGRES_USER" --dbname "$POSTGRES_DB" <<-EOSQL
  -- Application role: cannot bypass RLS, cannot own objects
  DO \$\$
  BEGIN
    IF NOT EXISTS (SELECT FROM pg_roles WHERE rolname = '${AYNESIL_DB_APP_USER:-aynesil_app}') THEN
      CREATE ROLE ${AYNESIL_DB_APP_USER:-aynesil_app} WITH LOGIN PASSWORD '${AYNESIL_DB_APP_PASSWORD:-changeme_app}' NOCREATEDB NOCREATEROLE;
    END IF;
  END
  \$\$;

  -- Grants (run after schema creation via migrations)
  -- GRANT USAGE ON SCHEMA core, iam, ref TO ${AYNESIL_DB_APP_USER:-aynesil_app};
  -- GRANT SELECT, INSERT, UPDATE, DELETE ON ALL TABLES IN SCHEMA core TO ${AYNESIL_DB_APP_USER:-aynesil_app};
  -- (Full grants are in db/roles/app_role_grants.sql — apply after migrations)
EOSQL
