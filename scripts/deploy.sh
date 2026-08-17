#!/bin/bash

set -euo pipefail

RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m'

DEPLOY_PATH="$(cd "$(dirname "$0")/.." && pwd)"
REPO_PATH="${DEPLOY_PATH}/source"
TARGET_BRANCH="${DEPLOY_BRANCH:-${GIT_BRANCH:-production}}"
IMAGE_OWNER="${GITHUB_REPO_OWNER:-your-github-username}"

log_info()  { echo -e "${GREEN}[INFO]${NC} $1"; }
log_warn()  { echo -e "${YELLOW}[WARN]${NC} $1"; }
log_error() { echo -e "${RED}[ERROR]${NC} $1"; }

# ========== 前置检查 ==========
if [ ! -f "${DEPLOY_PATH}/.env.production" ]; then
    log_error ".env.production not found at ${DEPLOY_PATH}"
    log_info "Copy from .env.production.example and fill in the production values."
    exit 1
fi

if [ ! -d "${REPO_PATH}/.git" ]; then
    log_error "${REPO_PATH} is missing or is not a Git repository."
    log_info "Clone the production branch into ${REPO_PATH} first."
    exit 1
fi

cd "${DEPLOY_PATH}"

set -a
. ./.env.production
set +a

export GITHUB_REPO_OWNER="${IMAGE_OWNER}"

log_info "Deploying Finance System..."
log_info "Image owner: ${IMAGE_OWNER}"
log_info "Target branch: ${TARGET_BRANCH}"

# ========== 拉取最新代码 ==========
cd "${REPO_PATH}"
log_info "Syncing source repository..."
git fetch origin
git checkout "${TARGET_BRANCH}"
git reset --hard "origin/${TARGET_BRANCH}"

SHA_SHORT="$(git rev-parse --short HEAD)"
API_IMAGE="ghcr.io/${IMAGE_OWNER}/finance-api"
WEB_IMAGE="ghcr.io/${IMAGE_OWNER}/finance-web"
log_info "Current commit: ${SHA_SHORT}"

# ========== 备份部署文件 ==========
log_info "Backing up current deploy files..."
mkdir -p "${DEPLOY_PATH}/.rollback/scripts"

if [ -f "${DEPLOY_PATH}/docker-compose.yml" ]; then
    cp "${DEPLOY_PATH}/docker-compose.yml" "${DEPLOY_PATH}/.rollback/docker-compose.yml"
fi

for file in deploy.sh backup-database.sh health-check.sh restart.sh restore-database.sh view-logs.sh; do
    if [ -f "${DEPLOY_PATH}/scripts/$file" ]; then
        cp "${DEPLOY_PATH}/scripts/$file" "${DEPLOY_PATH}/.rollback/scripts/$file"
    fi
done

# ========== 同步部署文件 ==========
log_info "Syncing deploy files from source..."
cp "${REPO_PATH}/docker-compose.yml" "${DEPLOY_PATH}/"

mkdir -p "${DEPLOY_PATH}/scripts"
for file in deploy.sh backup-database.sh health-check.sh restart.sh restore-database.sh view-logs.sh; do
    if [ -f "${REPO_PATH}/scripts/$file" ]; then
        cp "${REPO_PATH}/scripts/$file" "${DEPLOY_PATH}/scripts/$file"
    fi
done
chmod +x "${DEPLOY_PATH}"/scripts/*.sh 2>/dev/null || true

# ========== 验证 compose 配置 ==========
cd "${DEPLOY_PATH}"
log_info "Validating docker compose configuration..."
docker compose --env-file .env.production config > /dev/null

# ========== 构建镜像 ==========
cd "${REPO_PATH}"

log_info "Building backend image..."
docker build \
    -t "${API_IMAGE}:${SHA_SHORT}" \
    -t "${API_IMAGE}:latest" \
    -f backend/FinanceApp.Api/Dockerfile .

log_info "Building frontend image..."
docker build \
    -t "${WEB_IMAGE}:${SHA_SHORT}" \
    -t "${WEB_IMAGE}:latest" \
    --build-arg VITE_API_BASE_URL=/api \
    -f frontend/Dockerfile frontend/

# ========== 数据库备份 ==========
cd "${DEPLOY_PATH}"

if [ -n "${DB_HOST:-}" ] && [ -n "${DB_NAME:-}" ]; then
    ./scripts/backup-database.sh || log_warn "Database backup failed, continuing deployment"
else
    log_info "Database config incomplete, skipping backup"
fi

# ========== 记录旧镜像用于回滚 ==========
API_CONTAINER="$(docker compose --env-file .env.production ps -q api 2>/dev/null)" || API_CONTAINER=""
WEB_CONTAINER="$(docker compose --env-file .env.production ps -q web 2>/dev/null)" || WEB_CONTAINER=""
OLD_API_IMAGE="none"
OLD_WEB_IMAGE="none"
if [ -n "$API_CONTAINER" ]; then
    OLD_API_IMAGE="$(docker inspect --format='{{.Image}}' "$API_CONTAINER" 2>/dev/null)" || OLD_API_IMAGE="none"
fi
if [ -n "$WEB_CONTAINER" ]; then
    OLD_WEB_IMAGE="$(docker inspect --format='{{.Image}}' "$WEB_CONTAINER" 2>/dev/null)" || OLD_WEB_IMAGE="none"
fi

# ========== 部署 ==========
export API_IMAGE_TAG="${SHA_SHORT}"
export WEB_IMAGE_TAG="${SHA_SHORT}"

log_info "Stopping old containers..."
docker compose --env-file .env.production stop api web || true
docker compose --env-file .env.production rm -f api web || true

log_info "Starting new containers..."
docker compose --env-file .env.production up -d

# ========== 健康检查 ==========
# health-check.sh 内部已带轮询（覆盖 api healthcheck 的 start_period=120s），
# 无需再固定 sleep 等待；仅保留极短缓冲让 compose 完成容器创建。
log_info "Running health check (built-in polling)..."
sleep 3

HEALTH_OK=true

if [ -x ./scripts/health-check.sh ]; then
    if ! ./scripts/health-check.sh --env-file .env.production --max-wait 210; then
        HEALTH_OK=false
    fi
else
    log_warn "health-check.sh not found, skipping"
fi

if [ "$HEALTH_OK" = "false" ]; then
    log_error "Health check failed, rolling back..."

    # 恢复部署文件
    if [ -f .rollback/docker-compose.yml ]; then
        cp .rollback/docker-compose.yml docker-compose.yml
    fi
    for file in deploy.sh backup-database.sh health-check.sh restart.sh restore-database.sh view-logs.sh; do
        if [ -f ".rollback/scripts/$file" ]; then
            cp ".rollback/scripts/$file" "scripts/$file"
        fi
    done
    chmod +x scripts/*.sh 2>/dev/null || true

    # 回滚容器
    docker compose --env-file .env.production stop api web || true
    docker compose --env-file .env.production rm -f api web || true

    if [ "$OLD_API_IMAGE" != "none" ] || [ "$OLD_WEB_IMAGE" != "none" ]; then
        if [ "$OLD_API_IMAGE" != "none" ]; then
            docker tag "$OLD_API_IMAGE" "${API_IMAGE}:rollback" || true
        fi
        if [ "$OLD_WEB_IMAGE" != "none" ]; then
            docker tag "$OLD_WEB_IMAGE" "${WEB_IMAGE}:rollback" || true
        fi
        export API_IMAGE_TAG="rollback"
        export WEB_IMAGE_TAG="rollback"
        docker compose --env-file .env.production up -d
        log_warn "Rolled back to previous images"
    else
        log_error "No previous images to rollback, manual intervention required"
    fi

    exit 1
fi

# ========== 完成 ==========
HOST_IP=$(hostname -I | awk '{print $1}')
WEB_PORT_VALUE=${WEB_PORT:-8080}

log_info "Deployment succeeded (commit: ${SHA_SHORT})"
log_info "Frontend: http://${HOST_IP}:${WEB_PORT_VALUE}"
log_info "API: http://${HOST_IP}:${WEB_PORT_VALUE}/api"

docker compose --env-file .env.production ps

# 清理旧镜像
docker image prune -f --filter "until=72h" || true
