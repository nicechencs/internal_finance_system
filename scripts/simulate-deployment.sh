#!/bin/bash

set -euo pipefail

RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m'

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
echo "  Deployment Simulation"
echo "========================================"
echo
log_warn "This script validates the deployment assets locally without touching the server."
echo

check_command() {
    local command_name="$1"
    if command -v "$command_name" > /dev/null 2>&1; then
        log_info "$command_name is available."
        return 0
    fi

    log_error "$command_name is missing."
    return 1
}

check_file() {
    local path="$1"
    if [ -f "$path" ]; then
        log_info "$path exists."
        return 0
    fi

    log_error "$path is missing."
    return 1
}

MISSING_TOOLS=0
log_step "Checking required tools"
check_command git || MISSING_TOOLS=$((MISSING_TOOLS + 1))
check_command bash || MISSING_TOOLS=$((MISSING_TOOLS + 1))
check_command dotnet || MISSING_TOOLS=$((MISSING_TOOLS + 1))
check_command docker || MISSING_TOOLS=$((MISSING_TOOLS + 1))

if [ "$MISSING_TOOLS" -gt 0 ]; then
    log_error "Install the missing tools and rerun the simulation."
    exit 1
fi

MISSING_FILES=0
log_step "Checking required files"
check_file ".github/workflows/release-production.yml" || MISSING_FILES=$((MISSING_FILES + 1))
check_file "docker-compose.yml" || MISSING_FILES=$((MISSING_FILES + 1))
check_file "backend/FinanceApp.Api/Dockerfile" || MISSING_FILES=$((MISSING_FILES + 1))
check_file "frontend/Dockerfile" || MISSING_FILES=$((MISSING_FILES + 1))
check_file "scripts/deploy.sh" || MISSING_FILES=$((MISSING_FILES + 1))
check_file "scripts/backup-database.sh" || MISSING_FILES=$((MISSING_FILES + 1))
check_file "scripts/health-check.sh" || MISSING_FILES=$((MISSING_FILES + 1))
check_file "scripts/restart.sh" || MISSING_FILES=$((MISSING_FILES + 1))
check_file ".env.production.example" || MISSING_FILES=$((MISSING_FILES + 1))

if [ "$MISSING_FILES" -gt 0 ]; then
    log_error "Some deployment files are missing."
    exit 1
fi

log_step "Checking shell script syntax"
for script in scripts/deploy.sh scripts/backup-database.sh scripts/health-check.sh scripts/restart.sh scripts/restore-database.sh scripts/view-logs.sh; do
    if bash -n "$script"; then
        log_info "$script syntax is valid."
    else
        log_error "$script has a syntax error."
        exit 1
    fi
done

log_step "Checking docker compose configuration"
GITHUB_REPO_OWNER=simulation docker compose config > /dev/null
log_info "docker-compose.yml is valid."

log_step "Checking backend build"
dotnet build backend/FinanceApp.sln -c Release > /dev/null
log_info "Backend solution builds successfully."

echo
echo "========================================"
echo "  Simulation complete"
echo "========================================"
echo
log_info "The deployment assets passed the local validation steps."
