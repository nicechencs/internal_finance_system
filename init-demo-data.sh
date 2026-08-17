#!/usr/bin/env bash

set -euo pipefail

PROJECT_ROOT="$(cd "$(dirname "$0")" && pwd)"
DEMO_DATA_SCRIPT="${PROJECT_ROOT}/database/seed/seed_demo_data.sql"
CONTAINER_NAME="finance_db"
DATABASE_NAME="finance_dev"
DEFAULT_ADMIN_USERNAME="admin"
DEFAULT_ADMIN_PASSWORD="DemoOnly_ChangeMe!"

echo "========================================"
echo "初始化演示数据"
echo "========================================"
echo ""

if ! docker info > /dev/null 2>&1; then
    echo "[错误] Docker 未运行，请先启动 Docker"
    exit 1
fi

if [ ! -f "$DEMO_DATA_SCRIPT" ]; then
    echo "[错误] 未找到演示数据脚本"
    echo "路径: $DEMO_DATA_SCRIPT"
    exit 1
fi

if ! docker ps --format '{{.Names}}' | grep -qx "$CONTAINER_NAME"; then
    echo "[错误] PostgreSQL 容器未运行"
    echo "请先执行 start-dev.bat，或手动运行 docker-compose -f docker-compose.dev.yml up -d postgres"
    exit 1
fi

echo "[1/2] 检查数据库连接..."
if ! docker exec -i "$CONTAINER_NAME" psql -U postgres -d "$DATABASE_NAME" -c "SELECT 1;" > /dev/null 2>&1; then
    echo "[错误] 无法连接到数据库，请检查数据库是否正常运行"
    exit 1
fi

echo "[2/2] 导入演示数据..."
if cat "$DEMO_DATA_SCRIPT" | docker exec -i "$CONTAINER_NAME" psql -U postgres -d "$DATABASE_NAME"; then
    echo ""
    echo "========================================"
    echo "演示数据导入成功！"
    echo "========================================"
    echo ""
    echo "已创建以下演示数据："
    echo "- 账户、客户、供应商、人员等基础资料"
    echo "- 可用于本地开发和联调的样例业务数据"
    echo ""
    echo "默认账号：${DEFAULT_ADMIN_USERNAME} / ${DEFAULT_ADMIN_PASSWORD}"
    echo ""
else
    echo ""
    echo "[错误] 演示数据导入失败"
    echo "可能的原因："
    echo "1. 数据库表尚未创建"
    echo "2. 演示数据已存在"
    echo "3. SQL 脚本执行失败"
    echo ""
    echo "请查看上方的错误信息进行排查"
    echo ""
    exit 1
fi
