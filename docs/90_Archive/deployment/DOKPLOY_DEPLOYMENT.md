# Dokploy 部署指南

## 什么是 Dokploy？

Dokploy 是一个开源的自托管 PaaS 平台（类似 Heroku/Vercel），支持：
- Docker Compose 应用部署
- 自动 SSL 证书（Let's Encrypt）
- 内置监控和日志
- Git 集成自动部署
- Web UI 管理

官网：https://dokploy.com

## 前置条件

### 1. 服务器要求
- **操作系统**：Ubuntu 20.04+ / Debian 11+ / CentOS 8+
- **配置**：最低 2 CPU / 4GB RAM / 20GB 磁盘
- **推荐**：4 CPU / 8GB RAM / 50GB 磁盘
- **端口**：80 (HTTP)、443 (HTTPS)、3000 (Dokploy UI)

### 2. 域名配置（可选但推荐）
- 主域名：`finance.example.com` → 服务器 IP
- API 子域名：`api.finance.example.com` → 服务器 IP
- Dokploy 管理：`dokploy.example.com` → 服务器 IP

---

## 第一步：安装 Dokploy

### 在服务器上执行

```bash
# 1. SSH 登录服务器
ssh user@your-server-ip

# 2. 安装 Dokploy（一键安装脚本）
curl -sSL https://dokploy.com/install.sh | sh

# 3. 等待安装完成（约 3-5 分钟）
# 安装完成后会显示访问地址和初始密码
```

安装完成后，你会看到类似输出：
```
✅ Dokploy installed successfully!
🌐 Access Dokploy at: http://your-server-ip:3000
👤 Username: admin
🔑 Password: <随机生成的密码>
```

### 首次登录

1. 访问 `http://your-server-ip:3000`
2. 使用上面的用户名和密码登录
3. **立即修改密码**（Settings → Change Password）

---

## 第二步：配置 Dokploy

### 1. 连接 GitHub 仓库

在 Dokploy Web UI 中：

1. 进入 **Settings** → **Git Providers**
2. 点击 **Add Git Provider**
3. 选择 **GitHub**
4. 填写信息：
   - **Name**: `GitHub`
   - **Access Token**: 创建 GitHub Personal Access Token
     - 访问 GitHub Settings → Developer settings → Personal access tokens
     - 生成新 token，勾选 `repo` 权限
     - 复制 token 并粘贴到 Dokploy
5. 点击 **Save**

### 2. 创建项目

1. 点击 **Create Project**
2. 填写项目信息：
   - **Project Name**: `Finance System`
   - **Description**: `财务管理系统`
3. 点击 **Create**

---

## 第三步：部署应用

### 方案 A：使用 Docker Compose（推荐）

#### 1. 创建应用

在项目中点击 **Add Application** → **Docker Compose**

填写配置：
- **Application Name**: `finance`
- **Git Repository**: 选择你的仓库
- **Branch**: `production`
- **Compose File Path**: `docker-compose.prod.yml`

#### 2. 配置环境变量

点击 **Environment Variables**，添加以下变量：

```bash
# GitHub 配置
GITHUB_REPO_OWNER=your-github-username

# 数据库密码（强密码）
DB_PASSWORD=your-strong-password-here

# 端口配置
API_PORT=5000
WEB_PORT=80

# 管理员账户（首次部署）
BOOTSTRAP_ADMIN_ENABLED=true
BOOTSTRAP_ADMIN_USERNAME=admin
BOOTSTRAP_ADMIN_PASSWORD=your-admin-password
BOOTSTRAP_ADMIN_FULL_NAME=系统管理员
BOOTSTRAP_ADMIN_EMAIL=admin@example.com

# CORS 配置
CORS_ALLOWED_ORIGIN=http://your-domain.com

# 备份配置
BACKUP_RETENTION_DAYS=7
BACKUP_DIR=./backups
```

#### 3. 配置域名（可选）

在 **Domains** 标签页：

1. 点击 **Add Domain**
2. 前端域名：
   - **Domain**: `finance.example.com`
   - **Container Port**: `80`
   - **Enable SSL**: ✅（自动申请 Let's Encrypt 证书）
3. API 域名：
   - **Domain**: `api.finance.example.com`
   - **Container Port**: `5000`
   - **Enable SSL**: ✅

#### 4. 部署

1. 点击 **Deploy** 按钮
2. Dokploy 会自动：
   - 克隆 Git 仓库
   - 拉取 Docker 镜像（从 GHCR）
   - 启动容器
   - 配置反向代理
   - 申请 SSL 证书

#### 5. 查看部署状态

- **Logs** 标签页：查看实时日志
- **Monitoring** 标签页：查看资源使用情况
- **Health Checks** 标签页：查看健康检查状态

---

### 方案 B：使用 Dockerfile（手动构建）

如果你想让 Dokploy 自己构建镜像（而不是从 GHCR 拉取）：

#### 1. 创建应用

**Add Application** → **Dockerfile**

配置：
- **Application Name**: `finance-api`
- **Git Repository**: 选择仓库
- **Branch**: `production`
- **Dockerfile Path**: `backend/FinanceApp.Api/Dockerfile`
- **Build Context**: `.`（项目根目录）

#### 2. 重复创建前端应用

- **Application Name**: `finance-web`
- **Dockerfile Path**: `frontend/Dockerfile`
- **Build Context**: `frontend`

#### 3. 创建数据库

**Add Application** → **Database** → **PostgreSQL**

配置：
- **Database Name**: `finance`
- **Username**: `postgres`
- **Password**: `your-db-password`
- **Version**: `14`

---

## 第四步：配置自动部署

### 启用 Webhook

1. 在应用设置中，找到 **Webhooks**
2. 复制 Webhook URL
3. 在 GitHub 仓库中：
   - 进入 **Settings** → **Webhooks** → **Add webhook**
   - **Payload URL**: 粘贴 Dokploy Webhook URL
   - **Content type**: `application/json`
   - **Events**: 选择 `Just the push event`
   - **Active**: ✅
4. 保存

现在，每次推送到 `production` 分支，Dokploy 会自动重新部署。

---

## 第五步：数据库初始化

### 首次部署后

1. 在 Dokploy 中打开 **Terminal**（应用详情页）
2. 选择 `finance_db` 容器
3. 执行初始化脚本：

```bash
# 方法 1：通过 Dokploy Terminal
psql -U postgres -d finance -f /docker-entrypoint-initdb.d/init.sql

# 方法 2：通过 SSH 连接服务器
docker exec -i finance_db psql -U postgres finance < /path/to/init.sql
```

### 导入演示数据（可选）

```bash
# 在服务器上
cd /path/to/dokploy/data/finance
docker exec -i finance_db psql -U postgres finance < seed_demo_data.sql
```

---

## 第六步：验证部署

### 1. 检查服务状态

在 Dokploy UI 中：
- 所有容器应显示 **Running** 状态
- Health Checks 应显示 **Healthy**

### 2. 访问应用

- **前端**: `http://your-domain.com` 或 `http://server-ip`
- **API**: `http://api.your-domain.com/swagger` 或 `http://server-ip:5000/swagger`
- **健康检查**: `http://api.your-domain.com/health`

### 3. 测试登录

使用配置的管理员账户登录：
- 用户名：`admin`（或你配置的值）
- 密码：`BOOTSTRAP_ADMIN_PASSWORD` 的值

---

## 常见问题

### Q1: 镜像拉取失败？

**问题**：`Error: failed to pull image ghcr.io/xxx/finance-api:latest`

**解决**：
1. 确保镜像是公开的，或者配置 GHCR 认证
2. 在 Dokploy 中添加 Registry Credentials：
   - **Settings** → **Registries** → **Add Registry**
   - **Registry**: `ghcr.io`
   - **Username**: 你的 GitHub 用户名
   - **Password**: GitHub Personal Access Token (需要 `read:packages` 权限)

### Q2: 数据库连接失败？

**问题**：API 日志显示 `could not connect to database`

**解决**：
1. 检查 `DB_PASSWORD` 环境变量是否正确
2. 确保数据库容器已启动：`docker ps | grep postgres`
3. 检查网络连接：`docker network inspect dokploy_network`

### Q3: 端口冲突？

**问题**：`port 80 is already in use`

**解决**：
1. 修改 `WEB_PORT` 环境变量为其他端口（如 `8080`）
2. 或停止占用端口的服务：`sudo lsof -i :80`

### Q4: SSL 证书申请失败？

**问题**：域名配置后无法访问 HTTPS

**解决**：
1. 确保域名 DNS 已正确解析到服务器 IP
2. 检查防火墙是否开放 80 和 443 端口
3. 查看 Dokploy 日志：**Logs** → **Traefik**

### Q5: 如何查看日志？

在 Dokploy UI 中：
- **Logs** 标签页 → 选择容器 → 实时查看日志
- 或在服务器上：`docker logs -f finance_api`

### Q6: 如何备份数据库？

**方法 1：通过 Dokploy Terminal**
```bash
docker exec finance_db pg_dump -U postgres finance | gzip > backup.sql.gz
```

**方法 2：使用项目脚本**
```bash
cd /path/to/dokploy/data/finance
./scripts/backup-database.sh
```

### Q7: 如何回滚到上一版本？

1. 在 Dokploy 中，进入应用详情页
2. 点击 **Deployments** 标签页
3. 找到之前的部署记录
4. 点击 **Rollback** 按钮

---

## 监控和维护

### 1. 资源监控

Dokploy 内置监控面板：
- **CPU 使用率**
- **内存使用率**
- **磁盘使用率**
- **网络流量**

### 2. 日志管理

- 日志自动轮转（配置在 `docker-compose.prod.yml` 中）
- 保留最近 3 个文件，每个最大 10MB

### 3. 备份策略

**自动备份**（推荐）：
1. 在服务器上创建 cron 任务：
```bash
crontab -e

# 每天凌晨 2 点备份数据库
0 2 * * * cd /path/to/dokploy/data/finance && ./scripts/backup-database.sh
```

**手动备份**：
```bash
./scripts/backup-database.sh
```

### 4. 更新应用

**自动更新**：推送到 `production` 分支即可

**手动更新**：
1. 在 Dokploy 中点击 **Redeploy**
2. 或使用 Webhook 触发

---

## 性能优化

### 1. 启用 HTTP/2

在 Dokploy 的 Traefik 配置中已默认启用。

### 2. 启用 Gzip 压缩

前端 Nginx 已配置 Gzip，无需额外设置。

### 3. 配置 CDN（可选）

使用 Cloudflare 等 CDN 服务：
1. 将域名 DNS 托管到 Cloudflare
2. 启用 CDN 和缓存
3. 配置 SSL/TLS 为 **Full (strict)**

### 4. 数据库优化

在 `docker-compose.prod.yml` 中调整 PostgreSQL 配置：
```yaml
environment:
  POSTGRES_SHARED_BUFFERS: 256MB
  POSTGRES_EFFECTIVE_CACHE_SIZE: 1GB
  POSTGRES_MAX_CONNECTIONS: 100
```

---

## 安全建议

### 1. 修改默认端口

将 Dokploy 管理端口从 3000 改为其他端口：
```bash
# 在服务器上
sudo nano /etc/dokploy/config.yml
# 修改 port: 3000 为其他值
sudo systemctl restart dokploy
```

### 2. 启用防火墙

```bash
sudo ufw enable
sudo ufw allow 22/tcp    # SSH
sudo ufw allow 80/tcp    # HTTP
sudo ufw allow 443/tcp   # HTTPS
sudo ufw allow 3000/tcp  # Dokploy（仅限可信 IP）
```

### 3. 定期更新

```bash
# 更新 Dokploy
curl -sSL https://dokploy.com/update.sh | sh

# 更新系统
sudo apt update && sudo apt upgrade -y
```

### 4. 启用双因素认证

在 Dokploy Settings 中启用 2FA。

---

## 与 GitHub Actions 的对比

| 特性 | GitHub Actions | Dokploy |
|------|---------------|---------|
| 部署方式 | CI/CD Pipeline | Git Push 自动部署 |
| 服务器管理 | 需要自己配置 SSH | Web UI 管理 |
| 监控 | 需要额外配置 | 内置监控面板 |
| 日志查看 | SSH 登录查看 | Web UI 实时查看 |
| SSL 证书 | 手动配置 | 自动申请和续期 |
| 回滚 | 手动执行脚本 | 一键回滚 |
| 学习曲线 | 较陡峭 | 较平缓 |

**建议**：
- 小团队或个人项目：使用 Dokploy（更简单）
- 大型团队或复杂流程：使用 GitHub Actions（更灵活）
- 也可以两者结合：GitHub Actions 构建镜像，Dokploy 部署

---

## 下一步

1. ✅ 完成 Dokploy 安装和配置
2. ✅ 部署应用并验证
3. ⏭️ 配置域名和 SSL
4. ⏭️ 设置自动备份
5. ⏭️ 配置监控告警
6. ⏭️ 编写运维文档

---

## 相关资源

- [Dokploy 官方文档](https://docs.dokploy.com)
- [Dokploy GitHub](https://github.com/Dokploy/dokploy)
- [项目部署文档](./README.md)
- [GitHub Actions 部署](./DEPLOYMENT_FIXES.md)

---

**最后更新**：2026-03-18
**维护者**：开发团队
