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

if [ "${EUID}" -ne 0 ]; then
    log_error "Run this script with sudo."
    exit 1
fi

log_info "Initializing the server for Finance System..."

log_info "Updating apt metadata..."
apt-get update

log_info "Installing base packages..."
apt-get install -y \
    ca-certificates \
    curl \
    git \
    htop \
    net-tools \
    postgresql-client \
    ufw \
    vim \
    wget

if ! command -v docker > /dev/null 2>&1; then
    log_info "Installing Docker..."
    curl -fsSL https://get.docker.com -o /tmp/get-docker.sh
    sh /tmp/get-docker.sh
    rm -f /tmp/get-docker.sh
else
    log_info "Docker is already installed: $(docker --version)"
fi

systemctl enable docker
systemctl start docker

if ! docker compose version > /dev/null 2>&1; then
    log_info "Installing Docker Compose plugin..."
    apt-get install -y docker-compose-plugin
else
    log_info "Docker Compose plugin is already available: $(docker compose version --short)"
fi

DEPLOY_DIR="/opt/finance"
log_info "Preparing deployment directories under ${DEPLOY_DIR}..."
mkdir -p "${DEPLOY_DIR}/logs/backend"
mkdir -p "${DEPLOY_DIR}/logs/frontend"
mkdir -p "${DEPLOY_DIR}/backups"
mkdir -p "${DEPLOY_DIR}/scripts"

chmod 755 "${DEPLOY_DIR}"
chmod 755 "${DEPLOY_DIR}/scripts"

log_info "Configuring firewall..."
ufw --force enable
ufw allow 22/tcp
ufw allow 80/tcp
ufw allow 443/tcp

echo
echo "========================================"
echo "  Server initialization complete"
echo "========================================"
echo
log_info "Deployment directory: ${DEPLOY_DIR}"
log_info "Next steps:"
echo "  1. Copy the project deployment files to ${DEPLOY_DIR}"
echo "  2. Create ${DEPLOY_DIR}/.env.production from .env.production.example"
echo "  3. Set a unique BOOTSTRAP_ADMIN_PASSWORD (not the published demo password)"
echo "  4. Run docker compose --env-file .env.production up -d"
echo
log_warn "Consider hardening SSH and configuring TLS before exposing the system publicly."
