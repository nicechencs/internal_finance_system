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

GITHUB_REPO_OWNER="${GITHUB_REPO_OWNER:-your-github-username}"

SERVICE="${1:-all}"

case "$SERVICE" in
    api|backend)
        log_info "Restarting API service..."
        GITHUB_REPO_OWNER="${GITHUB_REPO_OWNER}" docker compose --env-file .env.production restart api
        ;;
    web|frontend)
        log_info "Restarting web service..."
        GITHUB_REPO_OWNER="${GITHUB_REPO_OWNER}" docker compose --env-file .env.production restart web
        ;;
    all)
        log_info "Restarting all services..."
        GITHUB_REPO_OWNER="${GITHUB_REPO_OWNER}" docker compose --env-file .env.production restart
        ;;
    *)
        log_error "Unknown service: $SERVICE"
        echo "Usage: $0 [api|web|all]"
        exit 1
        ;;
esac

echo
log_info "Container status:"
GITHUB_REPO_OWNER="${GITHUB_REPO_OWNER}" docker compose --env-file .env.production ps
