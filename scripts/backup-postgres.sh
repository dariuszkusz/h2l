#!/usr/bin/env bash
# PostgreSQL backup & restore helper for self-hosted h2l deployments.
#
# Usage:
#   ./scripts/backup-postgres.sh backup [output_dir]
#   ./scripts/backup-postgres.sh restore <backup_file.dump> [--yes]
#
# Environment variables (loaded from .env if present):
#   POSTGRES_DB             Database name (default: h2l)
#   POSTGRES_USER           Database user (default: h2l)
#   POSTGRES_CONTAINER      Docker container name (default: h2l-postgres)
#   BACKUP_RETENTION_DAYS   Days to keep old dumps when pruning (default: 30, 0 = disable)
set -euo pipefail

if [ -f ".env" ]; then
  # shellcheck disable=SC1091
  set -a
  source ".env"
  set +a
fi

POSTGRES_DB="${POSTGRES_DB:-h2l}"
POSTGRES_USER="${POSTGRES_USER:-h2l}"
POSTGRES_CONTAINER="${POSTGRES_CONTAINER:-h2l-postgres}"
BACKUP_RETENTION_DAYS="${BACKUP_RETENTION_DAYS:-30}"

usage() {
  echo "Usage:"
  echo "  $0 backup [output_dir]"
  echo "  $0 restore <backup_file.dump> [--yes]"
  exit 1
}

require_container() {
  if ! docker ps --format '{{.Names}}' | grep -qx "${POSTGRES_CONTAINER}"; then
    echo "Error: PostgreSQL container '${POSTGRES_CONTAINER}' is not running." >&2
    exit 1
  fi
}

cmd_backup() {
  local output_dir="${1:-./backups}"
  mkdir -p "${output_dir}"
  require_container

  local timestamp
  timestamp="$(date -u +%Y%m%dT%H%M%SZ)"
  local dump_file="${output_dir}/h2l-${POSTGRES_DB}-${timestamp}.dump"

  echo "[h2l-backup] Creating compressed PostgreSQL dump: ${dump_file}"
  docker exec "${POSTGRES_CONTAINER}" \
    pg_dump -U "${POSTGRES_USER}" -d "${POSTGRES_DB}" --format=custom --compress=9 \
    > "${dump_file}"

  if command -v sha256sum >/dev/null 2>&1; then
    sha256sum "${dump_file}" > "${dump_file}.sha256"
  fi

  echo "[h2l-backup] Backup completed successfully."

  if [ "${BACKUP_RETENTION_DAYS}" -gt 0 ] 2>/dev/null; then
    find "${output_dir}" -type f \( -name "h2l-*.dump" -o -name "h2l-*.dump.sha256" \) \
      -mtime "+${BACKUP_RETENTION_DAYS}" -delete
    echo "[h2l-backup] Pruned backups older than ${BACKUP_RETENTION_DAYS} days."
  fi
}

cmd_restore() {
  local dump_file="${1:-}"
  local auto_confirm="${2:-}"

  if [ -z "${dump_file}" ] || [ ! -f "${dump_file}" ]; then
    echo "Error: Backup file not found: '${dump_file}'" >&2
    usage
  fi

  require_container

  if [ -f "${dump_file}.sha256" ] && command -v sha256sum >/dev/null 2>&1; then
    echo "[h2l-backup] Verifying SHA-256 checksum..."
    sha256sum --check "${dump_file}.sha256"
  fi

  if [ "${auto_confirm}" != "--yes" ]; then
    echo "WARNING: This will overwrite database '${POSTGRES_DB}' in container '${POSTGRES_CONTAINER}'."
    read -r -p "Type 'restore' to continue: " confirm
    if [ "${confirm}" != "restore" ]; then
      echo "[h2l-backup] Restore aborted."
      exit 1
    fi
  fi

  echo "[h2l-backup] Restoring '${dump_file}' into database '${POSTGRES_DB}'..."
  docker exec -i "${POSTGRES_CONTAINER}" \
    pg_restore -U "${POSTGRES_USER}" -d "${POSTGRES_DB}" \
    --clean --if-exists --no-owner --no-privileges \
    < "${dump_file}"

  echo "[h2l-backup] Restore completed successfully."
}

case "${1:-}" in
  backup)
    cmd_backup "${2:-./backups}"
    ;;
  restore)
    cmd_restore "${2:-}" "${3:-}"
    ;;
  *)
    usage
    ;;
esac
