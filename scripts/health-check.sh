#!/bin/bash

set -euo pipefail

GREEN='\033[0;32m'
RED='\033[0;31m'
YELLOW='\033[1;33m'
NC='\033[0m'

FAILURES=0

log_info() {
    echo -e "${GREEN}[OK]${NC} $1"
}

log_error() {
    echo -e "${RED}[ERROR]${NC} $1"
    FAILURES=$((FAILURES + 1))
}

log_warn() {
    echo -e "${YELLOW}[WARN]${NC} $1"
}

# ==========================================================================
# 参数解析
# 与 deploy.sh 保持一致的调用方式：
#   --env-file <path>  传给 docker compose 的环境文件（用于 compose 变量插值）
#   -f | --file <path> 指定 compose 文件（也可用 COMPOSE_FILE 环境变量，docker 原生支持）
#   --max-wait <sec>   健康检查最长等待秒数（默认 150，覆盖 api healthcheck start_period=120s）
#   --interval <sec>   轮询间隔秒数（默认 5）
# 未显式给出 --env-file 时，若当前目录存在 .env.production 则自动采用。
# ==========================================================================
ENV_FILE=""
COMPOSE_FILE=""
MAX_WAIT=150
INTERVAL=5

while [ $# -gt 0 ]; do
    case "$1" in
        --env-file)
            ENV_FILE="$2"; shift 2 ;;
        --env-file=*)
            ENV_FILE="${1#*=}"; shift ;;
        -f|--file)
            COMPOSE_FILE="$2"; shift 2 ;;
        -f=*|--file=*)
            COMPOSE_FILE="${1#*=}"; shift ;;
        --max-wait)
            MAX_WAIT="$2"; shift 2 ;;
        --interval)
            INTERVAL="$2"; shift 2 ;;
        *)
            log_warn "Ignoring unknown argument: $1"; shift ;;
    esac
done

# 默认沿用 .env.production（与 deploy.sh 的调用上下文一致）
if [ -z "$ENV_FILE" ] && [ -f .env.production ]; then
    ENV_FILE=".env.production"
fi

# 载入 env 文件，供后续 WEB_PORT 等变量的展示性回退使用
if [ -n "$ENV_FILE" ] && [ -f "$ENV_FILE" ]; then
    set -a
    # shellcheck disable=SC1090
    . "$ENV_FILE"
    set +a
fi

# 构造统一的 docker compose 基础命令
COMPOSE_ARGS=()
if [ -n "$COMPOSE_FILE" ]; then
    COMPOSE_ARGS+=(-f "$COMPOSE_FILE")
fi
if [ -n "$ENV_FILE" ] && [ -f "$ENV_FILE" ]; then
    COMPOSE_ARGS+=(--env-file "$ENV_FILE")
fi

compose() {
    docker compose "${COMPOSE_ARGS[@]}" "$@"
}

service_exists() {
    compose config --services 2>/dev/null | grep -qx "$1"
}

# 轮询等待某个 compose 服务变为 healthy（若定义了 healthcheck）或 running（未定义时）
# 参数：<service> <label>
wait_for_service() {
    local svc="$1"
    local label="$2"
    local elapsed=0
    local cid="" state="" health=""

    while [ "$elapsed" -le "$MAX_WAIT" ]; do
        cid="$(compose ps -q "$svc" 2>/dev/null | head -n1 || true)"
        if [ -n "$cid" ]; then
            state="$(docker inspect --format '{{.State.Status}}' "$cid" 2>/dev/null || echo unknown)"
            health="$(docker inspect --format '{{if .State.Health}}{{.State.Health.Status}}{{else}}none{{end}}' "$cid" 2>/dev/null || echo none)"

            if [ "$health" = "healthy" ]; then
                log_info "${label} service is healthy (compose healthcheck passed)."
                return 0
            elif [ "$health" = "none" ] && [ "$state" = "running" ]; then
                log_info "${label} service is running (no healthcheck defined)."
                return 0
            elif [ "$state" = "exited" ] || [ "$state" = "dead" ]; then
                log_error "${label} service is ${state}."
                return 1
            fi
        fi
        sleep "$INTERVAL"
        elapsed=$((elapsed + INTERVAL))
    done

    if [ -z "$cid" ]; then
        log_error "${label} service container was not created within ${MAX_WAIT}s."
    else
        log_error "${label} service did not become healthy within ${MAX_WAIT}s (state=${state:-?}, health=${health:-?})."
    fi
    return 1
}

echo "========================================"
echo "  Finance System Health Check"
echo "========================================"
echo
if [ ${#COMPOSE_ARGS[@]} -gt 0 ]; then
    echo "Compose invocation: docker compose ${COMPOSE_ARGS[*]}"
    echo
fi

echo "1. Checking Docker..."
if command -v systemctl > /dev/null 2>&1; then
    if systemctl is-active --quiet docker; then
        log_info "Docker service is running."
    else
        log_error "Docker service is not running."
    fi
elif docker info > /dev/null 2>&1; then
    log_info "Docker daemon is reachable."
else
    log_error "Docker daemon is not reachable."
fi

echo
echo "2. Waiting for core services (api, web) to become healthy..."
echo "   (polling every ${INTERVAL}s, up to ${MAX_WAIT}s to cover api start_period)"
wait_for_service "api" "API" || true
wait_for_service "web" "Web" || true

echo
echo "3. Checking database service..."
# 生产库为外部数据库（compose 中无 db 服务）；仅 dev/testing compose 才带内置数据库。
DB_SERVICE=""
for candidate in postgres db database; do
    if service_exists "$candidate"; then
        DB_SERVICE="$candidate"
        break
    fi
done
if [ -n "$DB_SERVICE" ]; then
    wait_for_service "$DB_SERVICE" "Database (${DB_SERVICE})" || true
else
    log_info "No database service in compose; assuming external DB (covered by API /health)."
fi

echo
echo "4. Checking API health (via compose healthcheck result)..."
# 优先信任 compose 定义的 healthcheck（api 镜像内含 curl，命令为 curl -fsS http://localhost:8080/health）。
API_CID="$(compose ps -q api 2>/dev/null || true)"
if [ -n "$API_CID" ]; then
    API_HEALTH="$(docker inspect --format '{{if .State.Health}}{{.State.Health.Status}}{{else}}none{{end}}' "$API_CID" 2>/dev/null || echo none)"
    if [ "$API_HEALTH" = "healthy" ]; then
        log_info "API /health reported healthy by the container healthcheck."
    elif [ "$API_HEALTH" = "none" ]; then
        log_warn "API container has no healthcheck defined; relying on service running state."
    else
        log_error "API healthcheck status is '${API_HEALTH}'."
    fi
else
    log_error "API container not found; cannot read healthcheck status."
fi

echo
echo "5. Checking public web proxy exposure..."
# 生产 compose 的 web 仅 expose:80，无宿主端口映射；dev/testing 才有 ports 映射。
WEB_PUBLISHED="$(compose port web 80 2>/dev/null || true)"
if [ -n "$WEB_PUBLISHED" ]; then
    WEB_HOST_PORT="${WEB_PUBLISHED##*:}"
    log_info "web has a published host port: ${WEB_PUBLISHED}."

    if command -v curl > /dev/null 2>&1; then
        echo "   - Checking frontend root..."
        if curl -fsS "http://localhost:${WEB_HOST_PORT}/" > /dev/null 2>&1; then
            log_info "Frontend is reachable on localhost:${WEB_HOST_PORT}."
        else
            log_error "Frontend is not reachable on localhost:${WEB_HOST_PORT}."
        fi

        echo "   - Checking proxy auth route (expect 401 for anonymous)..."
        AUTH_ME_STATUS="$(curl -sS -o /dev/null -w '%{http_code}' "http://localhost:${WEB_HOST_PORT}/api/auth/me" || true)"
        if [ "$AUTH_ME_STATUS" = "401" ]; then
            log_info "Proxy route /api/auth/me returns the expected 401."
        else
            log_error "Unexpected status ${AUTH_ME_STATUS} from /api/auth/me via localhost:${WEB_HOST_PORT}."
        fi
    else
        log_warn "curl is not installed on host; skipping external HTTP checks."
    fi
else
    log_info "web has no published host port (expose-only). Skipping external port probe; traffic flows via the internal network / reverse proxy (Traefik)."
fi

echo
echo "6. Checking disk usage..."
DISK_USAGE=$(df -h / | awk 'NR==2 {print $5}' | sed 's/%//')
if [ "$DISK_USAGE" -lt 80 ]; then
    log_info "Disk usage is ${DISK_USAGE}%."
elif [ "$DISK_USAGE" -lt 90 ]; then
    log_warn "Disk usage is high at ${DISK_USAGE}%."
else
    log_warn "Disk usage is critically high at ${DISK_USAGE}%."
fi

echo
echo "7. Checking memory usage..."
if command -v free > /dev/null 2>&1; then
    MEM_USAGE=$(free | awk 'NR==2 {printf "%.0f", $3/$2 * 100}')
    if [ "$MEM_USAGE" -lt 80 ]; then
        log_info "Memory usage is ${MEM_USAGE}%."
    elif [ "$MEM_USAGE" -lt 90 ]; then
        log_warn "Memory usage is high at ${MEM_USAGE}%."
    else
        log_warn "Memory usage is critically high at ${MEM_USAGE}%."
    fi
else
    log_warn "'free' not available; skipping memory usage check."
fi

echo
echo "8. Recent error logs..."
echo
echo "--- API ---"
compose logs --tail 20 api 2>&1 | grep -i "error\|exception\|fail" || log_info "No recent API error logs."
echo
echo "--- Web ---"
compose logs --tail 20 web 2>&1 | grep -i "error\|exception\|fail" || log_info "No recent web error logs."

echo
echo "========================================"
if [ "$FAILURES" -eq 0 ]; then
    echo "  Health check passed"
    echo "========================================"
    exit 0
fi

echo "  Health check failed (${FAILURES} issue(s))"
echo "========================================"
exit 1
