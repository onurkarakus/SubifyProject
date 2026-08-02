#!/usr/bin/env bash
# Subify OS — restore custom-format dump. DESTRUCTIVE for target DB objects.
# Usage:
#   ./docker/scripts/restore-postgres.sh ./backups/subify-YYYYMMDD-HHMMSS.dump
set -euo pipefail

if [[ $# -lt 1 ]]; then
  echo "Usage: $0 <path-to.dump>" >&2
  exit 1
fi

DUMP_PATH="$1"
CONTAINER="${CONTAINER:-subify_postgres}"
USER_NAME="${POSTGRES_USER:-subify_admin}"
DB_NAME="${POSTGRES_DB:-subify_db}"
REMOTE="/tmp/subify-restore.dump"

if [[ ! -f "${DUMP_PATH}" ]]; then
  echo "File not found: ${DUMP_PATH}" >&2
  exit 1
fi

if ! docker ps --format '{{.Names}}' | grep -qx "${CONTAINER}"; then
  echo "Container '${CONTAINER}' is not running." >&2
  exit 1
fi

echo "WARNING: This will restore into ${DB_NAME} with --clean --if-exists."
echo "Stop API/web first if possible: docker compose stop api web"
read -r -p "Type 'yes' to continue: " confirm
if [[ "${confirm}" != "yes" ]]; then
  echo "Aborted."
  exit 1
fi

docker cp "${DUMP_PATH}" "${CONTAINER}:${REMOTE}"
docker exec -i "${CONTAINER}" pg_restore \
  -U "${USER_NAME}" \
  -d "${DB_NAME}" \
  --clean --if-exists \
  "${REMOTE}" || true
docker exec -t "${CONTAINER}" rm -f "${REMOTE}" || true

echo "Restore finished. Start services: docker compose start api web"
