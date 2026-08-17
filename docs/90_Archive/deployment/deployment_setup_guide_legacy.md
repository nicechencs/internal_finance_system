# 部署设置指南 — 生产环境 & 测试环境

本文档说明如何通过 GitHub Actions 自动部署财务管理系统到生产/测试服务器。

---

## 目录

1. [架构概览](#1-架构概览)
2. [前置条件](#2-前置条件)
3. [GitHub 仓库配置](#3-github-仓库配置)
4. [服务器配置 — 生产环境](#4-服务器配置--生产环境)
5. [服务器配置 — 测试环境](#5-服务器配置--测试环境)
6. [触发部署](#6-触发部署)
7. [部署流程详解](#7-部署流程详解)
8. [部署后验证](#8-部署后验证)
9. [运维命令速查](#9-运维命令速查)
10. [常见问题排查](#10-常见问题排查)

---

## 1. 架构概览

```
开发者推送代码
    │
    ▼
GitHub Actions（构建 Docker 镜像 → 推送到 GHCR）
    │
    ▼
SSH 连接目标服务器
    │
    ▼
拉取镜像 → 停旧容器 → 启新容器 → 健康检查
    │
    ▼
失败自动回滚 / 成功完成
```

**关键组件：**

| 组件 | 说明 |
|------|------|
| GHCR | GitHub Container Registry，存储 Docker 镜像 |
| docker-compose.yml | 生产环境容器编排 |
| docker-compose.testing.yml | 测试环境容器编排 |
| .env.production | 生产环境变量（服务器上手动创建） |
| .env.testing | 测试环境变量（服务器上手动创建） |

**两套环境对比：**

| 项目 | 生产环境 | 测试环境 |
|------|----------|----------|
| 触发分支 | `production` | `test` |
| GitHub Environment | `production`（可选） | `testing` |
| 服务器部署路径 | `/opt/finance/` | `/opt/finance-test/` |
| 默认 Web 端口 | `8080` | `8081` |
| 镜像名后缀 | `finance-api` / `web` | `finance-api-test` / `web-test` |
| 容器名 | `finance_api` / `web` | `finance_api_test` / `web_test` |
| 并发策略 | 禁止并行（排队等待） | 允许取消旧的（加快迭代） |
| Docker 网络 | `finance_network` | `finance_network_test` |

---

## 2. 前置条件

### 2.1 服务器要求

- Linux 服务器（推荐 Ubuntu 20.04+）
- Docker Engine 20.10+
- Docker Compose V2（`docker compose` 命令，非 `docker-compose`）
- curl（用于健康检查）
- 至少 2GB 可用内存（生产环境），1GB（测试环境）
- 可访问 `ghcr.io`（GitHub Container Registry）

### 2.2 数据库要求

- PostgreSQL 14+
- 数据库可以部署在同一服务器，也可以使用远程数据库服务
- 生产和测试必须使用**不同的数据库**

### 2.3 GitHub 要求

- GitHub 仓库已有 `production` 和 `test` 分支
- 生成一个 Personal Access Token（PAT），用于服务器从 GHCR 拉取镜像

### 2.4 SSH 密钥对

在本地生成 SSH 密钥对（用于 GitHub Actions 连接服务器）：

```bash
ssh-keygen -t ed25519 -C "github-actions-deploy" -f deploy_key
```

- `deploy_key`（私钥）→ 配置到 GitHub Secrets
- `deploy_key.pub`（公钥）→ 添加到服务器的 `~/.ssh/authorized_keys`

---

## 3. GitHub 仓库配置

### 3.1 生产环境 Secrets

在 **Settings → Secrets and variables → Actions → Repository secrets** 中添加：

| Secret 名称 | 说明 | 示例值 |
|---|---|---|
| `SSH_HOST` | 生产服务器 IP 地址或域名 | `203.0.113.10` |
| `SSH_PORT` | SSH 端口 | `22` |
| `SSH_USER` | SSH 登录用户名 | `root` 或 `deploy` |
| `SSH_PRIVATE_KEY` | SSH 私钥（完整内容，包含 BEGIN/END 行） | `-----BEGIN OPENSSH PRIVATE KEY-----...` |
| `GHCR_TOKEN` | GitHub PAT（需 `read:packages` 权限） | `ghp_xxxxxxxxxxxx` |
| `GHCR_USER` | GitHub 用户名 | `your-github-username` |

### 3.2 测试环境 Secrets

先创建 **GitHub Environment**：**Settings → Environments → New environment** → 命名为 `testing`。

在 `testing` 环境下添加 **Environment secrets**：

| Secret 名称 | 说明 | 示例值 |
|---|---|---|
| `TEST_SSH_HOST` | 测试服务器 IP 地址或域名 | `192.168.1.100` |
| `TEST_SSH_PORT` | SSH 端口 | `22` |
| `TEST_SSH_USER` | SSH 登录用户名 | `root` 或 `deploy` |
| `TEST_SSH_PRIVATE_KEY` | SSH 私钥（完整内容） | `-----BEGIN OPENSSH PRIVATE KEY-----...` |
| `GHCR_TOKEN` | GitHub PAT（需 `read:packages` 权限） | `ghp_xxxxxxxxxxxx` |
| `GHCR_USER` | GitHub 用户名 | `your-github-username` |

> **提示：** 如果生产和测试部署在同一台服务器上，`TEST_SSH_HOST` 和 `SSH_HOST` 可以填相同的 IP，只要端口（`WEB_PORT`）和部署路径不冲突即可。

### 3.3 生成 GitHub PAT（GHCR_TOKEN）

1. 访问 **GitHub → Settings → Developer settings → Personal access tokens → Tokens (classic)**
2. 点击 **Generate new token (classic)**
3. 勾选权限：`read:packages`
4. 生成后复制 Token，分别填入生产和测试环境的 `GHCR_TOKEN`

> **注意：** `GITHUB_TOKEN`（构建阶段推送镜像用的）是 GitHub Actions 自动提供的，无需手动配置。`GHCR_TOKEN` 是给服务器上 `docker login` 拉取镜像用的，需要手动创建。

### 3.4 配置总览

```
GitHub Repository
├── Secrets (Repository level) ── 用于生产部署
│   ├── SSH_HOST          ← 生产服务器 IP
│   ├── SSH_PORT          ← 22
│   ├── SSH_USER          ← 登录用户
│   ├── SSH_PRIVATE_KEY   ← 私钥
│   ├── GHCR_TOKEN        ← PAT
│   └── GHCR_USER         ← GitHub 用户名
│
└── Environments
    └── testing ── 用于测试部署
        ├── TEST_SSH_HOST          ← 测试服务器 IP
        ├── TEST_SSH_PORT          ← 22
        ├── TEST_SSH_USER          ← 登录用户
        ├── TEST_SSH_PRIVATE_KEY   ← 私钥
        ├── GHCR_TOKEN             ← PAT
        └── GHCR_USER              ← GitHub 用户名
```

---

## 4. 服务器配置 — 生产环境

### 4.1 安装 Docker

```bash
# Ubuntu
curl -fsSL https://get.docker.com | sh
sudo usermod -aG docker $USER
# 重新登录以生效
```

### 4.2 创建部署目录

```bash
sudo mkdir -p /opt/finance/{scripts,logs/backend,logs/frontend,backups}
sudo chown -R $(whoami):$(whoami) /opt/finance
```

### 4.3 配置 SSH 公钥

```bash
mkdir -p ~/.ssh && chmod 700 ~/.ssh
# 将 deploy_key.pub 的内容追加到 authorized_keys
echo "你的公钥内容" >> ~/.ssh/authorized_keys
chmod 600 ~/.ssh/authorized_keys
```

### 4.4 创建环境变量文件

```bash
cat > /opt/finance/.env.production << 'EOF'
# ============================================
# GitHub 配置
# ============================================
GITHUB_REPO_OWNER=your-github-username

# ============================================
# 数据库配置
# ============================================
# 数据库在本机：host.docker.internal
# 数据库在远程：填写远程 IP 或域名
DB_HOST=host.docker.internal
DB_PORT=5432
DB_NAME=finance
DB_USER=postgres
DB_PASSWORD=你的强密码

# ============================================
# 备份配置
# ============================================
BACKUP_RETENTION_DAYS=7
BACKUP_DIR=./backups

# ============================================
# 应用配置
# ============================================
WEB_PORT=8080
AUTH_COOKIE_SECURE_POLICY=SameAsRequest

# ============================================
# 管理员账户（首次部署设 true，之后改为 false）
# ============================================
BOOTSTRAP_ADMIN_ENABLED=true
BOOTSTRAP_ADMIN_USERNAME=admin
BOOTSTRAP_ADMIN_PASSWORD=你的管理员密码
BOOTSTRAP_ADMIN_FULL_NAME=系统管理员
BOOTSTRAP_ADMIN_EMAIL=admin@example.com

# ============================================
# CORS（填写实际访问地址）
# ============================================
CORS_ALLOWED_ORIGIN=http://你的域名或IP:8080
EOF

# 保护文件权限
chmod 600 /opt/finance/.env.production
```

### 4.5 环境变量说明

| 变量名 | 必填 | 默认值 | 说明 |
|--------|------|--------|------|
| `GITHUB_REPO_OWNER` | 是 | — | GitHub 用户名，用于拼接 GHCR 镜像地址 |
| `DB_HOST` | 是 | `host.docker.internal` | PostgreSQL 主机地址 |
| `DB_PORT` | 否 | `5432` | PostgreSQL 端口 |
| `DB_NAME` | 是 | `finance` | 数据库名称 |
| `DB_USER` | 是 | `postgres` | 数据库用户名 |
| `DB_PASSWORD` | 是 | — | 数据库密码 |
| `WEB_PORT` | 否 | `8080` | 前端服务端口 |
| `AUTH_COOKIE_SECURE_POLICY` | 否 | `SameAsRequest` | Cookie 安全策略（HTTPS 上线后改为 `Always`） |
| `BOOTSTRAP_ADMIN_ENABLED` | 否 | `false` | 是否创建初始管理员 |
| `BOOTSTRAP_ADMIN_USERNAME` | 否 | `admin` | 管理员用户名 |
| `BOOTSTRAP_ADMIN_PASSWORD` | 否 | — | 管理员密码 |
| `BOOTSTRAP_ADMIN_FULL_NAME` | 否 | `System Administrator` | 管理员显示名 |
| `BOOTSTRAP_ADMIN_EMAIL` | 否 | — | 管理员邮箱 |
| `CORS_ALLOWED_ORIGIN` | 是 | `http://localhost` | 允许的前端来源地址 |
| `BACKUP_RETENTION_DAYS` | 否 | `7` | 备份保留天数 |
| `BACKUP_DIR` | 否 | `./backups` | 备份目录 |

---

## 5. 服务器配置 — 测试环境

### 5.1 创建部署目录

```bash
sudo mkdir -p /opt/finance-test/{scripts/lib,logs/backend-testing,logs/frontend-testing,backups/testing}
sudo chown -R $(whoami):$(whoami) /opt/finance-test
```

### 5.2 配置 SSH 公钥

如果测试服务器与生产是同一台，此步骤已完成。否则同 [4.3](#43-配置-ssh-公钥) 操作。

### 5.3 创建环境变量文件

```bash
cat > /opt/finance-test/.env.testing << 'EOF'
# ============================================
# GitHub 配置
# ============================================
GITHUB_REPO_OWNER=your-github-username

# ============================================
# 数据库配置（必须与生产使用不同的数据库！）
# ============================================
DB_HOST=host.docker.internal
DB_PORT=5432
DB_NAME=finance_test
DB_USER=postgres
DB_PASSWORD=你的测试数据库密码

# ============================================
# 备份配置
# ============================================
BACKUP_RETENTION_DAYS=7
BACKUP_DIR=backups/testing

# ============================================
# 应用配置
# ============================================
WEB_PORT=8081
AUTH_COOKIE_SECURE_POLICY=SameAsRequest

# ============================================
# 管理员账户（首次部署设 true，之后改为 false）
# ============================================
BOOTSTRAP_ADMIN_ENABLED=true
BOOTSTRAP_ADMIN_USERNAME=admin
BOOTSTRAP_ADMIN_PASSWORD=你的测试管理员密码
BOOTSTRAP_ADMIN_FULL_NAME=Testing Administrator
BOOTSTRAP_ADMIN_EMAIL=admin@example.com

# ============================================
# CORS（填写测试环境实际访问地址）
# ============================================
CORS_ALLOWED_ORIGIN=http://你的服务器IP:8081
EOF

chmod 600 /opt/finance-test/.env.testing
```

### 5.4 测试环境特有说明

- 数据库名称用 `finance_test`，与生产的 `finance` 隔离
- 默认端口 `8081`，避免与生产 `80` 冲突
- 容器名和 Docker 网络均独立，不会影响生产环境
- `.env.testing` 必须在首次部署**前**手动创建，否则工作流会报错退出

---

## 6. 触发部署

### 6.1 自动触发

| 环境 | 触发条件 |
|------|----------|
| 生产 | 推送代码到 `production` 分支 |
| 测试 | 推送代码到 `test` 分支 |

```bash
# 部署到测试环境
git push origin test

# 部署到生产环境（通常通过 PR 合并到 production 分支）
git push origin production
```

### 6.2 手动触发

1. 进入 GitHub 仓库 → **Actions** 标签页
2. 选择 **Deploy to Production** 或 **Deploy to Testing**
3. 点击 **Run workflow** → 选择分支 → 点击运行

---

## 7. 部署流程详解

以下是 GitHub Actions 的完整部署步骤：

### 7.1 Build 阶段（GitHub 云端执行）

```
1. 检出代码
2. 登录 GHCR（使用 GITHUB_TOKEN）
3. 构建后端 Docker 镜像（多阶段构建：SDK → Runtime）
4. 构建前端 Docker 镜像（多阶段构建：Node → Nginx）
5. 推送镜像到 GHCR，标签为 commit SHA 和 latest
```

### 7.2 Deploy 阶段（目标服务器执行）

```
1. SSH 连接服务器
2. 备份当前 docker-compose.yml 和脚本到 .rollback/
3. SCP 上传新的 docker-compose.yml 和脚本
4. 读取 .env 文件
5. 备份数据库（失败不阻断部署）
6. docker login 到 GHCR
7. 拉取新镜像
8. 记录旧容器的镜像 ID（用于回滚）
9. 停止并删除旧的 api/web 容器
10. 启动新容器
11. 等待健康检查通过（API: 240s, Web: 180s）
12. 运行 health-check 脚本
13. 验证 /api/auth/me 返回 401
14. 任一步骤失败 → 自动回滚到旧镜像
```

### 7.3 回滚机制

部署失败时自动执行：
- 恢复 `.rollback/` 中的旧 docker-compose.yml 和脚本
- 将旧镜像打上 `rollback` 标签
- 使用旧镜像重启容器

> **注意：** 数据库迁移（EF Core Migrations）在 API 容器启动时自动执行。如果新版本迁移了数据库但容器启动失败，回滚后旧版本可能无法兼容新的数据库 Schema，此时需要手动恢复数据库备份。

---

## 8. 部署后验证

### 8.1 检查容器状态

```bash
# 生产环境
cd /opt/finance
docker compose --env-file .env.production ps

# 测试环境
cd /opt/finance-test
docker compose -f docker-compose.testing.yml --env-file .env.testing ps
```

### 8.2 测试 API

```bash
# 生产（端口 8080）
curl -s http://localhost:8080/api/health
curl -s -o /dev/null -w "%{http_code}" http://localhost:8080/api/auth/me
# 预期返回 401

# 测试（端口 8081）
curl -s http://localhost:8081/api/health
curl -s -o /dev/null -w "%{http_code}" http://localhost:8081/api/auth/me
# 预期返回 401
```

### 8.3 查看日志

```bash
# 生产
docker logs -f finance_api --tail 100
docker logs -f finance_web --tail 100

# 测试
docker logs -f finance_api_test --tail 100
docker logs -f finance_web_test --tail 100
```

### 8.4 运行健康检查脚本

```bash
# 生产
cd /opt/finance && ./scripts/health-check.sh

# 测试
cd /opt/finance-test && ./scripts/health-check-testing.sh
```

---

## 9. 运维命令速查

### 9.1 生产环境（/opt/finance）

```bash
# 查看容器状态
GITHUB_REPO_OWNER=your-username docker compose --env-file .env.production ps

# 查看日志
./scripts/view-logs.sh

# 重启服务
./scripts/restart.sh

# 备份数据库
./scripts/backup-database.sh

# 恢复数据库
./scripts/restore-database.sh backups/finance_XXXXXXXX_XXXXXX.sql.gz

# 手动部署（不通过 CI）
./scripts/deploy.sh
```

### 9.2 测试环境（/opt/finance-test）

```bash
# 查看容器状态
GITHUB_REPO_OWNER=your-username docker compose -f docker-compose.testing.yml --env-file .env.testing ps

# 查看日志
./scripts/view-logs-testing.sh

# 重启服务
./scripts/restart-testing.sh

# 备份数据库
./scripts/backup-database-testing.sh

# 恢复数据库
./scripts/restore-database-testing.sh backups/testing/finance_test_XXXXXXXX_XXXXXX.sql.gz
```

---

## 10. 常见问题排查

### Q: 部署失败提示 `.env.testing not found`

**原因：** 测试服务器上 `/opt/finance-test/.env.testing` 文件不存在。

**解决：** SSH 登录服务器，按照 [第 5.3 节](#53-创建环境变量文件) 创建该文件。

---

### Q: 容器健康检查超时

**可能原因：**
- 数据库连接失败（检查 `DB_HOST` / `DB_PASSWORD`）
- 端口被占用（检查 `WEB_PORT` 是否冲突）
- 服务器内存不足

**排查：**
```bash
docker logs finance_api --tail 50
docker inspect finance_api --format='{{.State.Health}}'
```

---

### Q: `docker login` 到 GHCR 失败

**原因：** `GHCR_TOKEN` 无效或权限不足。

**解决：** 重新生成 GitHub PAT，确保勾选 `read:packages` 权限。

---

### Q: 镜像拉取失败（403 Forbidden）

**可能原因：**
- 仓库是 Private 的，PAT 需要额外的 `repo` 权限
- `GHCR_USER` 填写错误

**解决：** 确认 PAT 权限，确认镜像地址拼写正确。

---

### Q: 回滚后旧容器也无法启动

**原因：** 新版本已执行了数据库迁移，旧版本代码不兼容新 Schema。

**解决：**
```bash
# 恢复数据库到部署前的备份
./scripts/restore-database.sh backups/最新的备份文件.sql.gz
# 然后重启
./scripts/restart.sh
```

---

### Q: CORS 报错（前端无法调用 API）

**原因：** `.env` 中 `CORS_ALLOWED_ORIGIN` 与实际访问地址不匹配。

**解决：** 确保值与浏览器地址栏的协议 + 域名 + 端口完全一致，例如 `http://finance.example.com`（不带尾部斜杠）。

---

### Q: 生产和测试能部署在同一台服务器吗？

**可以。** 两套环境使用不同的：
- 部署路径（`/opt/finance/` vs `/opt/finance-test/`）
- 容器名（`finance_api` vs `finance_api_test`）
- Docker 网络（`finance_network` vs `finance_network_test`）
- 端口（`8080` vs `8081`）
- 数据库（`finance` vs `finance_test`）

只需确保 GitHub Secrets 中 `SSH_HOST` 和 `TEST_SSH_HOST` 填相同 IP 即可。

---

### Q: 首次部署后需要做什么？

1. 登录系统验证管理员账户可用
2. 将 `.env` 中 `BOOTSTRAP_ADMIN_ENABLED` 改为 `false`（防止密码被重置）
3. 在系统内创建其他用户账户
