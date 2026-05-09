#!/bin/bash
# Runs ONCE when postgres container starts for the first time.
# Creates je_auth and je_jobs databases.
set -e
psql -v ON_ERROR_STOP=1 --username "$POSTGRES_USER" <<-EOSQL
    CREATE DATABASE je_auth;
    CREATE DATABASE je_jobs;
    GRANT ALL PRIVILEGES ON DATABASE je_auth TO $POSTGRES_USER;
    GRANT ALL PRIVILEGES ON DATABASE je_jobs TO $POSTGRES_USER;
EOSQL
echo "Created databases: je_auth, je_jobs"