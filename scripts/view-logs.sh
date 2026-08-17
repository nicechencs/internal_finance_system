#!/bin/bash

set -euo pipefail

GREEN='\033[0;32m'
NC='\033[0m'

log_info() {
    echo -e "${GREEN}[INFO]${NC} $1"
}

SERVICE="${1:-all}"
LINES="${2:-100}"

echo "========================================"
echo "  Finance System Logs"
echo "========================================"
echo

case "$SERVICE" in
    api|backend)
        log_info "Showing API logs (last ${LINES} lines)..."
        docker logs --tail "$LINES" -f finance_api
        ;;
    web|frontend)
        log_info "Showing web logs (last ${LINES} lines)..."
        docker logs --tail "$LINES" -f finance_web
        ;;
    db|database|postgres)
        log_info "Showing database logs (last ${LINES} lines)..."
        docker logs --tail "$LINES" -f finance_db
        ;;
    all)
        log_info "Showing recent logs for all services..."
        echo
        echo "--- API ---"
        docker logs --tail "$LINES" finance_api
        echo
        echo "--- Web ---"
        docker logs --tail "$LINES" finance_web
        if docker ps -a --format '{{.Names}}' | grep -q '^finance_db$'; then
            echo
            echo "--- Database ---"
            docker logs --tail "$LINES" finance_db
        fi
        ;;
    *)
        echo "Usage: $0 [api|web|db|all] [lines]"
        exit 1
        ;;
esac
