#!/bin/bash

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
# shellcheck source=lib/testing-deploy.sh
. "${SCRIPT_DIR}/lib/testing-deploy.sh"

require_testing_env
require_testing_github_repo_owner

SERVICE="${1:-all}"

case "${SERVICE}" in
    api|backend)
        log_info "Restarting testing API service..."
        testing_compose restart api
        ;;
    web|frontend)
        log_info "Restarting testing web service..."
        testing_compose restart web
        ;;
    all)
        log_info "Restarting all testing services..."
        testing_compose restart
        ;;
    *)
        log_error "Unknown service: ${SERVICE}"
        echo "Usage: $0 [api|web|all]"
        exit 1
        ;;
esac

echo
log_info "Container status:"
testing_compose ps
