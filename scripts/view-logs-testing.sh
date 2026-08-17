#!/bin/bash

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
# shellcheck source=lib/testing-deploy.sh
. "${SCRIPT_DIR}/lib/testing-deploy.sh"

SERVICE="${1:-all}"
LINES="${2:-100}"

echo "========================================"
echo "  Finance System Testing Logs"
echo "========================================"
echo

case "${SERVICE}" in
    api|backend)
        log_info "Showing testing API logs (last ${LINES} lines)..."
        docker logs --tail "${LINES}" -f "${TESTING_API_CONTAINER}"
        ;;
    web|frontend)
        log_info "Showing testing web logs (last ${LINES} lines)..."
        docker logs --tail "${LINES}" -f "${TESTING_WEB_CONTAINER}"
        ;;
    db|database|postgres)
        log_info "Showing testing database logs (last ${LINES} lines)..."
        docker logs --tail "${LINES}" -f "${TESTING_DB_CONTAINER}"
        ;;
    all)
        log_info "Showing recent logs for all testing services..."
        echo
        echo "--- Testing API ---"
        docker logs --tail "${LINES}" "${TESTING_API_CONTAINER}"
        echo
        echo "--- Testing Web ---"
        docker logs --tail "${LINES}" "${TESTING_WEB_CONTAINER}"
        if docker ps -a --format '{{.Names}}' | grep -q "^${TESTING_DB_CONTAINER}$"; then
            echo
            echo "--- Testing Database ---"
            docker logs --tail "${LINES}" "${TESTING_DB_CONTAINER}"
        fi
        ;;
    *)
        echo "Usage: $0 [api|web|db|all] [lines]"
        exit 1
        ;;
esac
