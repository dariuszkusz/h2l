#!/usr/bin/env bash
set -euo pipefail

ATTACHMENTS_DIR="${ATTACHMENTS_STORAGE_PATH:-/data/attachments}"
mkdir -p "${ATTACHMENTS_DIR}"

# -----------------------------------------------------------------------------
# External PostgreSQL mode: DB_CONNECTION_STRING is provided
# -----------------------------------------------------------------------------
if [ -n "${DB_CONNECTION_STRING:-}" ]; then
  echo "[h2l] External DB_CONNECTION_STRING detected — skipping embedded PostgreSQL."
  exec dotnet /app/H2l.WebApi.dll "$@"
fi

# -----------------------------------------------------------------------------
# Embedded PostgreSQL mode (Quickstart / single-container docker run)
# -----------------------------------------------------------------------------
PGDATA="${PGDATA:-/data/postgres}"
PG_PORT="${EMBEDDED_PG_PORT:-5432}"
PG_USER="${POSTGRES_USER:-h2l}"
PG_DB="${POSTGRES_DB:-h2l}"
PG_PASSWORD_FILE="/data/.embedded-pg-password"

mkdir -p "${PGDATA}"
chmod 700 "${PGDATA}"

if [ -n "${POSTGRES_PASSWORD:-}" ]; then
  PG_PASSWORD="${POSTGRES_PASSWORD}"
elif [ -s "${PG_PASSWORD_FILE}" ]; then
  PG_PASSWORD="$(cat "${PG_PASSWORD_FILE}")"
else
  PG_PASSWORD="$(head -c 24 /dev/urandom | od -An -tx1 | tr -d ' \n')"
  umask 077
  printf '%s' "${PG_PASSWORD}" > "${PG_PASSWORD_FILE}"
fi

if [ ! -s "${PGDATA}/PG_VERSION" ]; then
  echo "[h2l] Initializing embedded PostgreSQL 17 cluster in ${PGDATA}..."
  PWFILE="$(mktemp)"
  chmod 600 "${PWFILE}"
  printf '%s' "${PG_PASSWORD}" > "${PWFILE}"

  initdb \
    --pgdata="${PGDATA}" \
    --username="${PG_USER}" \
    --encoding=UTF8 \
    --auth-local=scram-sha-256 \
    --auth-host=scram-sha-256 \
    --pwfile="${PWFILE}" >/dev/null

  rm -f "${PWFILE}"

  {
    echo "listen_addresses = '127.0.0.1'"
    echo "port = ${PG_PORT}"
    echo "unix_socket_directories = '/var/run/postgresql'"
  } >> "${PGDATA}/postgresql.conf"
fi

echo "[h2l] Starting embedded PostgreSQL 17 on 127.0.0.1:${PG_PORT}..."
pg_ctl \
  -D "${PGDATA}" \
  -o "-c listen_addresses=127.0.0.1 -c port=${PG_PORT} -c unix_socket_directories=/var/run/postgresql" \
  -w start >/dev/null

if ! PGPASSWORD="${PG_PASSWORD}" psql \
  -h 127.0.0.1 -p "${PG_PORT}" -U "${PG_USER}" -d postgres \
  -tAc "SELECT 1 FROM pg_database WHERE datname = '${PG_DB}'" | grep -q '^1$'; then
  echo "[h2l] Creating embedded database '${PG_DB}'..."
  PGPASSWORD="${PG_PASSWORD}" createdb -h 127.0.0.1 -p "${PG_PORT}" -U "${PG_USER}" "${PG_DB}"
fi

export DB_CONNECTION_STRING="Host=127.0.0.1;Port=${PG_PORT};Database=${PG_DB};Username=${PG_USER};Password=${PG_PASSWORD}"
export DB_AUTO_MIGRATE="${DB_AUTO_MIGRATE:-true}"

APP_PID=""

shutdown_handler() {
  echo "[h2l] Shutdown signal received — stopping application and embedded PostgreSQL..."
  if [ -n "${APP_PID}" ] && kill -0 "${APP_PID}" 2>/dev/null; then
    kill -TERM "${APP_PID}" 2>/dev/null || true
    wait "${APP_PID}" || true
  fi
  pg_ctl -D "${PGDATA}" -m fast stop >/dev/null 2>&1 || true
  exit 0
}

trap shutdown_handler TERM INT

dotnet /app/H2l.WebApi.dll "$@" &
APP_PID=$!

set +e
wait "${APP_PID}"
APP_EXIT=$?
set -e

pg_ctl -D "${PGDATA}" -m fast stop >/dev/null 2>&1 || true
exit "${APP_EXIT}"
