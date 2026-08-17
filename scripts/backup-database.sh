#!/bin/bash

set -euo pipefail

GREEN='\033[0;32m'
RED='\033[0;31m'
NC='\033[0m'

log_info() {
    echo -e "${GREEN}[INFO]${NC} $1"
}

log_error() {
    echo -e "${RED}[ERROR]${NC} $1"
}

if [ ! -f .env.production ]; then
    log_error ".env.production file not found."
    exit 1
fi

set -a
. ./.env.production
set +a

if [ -z "${DB_HOST:-}" ] || [ -z "${DB_NAME:-}" ] || [ -z "${DB_USER:-}" ] || [ -z "${DB_PASSWORD:-}" ]; then
    log_error "Database settings are incomplete. Check .env.production."
    exit 1
fi

BACKUP_DIR="${BACKUP_DIR:-./backups}"
mkdir -p "$BACKUP_DIR"

TIMESTAMP="$(date +"%Y%m%d_%H%M%S")"
BACKUP_FILE="${BACKUP_DIR}/finance_${TIMESTAMP}.sql"

log_info "Starting database backup..."
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
find "${BACKUP_DIR}" -name "finance_*.sql.gz" -mtime +"${RETENTION_DAYS}" -delete

log_info "Current backups:"
ls -lh "${BACKUP_DIR}"/finance_*.sql.gz 2>/dev/null || echo "No backup files found."
