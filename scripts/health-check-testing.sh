#!/bin/bash

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
# shellcheck source=lib/testing-deploy.sh
. "${SCRIPT_DIR}/lib/testing-deploy.sh"

FAILURES=0

report_error() {
    log_error "$1"
    FAILURES=$((FAILURES + 1))
}

load_testing_env || true
WEB_PORT_VALUE="$(testing_web_port)"

echo "========================================"
echo "  Finance System Testing Health Check"
echo "========================================"
echo

echo "1. Checking Docker..."
if command -v systemctl > /dev/null 2>&1; then
    if systemctl is-active --quiet docker; then
        log_info "Docker service is running."
    else
        report_error "Docker service is not running."
    fi
elif docker info > /dev/null 2>&1; then
    log_info "Docker daemon is reachable."
else
    report_error "Docker daemon is not reachable."
fi

echo
echo "2. Checking containers..."
check_container() {
    local container_name="$1"
    local service_name="$2"

    if docker ps --format '{{.Names}}' | grep -q "^${container_name}$"; then
        local status
        status="$(docker inspect --format='{{if .State.Health}}{{.State.Health.Status}}{{else}}{{.State.Status}}{{end}}' "${container_name}")"
        if [ "${status}" = "running" ] || [ "${status}" = "healthy" ]; then
            log_info "${service_name} container is ${status}."
        else
            report_error "${service_name} container status is ${status}."
        fi
    else
        report_error "${service_name} container is missing."
    fi
}

check_container "${TESTING_API_CONTAINER}" "Testing API"
check_container "${TESTING_WEB_CONTAINER}" "Testing Web"

if docker ps --format '{{.Names}}' | grep -q "^${TESTING_DB_CONTAINER}$"; then
    check_container "${TESTING_DB_CONTAINER}" "Testing Database"
else
    log_info "No local testing database container detected. Assuming external database."
fi

echo
echo "3. Checking database connectivity..."
if docker ps --format '{{.Names}}' | grep -q "^${TESTING_DB_CONTAINER}$"; then
    if docker exec "${TESTING_DB_CONTAINER}" pg_isready -U postgres > /dev/null 2>&1; then
        log_info "Local testing PostgreSQL is ready."
    else
        report_error "Local testing PostgreSQL is not ready."
    fi
else
    log_info "Database connectivity is covered by the API /health endpoint."
fi

echo
echo "4. Checking API health through the internal Docker network..."
if docker exec "${TESTING_WEB_CONTAINER}" curl -fsS http://api:8080/health > /dev/null 2>&1; then
    log_info "API /health is reachable from the testing web container."
else
    report_error "Testing web container cannot reach http://api:8080/health."
fi

echo
echo "5. Checking authenticated API route through the public web proxy..."
if command -v curl > /dev/null 2>&1; then
    AUTH_ME_STATUS="$(curl -sS -o /dev/null -w "%{http_code}" "http://localhost:${WEB_PORT_VALUE}/api/auth/me" || true)"
    if [ "${AUTH_ME_STATUS}" = "401" ]; then
        log_info "Proxy route /api/auth/me returns the expected 401 for an anonymous request."
    else
        report_error "Unexpected status ${AUTH_ME_STATUS} from http://localhost:${WEB_PORT_VALUE}/api/auth/me."
    fi
else
    log_warn "curl is not installed; skipping proxy auth route check."
fi

echo
echo "6. Checking frontend health..."
if command -v curl > /dev/null 2>&1; then
    if curl -fsS "http://localhost:${WEB_PORT_VALUE}/" > /dev/null; then
        log_info "Frontend is reachable on localhost:${WEB_PORT_VALUE}."
    else
        report_error "Frontend is not reachable on localhost:${WEB_PORT_VALUE}."
    fi
else
    log_warn "curl is not installed; skipping frontend HTTP check."
fi

echo
echo "7. Checking disk usage..."
DISK_USAGE="$(df -h / | awk 'NR==2 {print $5}' | sed 's/%//')"
if [ "${DISK_USAGE}" -lt 80 ]; then
    log_info "Disk usage is ${DISK_USAGE}%."
elif [ "${DISK_USAGE}" -lt 90 ]; then
    log_warn "Disk usage is high at ${DISK_USAGE}%."
else
    log_warn "Disk usage is critically high at ${DISK_USAGE}%."
fi

echo
echo "8. Checking memory usage..."
MEM_USAGE="$(free | awk 'NR==2 {printf "%.0f", $3/$2 * 100}')"
if [ "${MEM_USAGE}" -lt 80 ]; then
    log_info "Memory usage is ${MEM_USAGE}%."
elif [ "${MEM_USAGE}" -lt 90 ]; then
    log_warn "Memory usage is high at ${MEM_USAGE}%."
else
    log_warn "Memory usage is critically high at ${MEM_USAGE}%."
fi

echo
echo "9. Recent error logs..."
echo
echo "--- Testing API ---"
docker logs --tail 20 "${TESTING_API_CONTAINER}" 2>&1 | grep -Ei "error|exception|fail" || log_info "No recent testing API error logs."
echo
echo "--- Testing Web ---"
docker logs --tail 20 "${TESTING_WEB_CONTAINER}" 2>&1 | grep -Ei "error|exception|fail" || log_info "No recent testing web error logs."

echo
echo "========================================"
if [ "${FAILURES}" -eq 0 ]; then
    echo "  Health check passed"
    echo "========================================"
    exit 0
fi

echo "  Health check failed (${FAILURES} issue(s))"
echo "========================================"
exit 1
