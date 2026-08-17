#!/bin/bash

set -euo pipefail

RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m'
DOTNET_CMD=""
DOCKER_CMD=""

log_info() {
    echo -e "${GREEN}[OK]${NC} $1"
}

log_error() {
    echo -e "${RED}[ERROR]${NC} $1"
}

log_warn() {
    echo -e "${YELLOW}[WARN]${NC} $1"
}

log_step() {
    echo -e "${BLUE}[STEP]${NC} $1"
}

echo "========================================"
echo "  Testing Deployment Simulation"
echo "========================================"
echo
log_warn "This script validates the testing deployment assets locally without touching the server."
echo

check_command() {
    local command_name="$1"
    if command -v "${command_name}" > /dev/null 2>&1; then
        log_info "${command_name} is available."
        return 0
    fi

    log_error "${command_name} is missing."
    return 1
}

resolve_command() {
    local primary="$1"
    local secondary="${2:-}"

    if command -v "${primary}" > /dev/null 2>&1; then
        printf '%s\n' "${primary}"
        return 0
    fi

    if [ -n "${secondary}" ] && command -v "${secondary}" > /dev/null 2>&1; then
        printf '%s\n' "${secondary}"
        return 0
    fi

    return 1
}

check_file() {
    local path="$1"
    if [ -f "${path}" ]; then
        log_info "${path} exists."
        return 0
    fi

    log_error "${path} is missing."
    return 1
}

MISSING_TOOLS=0
log_step "Checking required tools"
check_command git || MISSING_TOOLS=$((MISSING_TOOLS + 1))
check_command bash || MISSING_TOOLS=$((MISSING_TOOLS + 1))

DOTNET_CMD="$(resolve_command dotnet.exe dotnet || true)"
if [ -n "${DOTNET_CMD}" ]; then
    log_info "${DOTNET_CMD} is available."
else
    log_error "dotnet is missing."
    MISSING_TOOLS=$((MISSING_TOOLS + 1))
fi

DOCKER_CMD="$(resolve_command docker.exe docker || true)"
if [ -n "${DOCKER_CMD}" ]; then
    log_info "${DOCKER_CMD} is available."
else
    log_error "docker is missing."
    MISSING_TOOLS=$((MISSING_TOOLS + 1))
fi

if [ "${MISSING_TOOLS}" -gt 0 ]; then
    log_error "Install the missing tools and rerun the simulation."
    exit 1
fi

MISSING_FILES=0
log_step "Checking required files"
check_file ".github/workflows/deploy-testing.yml" || MISSING_FILES=$((MISSING_FILES + 1))
check_file "docker-compose.testing.yml" || MISSING_FILES=$((MISSING_FILES + 1))
check_file "backend/FinanceApp.Api/Dockerfile" || MISSING_FILES=$((MISSING_FILES + 1))
check_file "frontend/Dockerfile" || MISSING_FILES=$((MISSING_FILES + 1))
check_file "scripts/lib/testing-deploy.sh" || MISSING_FILES=$((MISSING_FILES + 1))
check_file "scripts/deploy-testing.sh" || MISSING_FILES=$((MISSING_FILES + 1))
check_file "scripts/backup-database-testing.sh" || MISSING_FILES=$((MISSING_FILES + 1))
check_file "scripts/health-check-testing.sh" || MISSING_FILES=$((MISSING_FILES + 1))
check_file "scripts/restart-testing.sh" || MISSING_FILES=$((MISSING_FILES + 1))
check_file "scripts/restore-database-testing.sh" || MISSING_FILES=$((MISSING_FILES + 1))
check_file "scripts/view-logs-testing.sh" || MISSING_FILES=$((MISSING_FILES + 1))
check_file ".env.testing.example" || MISSING_FILES=$((MISSING_FILES + 1))

if [ "${MISSING_FILES}" -gt 0 ]; then
    log_error "Some testing deployment files are missing."
    exit 1
fi

log_step "Checking shell script syntax"
for script in \
    scripts/lib/testing-deploy.sh \
    scripts/deploy-testing.sh \
    scripts/backup-database-testing.sh \
    scripts/health-check-testing.sh \
    scripts/restart-testing.sh \
    scripts/restore-database-testing.sh \
    scripts/view-logs-testing.sh \
    scripts/simulate-testing-deployment.sh; do
    if bash -n "${script}"; then
        log_info "${script} syntax is valid."
    else
        log_error "${script} has a syntax error."
        exit 1
    fi
done

log_step "Checking docker compose configuration"
GITHUB_REPO_OWNER=simulation "${DOCKER_CMD}" compose -f docker-compose.testing.yml --env-file .env.testing.example config > /dev/null
log_info "docker-compose.testing.yml is valid."

log_step "Checking backend build"
"${DOTNET_CMD}" build backend/FinanceApp.sln -c Release > /dev/null
log_info "Backend solution builds successfully."

echo
echo "========================================"
echo "  Simulation complete"
echo "========================================"
echo
log_info "The testing deployment assets passed the local validation steps."
