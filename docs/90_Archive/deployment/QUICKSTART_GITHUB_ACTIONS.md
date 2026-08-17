# GitHub Actions 部署快速参考

## 🎯 核心修改

### 1. docker-compose.yml
```yaml
# 从本地构建改为使用预构建镜像
image: ghcr.io/${GITHUB_REPO_OWNER}/finance-api:latest
```

### 2. 必需的环境变量
```bash
# 在 .env.production 中配置
GITHUB_REPO_OWNER=your-github-username  # 你的 GitHub 用户名
```

### 3. 必需的 GitHub Secrets
- `SSH_HOST` - 服务器地址
- `SSH_PORT` - SSH 端口
- `SSH_USER` - SSH 用户名
- `SSH_PRIVATE_KEY` - SSH 私钥
- `GHCR_USER` - GitHub 用户名
- `GHCR_TOKEN` - GitHub PAT（需要 read:packages 和 write:packages 权限）

## 🚀 快速开始

### 步骤 1：配置 GitHub Secrets
在 GitHub 仓库 Settings → Secrets and variables → Actions 中添加上述 6 个 Secrets。

### 步骤 2：配置服务器
```bash
# 登录服务器
ssh user@your-server

# 创建部署目录
sudo mkdir -p /opt/finance
sudo chown $USER:$USER /opt/finance
cd /opt/finance

# 创建环境变量文件
cat > .env.production << 'EOF'
GITHUB_REPO_OWNER=your-github-username
DB_HOST=your-database-host
DB_PORT=5432
DB_NAME=finance
DB_USER=postgres
DB_PASSWORD=your-password
API_PORT=5000
WEB_PORT=80
CORS_ALLOWED_ORIGIN=http://your-domain.com
BOOTSTRAP_ADMIN_ENABLED=true
BOOTSTRAP_ADMIN_USERNAME=admin
BOOTSTRAP_ADMIN_PASSWORD=your-admin-password
BOOTSTRAP_ADMIN_FULL_NAME=系统管理员
BOOTSTRAP_ADMIN_EMAIL=admin@example.com
EOF

chmod 600 .env.production
```

### 步骤 3：准备数据库
```bash
# 在数据库中执行
psql -h your-db-host -U postgres -c "CREATE DATABASE finance;"
psql -h your-db-host -U postgres -d finance -f docs/02_Database/01_database_schema.sql
```

### 步骤 4：触发部署
```bash
# 在本地推送到 production 分支
git push origin production
```

## 📋 验证清单

部署前确认：
- [ ] GitHub Secrets 已配置（6 个）
- [ ] 服务器 `.env.production` 已创建
- [ ] `GITHUB_REPO_OWNER` 已设置
- [ ] 外部数据库已创建
- [ ] 数据库 Schema 已导入
- [ ] SSH 连接测试成功

## 🔧 常用命令

```bash
# 查看容器状态
docker ps

# 查看日志
docker logs -f finance_api
docker logs -f finance_web

# 重启服务
cd /opt/finance
./scripts/restart.sh

# 备份数据库
./scripts/backup-database.sh

# 手动拉取镜像
echo "YOUR_TOKEN" | docker login ghcr.io -u YOUR_USERNAME --password-stdin
docker pull ghcr.io/your-github-username/finance-api:latest
docker pull ghcr.io/your-github-username/finance-web:latest
```

## ⚠️ 重要提示

1. **GITHUB_REPO_OWNER 必须配置**：否则 docker compose 无法找到镜像
2. **GHCR_TOKEN 需要正确权限**：read:packages 和 write:packages
3. **首次部署后**：将 `BOOTSTRAP_ADMIN_ENABLED` 改为 `false`
4. **CORS 配置**：生产环境改为实际域名

## 📚 详细文档

- [完整配置指南](./GITHUB_ACTIONS_SETUP.md)
- [修复说明](./GITHUB_ACTIONS_FIXES.md)
- [GitHub Secrets 配置](./GITHUB_SECRETS.md)
