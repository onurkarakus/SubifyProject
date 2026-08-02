#!/usr/bin/env bash
# Subify OS — Postgres backup (custom format).
# Usage (from repo root or docker/):
#   ./docker/scripts/backup-postgres.sh
#   CONTAINER=subify_postgres OUT_DIR=./backups ./docker/scripts/backup-postgres.sh
set -euo pipefail

CONTAINER="${CONTAINER:-subify_postgres}"
USER_NAME="${POSTGRES_USER:-subify_admin}"
DB_NAME="${POSTGRES_DB:-subify_db}"
OUT_DIR="${OUT_DIR:-./backups}"
STAMP="$(date +%Y%m%d-%H%M%S)"
FILE_NAME="subify-${STAMP}.dump"
REMOTE="/tmp/${FILE_NAME}"

mkdir -p "${OUT_DIR}"

if ! docker ps --format '{{.Names}}' | grep -qx "${CONTAINER}"; then
  echo "Container '${CONTAINER}' is not running." >&2
  exit 1
fi

echo "Dumping ${DB_NAME} from ${CONTAINER}..."
docker exec -t "${CONTAINER}" pg_dump \
  -U "${USER_NAME}" \
  -d "${DB_NAME}" \
  --format=custom \
  -f "${REMOTE}"

docker cp "${CONTAINER}:${REMOTE}" "${OUT_DIR}/${FILE_NAME}"
docker exec -t "${CONTAINER}" rm -f "${REMOTE}" || true

echo "Wrote ${OUT_DIR}/${FILE_NAME}"
ls -lh "${OUT_DIR}/${FILE_NAME}"
