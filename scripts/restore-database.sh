#!/bin/bash

set -euo pipefail

GREEN='\033[0;32m'
RED='\033[0;31m'
YELLOW='\033[1;33m'
NC='\033[0m'

log_info() {
    echo -e "${GREEN}[INFO]${NC} $1"
}

log_error() {
    echo -e "${RED}[ERROR]${NC} $1"
}

log_warn() {
    echo -e "${YELLOW}[WARN]${NC} $1"
}

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

if [ ! -f "$BACKUP_FILE" ]; then
    log_error "Backup file not found: $BACKUP_FILE"
    exit 1
fi

if [ ! -f .env.production ]; then
    log_error ".env.production file not found."
    exit 1
fi

set -a
. ./.env.production
set +a

GITHUB_REPO_OWNER="${GITHUB_REPO_OWNER:-your-github-username}"

if [ -z "${DB_HOST:-}" ] || [ -z "${DB_NAME:-}" ] || [ -z "${DB_USER:-}" ] || [ -z "${DB_PASSWORD:-}" ]; then
    log_error "Database settings are incomplete in .env.production."
    exit 1
fi

log_warn "This operation will replace the current database."
if [ "$SKIP_CONFIRM" != "true" ]; then
    read -r -p "Type 'yes' to continue: " confirm

    if [ "$confirm" != "yes" ]; then
        log_info "Restore cancelled."
        exit 0
    fi
fi

TEMP_SQL_FILE=""
cleanup() {
    if [ -n "$TEMP_SQL_FILE" ] && [ -f "$TEMP_SQL_FILE" ]; then
        rm -f "$TEMP_SQL_FILE"
    fi
}
trap cleanup EXIT

if [[ "$BACKUP_FILE" == *.gz ]]; then
    TEMP_SQL_FILE="$(mktemp)"
    gzip -dc "$BACKUP_FILE" > "$TEMP_SQL_FILE"
else
    TEMP_SQL_FILE="$BACKUP_FILE"
fi

log_info "Stopping API service..."
GITHUB_REPO_OWNER="${GITHUB_REPO_OWNER}" docker compose --env-file .env.production stop api

log_info "Recreating database ${DB_NAME}..."
PGPASSWORD="${DB_PASSWORD}" psql -h "${DB_HOST}" -p "${DB_PORT:-5432}" -U "${DB_USER}" postgres -c "DROP DATABASE IF EXISTS \"${DB_NAME}\";"
PGPASSWORD="${DB_PASSWORD}" psql -h "${DB_HOST}" -p "${DB_PORT:-5432}" -U "${DB_USER}" postgres -c "CREATE DATABASE \"${DB_NAME}\";"

log_info "Restoring database contents..."
PGPASSWORD="${DB_PASSWORD}" psql -h "${DB_HOST}" -p "${DB_PORT:-5432}" -U "${DB_USER}" "${DB_NAME}" < "$TEMP_SQL_FILE"

log_info "Starting API service..."
GITHUB_REPO_OWNER="${GITHUB_REPO_OWNER}" docker compose --env-file .env.production start api

log_info "Database restore completed."
log_warn "Run ./scripts/health-check.sh to verify the service."
