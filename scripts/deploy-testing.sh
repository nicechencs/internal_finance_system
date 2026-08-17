#!/bin/bash

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
# shellcheck source=lib/testing-deploy.sh
. "${SCRIPT_DIR}/lib/testing-deploy.sh"

require_testing_env
require_testing_github_repo_owner

log_info "Deploying Finance System (testing)..."
log_info "Image registry: ghcr.io/${GITHUB_REPO_OWNER}"

log_info "Pulling latest testing images..."
testing_compose pull api web || log_warn "Could not pull latest images, using local cache"

log_info "Stopping existing testing api/web containers..."
testing_compose stop api web || true

log_info "Removing existing testing api/web containers..."
testing_compose rm -f api web || true

log_info "Starting testing services..."
testing_compose up -d

log_info "Waiting for services to boot..."
sleep 10

log_info "Container status:"
testing_compose ps

HOST_IP="$(testing_host_ip)"
WEB_PORT_VALUE="$(testing_web_port)"

log_info "Testing deployment finished."
log_info "Frontend URL: http://${HOST_IP}:${WEB_PORT_VALUE}"
log_info "API URL via web proxy: http://${HOST_IP}:${WEB_PORT_VALUE}/api"
log_warn "Run ./scripts/health-check-testing.sh to validate the testing deployment."
