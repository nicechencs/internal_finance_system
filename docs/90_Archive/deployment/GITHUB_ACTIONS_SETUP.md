# GitHub Actions 部署配置指南

## 📋 配置清单

### 1. GitHub Secrets 配置

在 GitHub 仓库中配置以下 Secrets（Settings → Secrets and variables → Actions）：

| Secret 名称 | 说明 | 示例值 |
|------------|------|--------|
| `SSH_HOST` | 服务器 IP 或域名 | `192.168.1.100` |
| `SSH_PORT` | SSH 端口 | `22` |
| `SSH_USER` | SSH 用户名 | `ubuntu` |
| `SSH_PRIVATE_KEY` | SSH 私钥（完整内容） | `-----BEGIN OPENSSH PRIVATE KEY-----...` |
| `GHCR_USER` | GitHub 用户名 | `your-github-username` |
| `GHCR_TOKEN` | GitHub Personal Access Token | `ghp_xxxxxxxxxxxx` |

### 2. 服务器环境变量配置

在服务器的 `/opt/finance/.env.production` 文件中配置：

```bash
# ============================================
# GitHub 配置
# ============================================
GITHUB_REPO_OWNER=your-github-username  # 你的 GitHub 用户名

# ============================================
# 数据库配置（外部数据库）
# ============================================
DB_HOST=your-database-host  # 数据库地址
DB_PORT=5432
DB_NAME=finance
DB_USER=postgres
DB_PASSWORD=your-strong-password

# ============================================
# 应用端口配置
# ============================================
API_PORT=5000
WEB_PORT=80

# ============================================
# 管理员账户配置（首次部署）
# ============================================
BOOTSTRAP_ADMIN_ENABLED=true
BOOTSTRAP_ADMIN_USERNAME=admin
BOOTSTRAP_ADMIN_PASSWORD=your-admin-password
BOOTSTRAP_ADMIN_FULL_NAME=系统管理员
BOOTSTRAP_ADMIN_EMAIL=admin@example.com

# ============================================
# CORS 配置
# ============================================
CORS_ALLOWED_ORIGIN=http://your-domain.com
```

## 🚀 部署流程

### 首次部署

#### 步骤 1：配置 GitHub Secrets

参考上面的表格，在 GitHub 仓库中配置所有必需的 Secrets。

**获取 GHCR_TOKEN**：
1. 访问 GitHub Settings → Developer settings → Personal access tokens → Tokens (classic)
2. 点击 "Generate new token (classic)"
3. 勾选权限：`read:packages` 和 `write:packages`
4. 生成并复制 Token

**获取 SSH 私钥**：
```bash
# 在服务器上生成密钥对
ssh-keygen -t ed25519 -C "github-actions" -f ~/.ssh/github_actions_key -N ""

# 将公钥添加到 authorized_keys
cat ~/.ssh/github_actions_key.pub >> ~/.ssh/authorized_keys
chmod 600 ~/.ssh/authorized_keys

# 显示私钥（复制到 GitHub Secrets）
cat ~/.ssh/github_actions_key
```

#### 步骤 2：初始化服务器

```bash
# 登录服务器
ssh user@your-server

# 创建部署目录
sudo mkdir -p /opt/finance
sudo chown $USER:$USER /opt/finance
cd /opt/finance

# 创建日志目录
mkdir -p logs/backend logs/frontend

# 配置环境变量
cp .env.production.example .env.production
nano .env.production  # 填写实际配置
```

#### 步骤 3：准备数据库

确保外部数据库已创建并可访问：

```sql
-- 在数据库中执行
CREATE DATABASE finance;
```

然后导入数据库 Schema：

```bash
# 方法 1：使用 psql
psql -h your-db-host -U postgres -d finance -f docs/02_Database/01_database_schema.sql

# 方法 2：如果需要演示数据
psql -h your-db-host -U postgres -d finance -f docs/02_Database/seed_demo_data.sql
```

#### 步骤 4：触发部署

```bash
# 在本地推送到 production 分支
git push origin production
```

GitHub Actions 将自动执行：
1. 构建 Docker 镜像
2. 推送到 GHCR
3. 同步文件到服务器
4. 拉取最新镜像
5. 启动容器
6. 健康检查
7. 失败时自动回滚

### 后续部署

每次推送到 `production` 分支都会自动触发部署：

```bash
git checkout production
git merge main  # 或其他分支
git push origin production
```

## 🔍 验证部署

### 1. 查看 GitHub Actions 日志

访问 GitHub 仓库的 Actions 标签页，查看部署日志。

### 2. 检查服务器状态

```bash
# 登录服务器
ssh user@your-server
cd /opt/finance

# 查看容器状态
docker ps

# 查看日志
docker logs finance_api
docker logs finance_web

# 或使用脚本
./scripts/view-logs.sh
```

### 3. 访问应用

- 前端：`http://your-server-ip`
- API：`http://your-server-ip:5000`
- Swagger：`http://your-server-ip:5000/swagger`

## 🛠️ 常用操作

### 手动触发部署

在 GitHub Actions 页面，选择 "Deploy to Production" workflow，点击 "Run workflow"。

### 查看日志

```bash
# 实时查看所有日志
./scripts/view-logs.sh

# 查看特定服务日志
docker logs -f finance_api
docker logs -f finance_web
```

### 重启服务

```bash
# 重启所有服务
./scripts/restart.sh

# 重启特定服务
./scripts/restart.sh api
./scripts/restart.sh web
```

### 备份数据库

```bash
./scripts/backup-database.sh
```

### 恢复数据库

```bash
./scripts/restore-database.sh backups/finance_20260318_120000.sql.gz
```

## ⚠️ 注意事项

### 1. GITHUB_REPO_OWNER 必须配置

在 `.env.production` 中必须设置 `GITHUB_REPO_OWNER`，否则无法拉取镜像。

### 2. 数据库连接

确保服务器能够访问外部数据库：
- 检查防火墙规则
- 检查数据库白名单
- 使用 `host.docker.internal` 访问宿主机上的数据库

### 3. 首次部署后

首次部署成功后，将 `.env.production` 中的 `BOOTSTRAP_ADMIN_ENABLED` 改为 `false`，避免重复创建管理员账户。

### 4. CORS 配置

生产环境务必修改 `CORS_ALLOWED_ORIGIN` 为实际域名，不要使用 `http://localhost`。

### 5. 镜像权限

如果镜像是私有的，确保：
- `GHCR_TOKEN` 有 `read:packages` 权限
- Token 未过期

## 🐛 故障排查

### 部署失败

1. 查看 GitHub Actions 日志，定位失败步骤
2. 检查 SSH 连接是否正常
3. 检查服务器磁盘空间

### 健康检查失败

1. 检查 API 是否正常启动：`docker logs finance_api`
2. 检查数据库连接是否正常
3. 检查端口是否被占用

### 镜像拉取失败

1. 检查 `GHCR_TOKEN` 是否有效
2. 检查 `GITHUB_REPO_OWNER` 是否正确
3. 检查镜像是否存在：访问 `https://github.com/your-github-username?tab=packages`

### 容器无法启动

1. 检查环境变量配置
2. 检查数据库连接
3. 查看容器日志：`docker logs finance_api`

## 📚 相关文档

- [GitHub Secrets 配置详解](./GITHUB_SECRETS.md)
- [部署修复说明](./DEPLOYMENT_FIXES.md)
- [外部数据库配置](./EXTERNAL_DATABASE.md)
- [部署方式对比](./DEPLOYMENT_COMPARISON.md)
