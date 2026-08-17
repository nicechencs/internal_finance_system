# GitHub Actions 部署配置修复报告

## 修复日期
2026-03-18

## 修复目标
将部署方式从 Dokploy 切换到 GitHub Actions，使用预构建镜像部署到自有服务器（使用外部数据库）。

## 修改文件清单

### 1. docker-compose.yml
**修改内容**：将本地构建改为使用预构建镜像

```diff
- build:
-   context: .
-   dockerfile: backend/FinanceApp.Api/Dockerfile
+ image: ghcr.io/${GITHUB_REPO_OWNER}/finance-api:latest
```

**原因**：GitHub Actions 在 CI 阶段构建镜像并推送到 GHCR，服务器只需拉取镜像即可，无需本地构建。

### 2. .github/workflows/deploy-production.yml
**修改内容**：
1. 在部署脚本中设置 `GITHUB_REPO_OWNER` 环境变量
2. 使用环境变量替换硬编码的镜像路径
3. 在所有 `docker compose` 命令前传递环境变量

```diff
+ export GITHUB_REPO_OWNER="${{ github.repository_owner }}"
- docker pull ${{ env.IMAGE_PREFIX }}/finance-api:latest
+ docker pull ghcr.io/${GITHUB_REPO_OWNER}/finance-api:latest
- docker compose --env-file .env.production up -d
+ GITHUB_REPO_OWNER=${GITHUB_REPO_OWNER} docker compose --env-file .env.production up -d
```

**原因**：docker-compose.yml 使用 `${GITHUB_REPO_OWNER}` 变量，需要在运行时传递。

### 3. scripts/deploy.sh
**修改内容**：
1. 添加 `GITHUB_REPO_OWNER` 环境变量检查
2. 在所有 `docker compose` 命令前传递环境变量

```bash
# 确保 GITHUB_REPO_OWNER 已设置
if [ -z "$GITHUB_REPO_OWNER" ]; then
    log_error "GITHUB_REPO_OWNER 未设置！"
    exit 1
fi

GITHUB_REPO_OWNER=${GITHUB_REPO_OWNER} docker compose --env-file .env.production up -d
```

### 4. scripts/restart.sh
**修改内容**：同 deploy.sh，添加环境变量检查和传递

### 5. scripts/restore-database.sh
**修改内容**：同 deploy.sh，添加环境变量检查和传递

### 6. deploy/GITHUB_ACTIONS_SETUP.md（新增）
**内容**：完整的 GitHub Actions 部署配置指南，包括：
- GitHub Secrets 配置清单
- 服务器环境变量配置
- 首次部署步骤
- 常用操作命令
- 故障排查指南

## 配置要求

### GitHub Secrets（必需）
| Secret 名称 | 说明 |
|------------|------|
| `SSH_HOST` | 服务器地址 |
| `SSH_PORT` | SSH 端口 |
| `SSH_USER` | SSH 用户名 |
| `SSH_PRIVATE_KEY` | SSH 私钥 |
| `GHCR_USER` | GitHub 用户名 |
| `GHCR_TOKEN` | GitHub Personal Access Token（需要 read:packages 和 write:packages 权限） |

### 服务器环境变量（.env.production）
```bash
# 必需配置
GITHUB_REPO_OWNER=your-github-username  # GitHub 用户名

# 数据库配置（外部数据库）
DB_HOST=your-database-host
DB_PORT=5432
DB_NAME=finance
DB_USER=postgres
DB_PASSWORD=your-password

# 应用配置
API_PORT=5000
WEB_PORT=80
CORS_ALLOWED_ORIGIN=http://your-domain.com

# 首次部署时启用
BOOTSTRAP_ADMIN_ENABLED=true
BOOTSTRAP_ADMIN_USERNAME=admin
BOOTSTRAP_ADMIN_PASSWORD=your-admin-password
```

## 部署流程

### 自动部署（推荐）
1. 配置 GitHub Secrets
2. 在服务器创建 `.env.production` 文件
3. 推送代码到 `production` 分支
4. GitHub Actions 自动执行部署

### 手动部署
```bash
# 在服务器上
cd /opt/finance

# 拉取镜像
echo "YOUR_GHCR_TOKEN" | docker login ghcr.io -u YOUR_USERNAME --password-stdin
docker pull ghcr.io/your-github-username/finance-api:latest
docker pull ghcr.io/your-github-username/finance-web:latest

# 部署
./scripts/deploy.sh
```

## 关键改进

### 1. 统一镜像管理
- CI 阶段构建并推送镜像到 GHCR
- 服务器直接拉取预构建镜像
- 避免服务器上重复构建，节省时间和资源

### 2. 环境变量注入
- 使用 `${GITHUB_REPO_OWNER}` 变量动态指定镜像仓库
- 支持多用户/组织部署
- 避免硬编码镜像路径

### 3. 外部数据库支持
- docker-compose.yml 不包含数据库服务
- 通过环境变量配置外部数据库连接
- 支持云数据库或独立数据库服务器

### 4. 健康检查与回滚
- 部署后自动健康检查
- 检查失败自动回滚到旧版本
- 最小化服务中断时间

## 验证清单

部署前请确认：

- [ ] GitHub Secrets 已配置（6 个）
- [ ] 服务器 `.env.production` 已配置
- [ ] `GITHUB_REPO_OWNER` 已设置为你的 GitHub 用户名
- [ ] 外部数据库已创建并可访问
- [ ] 数据库 Schema 已导入
- [ ] SSH 密钥已配置并测试连接
- [ ] GHCR_TOKEN 有正确的权限

## 常见问题

### Q: 为什么需要 GHCR_TOKEN？
A: 服务器需要从 GitHub Container Registry 拉取私有镜像，`GITHUB_TOKEN` 只在 Actions 运行时有效，无法在外部服务器使用。

### Q: GITHUB_REPO_OWNER 必须配置吗？
A: 是的，docker-compose.yml 使用此变量构建镜像路径。如果不配置，docker compose 会报错找不到镜像。

### Q: 可以使用公开镜像吗？
A: 可以。如果将 GHCR 镜像设为 Public，服务器拉取时不需要登录，但仍需配置 `GITHUB_REPO_OWNER`。

### Q: 数据库必须是外部的吗？
A: 不是。如果需要在 docker-compose 中包含数据库，可以参考 `docker-compose.dev.yml` 添加 postgres 服务。

## 下一步

1. 按照 `deploy/GITHUB_ACTIONS_SETUP.md` 配置 GitHub Secrets
2. 在服务器上创建 `.env.production` 文件
3. 推送到 `production` 分支触发首次部署
4. 验证部署成功后，将 `BOOTSTRAP_ADMIN_ENABLED` 改为 `false`

## 相关文档

- [GitHub Actions 部署配置指南](./GITHUB_ACTIONS_SETUP.md)
- [GitHub Secrets 配置](./GITHUB_SECRETS.md)
- [外部数据库配置](./EXTERNAL_DATABASE.md)
- [部署修复说明](./DEPLOYMENT_FIXES.md)
