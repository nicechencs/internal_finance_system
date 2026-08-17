#!/bin/bash

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
# shellcheck source=lib/testing-deploy.sh
. "${SCRIPT_DIR}/lib/testing-deploy.sh"

if [ $# -eq 0 ]; then
    log_error "Backup file path is required."
    echo "Usage: $0 [--yes] <backup_file>"
    exit 1
fi

SKIP_CONFIRM=false
if [ "$1" = "--yes" ]; then
    SKIP_CONFIRM=true
    shift
fi

if [ $# -eq 0 ]; then
    log_error "Backup file path is required."
    echo "Usage: $0 [--yes] <backup_file>"
    exit 1
fi

BACKUP_FILE="$1"

if [ ! -f "${BACKUP_FILE}" ]; then
    log_error "Backup file not found: ${BACKUP_FILE}"
    exit 1
fi

require_testing_env

if [ -z "${DB_HOST:-}" ] || [ -z "${DB_NAME:-}" ] || [ -z "${DB_USER:-}" ] || [ -z "${DB_PASSWORD:-}" ]; then
    log_error "Database settings are incomplete in ${TESTING_ENV_FILE}."
    exit 1
fi

log_warn "This operation will replace the current testing database."
if [ "${SKIP_CONFIRM}" != "true" ]; then
    read -r -p "Type 'yes' to continue: " confirm

    if [ "${confirm}" != "yes" ]; then
        log_info "Restore cancelled."
        exit 0
    fi
fi

TEMP_SQL_FILE=""
cleanup() {
    if [ -n "${TEMP_SQL_FILE}" ] && [ -f "${TEMP_SQL_FILE}" ]; then
        rm -f "${TEMP_SQL_FILE}"
    fi
}
trap cleanup EXIT

if [[ "${BACKUP_FILE}" == *.gz ]]; then
    TEMP_SQL_FILE="$(mktemp)"
    gzip -dc "${BACKUP_FILE}" > "${TEMP_SQL_FILE}"
else
    TEMP_SQL_FILE="${BACKUP_FILE}"
fi

log_info "Stopping testing API service..."
testing_compose stop api

log_info "Recreating database ${DB_NAME}..."
PGPASSWORD="${DB_PASSWORD}" psql -h "${DB_HOST}" -p "${DB_PORT:-5432}" -U "${DB_USER}" postgres -c "DROP DATABASE IF EXISTS \"${DB_NAME}\";"
PGPASSWORD="${DB_PASSWORD}" psql -h "${DB_HOST}" -p "${DB_PORT:-5432}" -U "${DB_USER}" postgres -c "CREATE DATABASE \"${DB_NAME}\";"

log_info "Restoring database contents..."
PGPASSWORD="${DB_PASSWORD}" psql -h "${DB_HOST}" -p "${DB_PORT:-5432}" -U "${DB_USER}" "${DB_NAME}" < "${TEMP_SQL_FILE}"

log_info "Starting testing API service..."
testing_compose start api

log_info "Testing database restore completed."
log_warn "Run ./scripts/health-check-testing.sh to verify the service."
