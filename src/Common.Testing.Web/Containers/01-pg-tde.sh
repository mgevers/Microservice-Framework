#!/bin/sh
# Sets up a new Percona Postgres cluster, for the first start only.
#
# Encryption (pg_tde):
# - Creates the extension in template1, so every new database (EF creates "gamestats") has it.
# - Makes tde_heap the default table access method for the roles, so the tables EF creates are encrypted.
# - Turns on WAL encryption. The server restarts after this script, which applies the setting.
#
# Access:
# - gamestats_app builds and changes the schema and writes the data (the message processor).
# - gamestats_api only reads the tags and adds audit entries (the API). The processor grants it that after each
#   migration, because the tables do not exist yet.
# - If /certs/server.crt exists, the server requires TLS and password (SCRAM) login for every network connection.
#
# The file key provider (the fallback) keeps the keys unencrypted, in the data directory. Use it for tests only.
set -e

KEYS_DIR="$PGDATA/keys"
DB_USER="${POSTGRES_USER:-postgres}"
: "${GAMESTATS_APP_PASSWORD:?GAMESTATS_APP_PASSWORD must be set}"
: "${GAMESTATS_API_PASSWORD:?GAMESTATS_API_PASSWORD must be set}"
mkdir -p "$KEYS_DIR"

# With PG_TDE_VAULT_URL set, the keys live in OpenBao (or Vault) and not on this disk.
# Without it, a file provider is used. That is for tests and local experiments only.
if [ -n "$PG_TDE_VAULT_URL" ]; then
  PROVIDER_SQL="SELECT pg_tde_add_global_key_provider_vault_v2('key-store', '$PG_TDE_VAULT_URL', 'pgtde', '/secrets/pgtde-token', '/certs/ca.crt');"
  PROVIDER=key-store
else
  PROVIDER_SQL="SELECT pg_tde_add_global_key_provider_file('dev-file-provider', '$KEYS_DIR/pg_tde.per');"
  PROVIDER=dev-file-provider
fi

psql -v ON_ERROR_STOP=1 --username "$DB_USER" --dbname template1 <<SQL
CREATE EXTENSION IF NOT EXISTS pg_tde;
$PROVIDER_SQL
SELECT pg_tde_create_key_using_global_key_provider('default-key', '$PROVIDER');
SELECT pg_tde_set_default_key_using_global_key_provider('default-key', '$PROVIDER');
SELECT pg_tde_create_key_using_global_key_provider('wal-key', '$PROVIDER');
SELECT pg_tde_set_server_key_using_global_key_provider('wal-key', '$PROVIDER');
ALTER SYSTEM SET pg_tde.wal_encrypt = on;

CREATE ROLE gamestats_app LOGIN CREATEDB PASSWORD '$GAMESTATS_APP_PASSWORD';
CREATE ROLE gamestats_api LOGIN PASSWORD '$GAMESTATS_API_PASSWORD';

ALTER ROLE "$DB_USER" SET default_table_access_method = tde_heap;
ALTER ROLE gamestats_app SET default_table_access_method = tde_heap;
ALTER ROLE gamestats_api SET default_table_access_method = tde_heap;
SQL

# Keycloak keeps its users and sessions in its own database, which is encrypted like the others.
if [ -n "$KEYCLOAK_DB_PASSWORD" ]; then
  psql -v ON_ERROR_STOP=1 --username "$DB_USER" --dbname template1 <<SQL
CREATE ROLE keycloak LOGIN PASSWORD '$KEYCLOAK_DB_PASSWORD';
ALTER ROLE keycloak SET default_table_access_method = tde_heap;
CREATE DATABASE keycloak OWNER keycloak;
SQL
fi

# The Accounts service has its own database and roles, built like the GameStats ones.
# accounts_app owns the schema and writes the data (the message processor). accounts_api only reads and adds audit entries.
if [ -n "$ACCOUNTS_APP_PASSWORD" ] && [ -n "$ACCOUNTS_API_PASSWORD" ]; then
  psql -v ON_ERROR_STOP=1 --username "$DB_USER" --dbname template1 <<SQL
CREATE ROLE accounts_app LOGIN CREATEDB PASSWORD '$ACCOUNTS_APP_PASSWORD';
CREATE ROLE accounts_api LOGIN PASSWORD '$ACCOUNTS_API_PASSWORD';
ALTER ROLE accounts_app SET default_table_access_method = tde_heap;
ALTER ROLE accounts_api SET default_table_access_method = tde_heap;
SQL
fi

# The backup job reads all the data and can change nothing.
if [ -n "$BACKUP_DB_PASSWORD" ]; then
  psql -v ON_ERROR_STOP=1 --username "$DB_USER" --dbname template1 <<SQL
CREATE ROLE gamestats_backup LOGIN PASSWORD '$BACKUP_DB_PASSWORD';
GRANT pg_read_all_data TO gamestats_backup;
SQL
fi

if [ -f /certs/server.crt ]; then
  TLS_DIR="$PGDATA/tls"
  mkdir -p "$TLS_DIR"
  cp /certs/server.crt "$TLS_DIR/server.crt"
  cp /certs/server.key "$TLS_DIR/server.key"
  chmod 600 "$TLS_DIR/server.key"

  psql -v ON_ERROR_STOP=1 --username "$DB_USER" --dbname template1 <<SQL
ALTER SYSTEM SET ssl = on;
ALTER SYSTEM SET ssl_cert_file = '$TLS_DIR/server.crt';
ALTER SYSTEM SET ssl_key_file = '$TLS_DIR/server.key';
ALTER SYSTEM SET ssl_min_protocol_version = 'TLSv1.2';
ALTER SYSTEM SET password_encryption = 'scram-sha-256';
SQL

  # Local connections (the container itself) keep their rules. Every network connection needs TLS and a password.
  sed -i '/^host /d;/^hostssl /d;/^hostnossl /d' "$PGDATA/pg_hba.conf"
  {
    echo "hostssl all all all scram-sha-256"
    echo "host    all all all reject"
  } >> "$PGDATA/pg_hba.conf"
fi
