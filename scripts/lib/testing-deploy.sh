#!/bin/bash

RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m'

TESTING_SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
TESTING_PROJECT_ROOT="$(cd "${TESTING_SCRIPT_DIR}/../.." && pwd)"
TESTING_ENV_FILE="${TESTING_PROJECT_ROOT}/.env.testing"
TESTING_ENV_TEMPLATE="${TESTING_PROJECT_ROOT}/.env.testing.example"
TESTING_COMPOSE_FILE="${TESTING_PROJECT_ROOT}/docker-compose.testing.yml"
TESTING_API_CONTAINER="finance_api_test"
TESTING_WEB_CONTAINER="finance_web_test"
TESTING_DB_CONTAINER="finance_db_test"
TESTING_DEFAULT_WEB_PORT="8081"

log_info() {
    echo -e "${GREEN}[INFO]${NC} $1"
}

log_warn() {
    echo -e "${YELLOW}[WARN]${NC} $1"
}

log_error() {
    echo -e "${RED}[ERROR]${NC} $1"
}

load_testing_env() {
    if [ ! -f "${TESTING_ENV_FILE}" ]; then
        return 1
    fi

    set -a
    # shellcheck disable=SC1090
    . "${TESTING_ENV_FILE}"
    set +a
}

require_testing_env() {
    if load_testing_env; then
        return 0
    fi

    log_error "${TESTING_ENV_FILE} file not found."
    log_info "Copy from ${TESTING_ENV_TEMPLATE} and fill in the testing values."
    exit 1
}

require_testing_github_repo_owner() {
    if [ -n "${GITHUB_REPO_OWNER:-}" ]; then
        return 0
    fi

    log_error "GITHUB_REPO_OWNER is not set."
    log_info "Configure GITHUB_REPO_OWNER in ${TESTING_ENV_FILE}."
    exit 1
}

testing_compose() {
    GITHUB_REPO_OWNER="${GITHUB_REPO_OWNER}" docker compose -f "${TESTING_COMPOSE_FILE}" --env-file "${TESTING_ENV_FILE}" "$@"
}

testing_web_port() {
    printf '%s\n' "${WEB_PORT:-${TESTING_DEFAULT_WEB_PORT}}"
}

testing_host_ip() {
    if command -v hostname > /dev/null 2>&1; then
        local host_ip
        host_ip="$(hostname -I 2> /dev/null | awk '{print $1}')"
        if [ -n "${host_ip}" ]; then
            printf '%s\n' "${host_ip}"
            return 0
        fi
    fi

    printf '%s\n' "localhost"
}

resolve_testing_path() {
    local candidate="$1"

    case "${candidate}" in
        /*)
            printf '%s\n' "${candidate}"
            ;;
        *)
            printf '%s\n' "${TESTING_PROJECT_ROOT}/${candidate}"
            ;;
    esac
}
