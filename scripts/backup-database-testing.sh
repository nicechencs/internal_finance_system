#!/bin/bash

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
# shellcheck source=lib/testing-deploy.sh
. "${SCRIPT_DIR}/lib/testing-deploy.sh"

require_testing_env

if [ -z "${DB_HOST:-}" ] || [ -z "${DB_NAME:-}" ] || [ -z "${DB_USER:-}" ] || [ -z "${DB_PASSWORD:-}" ]; then
    log_error "Database settings are incomplete. Check ${TESTING_ENV_FILE}."
    exit 1
fi

BACKUP_DIR_VALUE="$(resolve_testing_path "${BACKUP_DIR:-backups/testing}")"
mkdir -p "${BACKUP_DIR_VALUE}"

TIMESTAMP="$(date +"%Y%m%d_%H%M%S")"
BACKUP_FILE="${BACKUP_DIR_VALUE}/finance_test_${TIMESTAMP}.sql"

log_info "Starting testing database backup..."
log_info "Database host: ${DB_HOST}"
log_info "Database name: ${DB_NAME}"

PGPASSWORD="${DB_PASSWORD}" pg_dump \
    -h "${DB_HOST}" \
    -p "${DB_PORT:-5432}" \
    -U "${DB_USER}" \
    "${DB_NAME}" > "${BACKUP_FILE}"

gzip "${BACKUP_FILE}"
log_info "Backup created: ${BACKUP_FILE}.gz"

if ! gunzip -t "${BACKUP_FILE}.gz" 2>/dev/null; then
    log_error "Backup archive validation failed: ${BACKUP_FILE}.gz"
    rm -f "${BACKUP_FILE}.gz"
    exit 1
fi

log_info "Backup archive validation passed."

RETENTION_DAYS="${BACKUP_RETENTION_DAYS:-7}"
log_info "Removing backup files older than ${RETENTION_DAYS} days..."
find "${BACKUP_DIR_VALUE}" -name "finance_test_*.sql.gz" -mtime +"${RETENTION_DAYS}" -delete

log_info "Current backups:"
ls -lh "${BACKUP_DIR_VALUE}"/finance_test_*.sql.gz 2>/dev/null || echo "No backup files found."
