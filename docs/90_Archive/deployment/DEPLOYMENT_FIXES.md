# 部署系统修复说明

## 修复日期
2026-03-18

## 修复的问题

### 🔴 严重问题（已修复）

#### 1. 脚本路径不匹配
**问题**：GitHub Actions 调用 `./scripts/xxx.sh`，但脚本实际在 `deploy/scripts/` 目录下。

**修复**：
- 将 `deploy/scripts/` 移动到项目根目录 `scripts/`
- 所有部署脚本现在位于 `scripts/` 目录
- GitHub Actions workflow 使用 `scp-action` 同步脚本到服务器

#### 2. 缺少 `.env.production` 模板
**问题**：部署脚本依赖 `.env.production` 文件，但项目中没有提供模板。

**修复**：
- 创建 `.env.production.example` 模板文件
- 包含所有必需的环境变量和详细说明
- 添加到 `.gitignore` 防止敏感信息泄露

#### 3. 缺少生产环境 docker-compose 配置
**问题**：脚本引用 `docker-compose.prod.yml`，但文件在 `deploy/` 目录下。

**修复**：
- 复制 `deploy/docker-compose.prod.yml` 到项目根目录
- 服务器部署时会同步此文件

### 🟡 中等问题（已修复）

#### 4. GITHUB_TOKEN 权限问题
**问题**：使用 `GITHUB_TOKEN` 在服务器上拉取镜像，但该 token 有效期短且权限有限。

**修复**：
- 新增 `GHCR_USER` 和 `GHCR_TOKEN` secrets
- 使用 Personal Access Token (PAT) 拉取镜像
- 更新 `GITHUB_SECRETS.md` 文档

#### 5. 重复拉取镜像
**问题**：GitHub Actions 和 `deploy.sh` 都执行 `docker pull`，浪费时间和带宽。

**修复**：
- 移除 `deploy.sh` 中的 `docker pull` 步骤
- 仅在 GitHub Actions 中拉取镜像

#### 6. 健康检查失败无回滚
**问题**：健康检查失败后，新容器仍在运行，可能导致服务不可用。

**修复**：
- 在 workflow 中添加健康检查逻辑
- 检查失败时自动回滚到旧镜像
- 记录旧镜像 ID 用于回滚

#### 7. Docker Compose 命令版本
**问题**：使用旧版 `docker-compose` 命令（V1），新版 Docker 推荐使用 `docker compose`（V2）。

**修复**：
- 批量更新所有脚本中的 `docker-compose` 为 `docker compose`
- 兼容 Docker Compose V2

### 🟢 架构改进

#### 8. 分离构建和部署 Job
**改进**：
- 将 `build-and-deploy` 拆分为 `build` 和 `deploy` 两个 job
- 构建失败时不会触发部署
- 更清晰的日志输出

#### 9. 文件同步机制
**改进**：
- 使用 `scp-action` 同步部署文件到服务器
- 确保服务器上的脚本和配置始终是最新版本

#### 10. 简化健康检查
**改进**：
- 移除 `health-check.sh` 中的 `systemctl` 检查（容器环境不适用）
- 在 workflow 中实现轻量级健康检查
- 仅检查关键服务（API 可访问性）

## 新增的 GitHub Secrets

| Secret 名称 | 说明 | 必需 |
|------------|------|------|
| `GHCR_USER` | GitHub 用户名 | ✅ |
| `GHCR_TOKEN` | GitHub Personal Access Token | ✅ |
| `SSH_HOST` | 服务器地址 | ✅ |
| `SSH_PORT` | SSH 端口 | ✅ |
| `SSH_USER` | SSH 用户名 | ✅ |
| `SSH_PRIVATE_KEY` | SSH 私钥 | ✅ |

详细配置方法见 [GITHUB_SECRETS.md](./GITHUB_SECRETS.md)

## 部署流程

### 自动部署（推荐）

1. **配置 GitHub Secrets**（首次部署）
   ```bash
   # 参考 deploy/GITHUB_SECRETS.md
   ```

2. **初始化服务器**（首次部署）
   ```bash
   # 在服务器上执行
   sudo ./scripts/init-server.sh

   # 创建部署目录
   sudo mkdir -p /opt/finance
   sudo chown $USER:$USER /opt/finance
   cd /opt/finance

   # 配置环境变量
   cp .env.production.example .env.production
   nano .env.production  # 填写实际配置
   ```

3. **推送到 production 分支**
   ```bash
   git push origin production
   ```

4. **GitHub Actions 自动执行**
   - 构建 Docker 镜像
   - 推送到 GHCR
   - 同步部署文件到服务器
   - 备份数据库
   - 拉取最新镜像
   - 重启服务
   - 健康检查
   - 失败时自动回滚

### 手动部署

如果需要手动部署（不通过 GitHub Actions）：

```bash
# 1. 登录服务器
ssh user@server

# 2. 进入部署目录
cd /opt/finance

# 3. 拉取最新代码（如果使用 git）
git pull origin production

# 4. 登录 GHCR
echo "YOUR_GHCR_TOKEN" | docker login ghcr.io -u YOUR_USERNAME --password-stdin

# 5. 拉取镜像
docker pull ghcr.io/YOUR_USERNAME/finance-api:latest
docker pull ghcr.io/YOUR_USERNAME/finance-web:latest

# 6. 备份数据库
./scripts/backup-database.sh

# 7. 部署
./scripts/deploy.sh

# 8. 健康检查
./scripts/health-check.sh
```

## 回滚操作

如果部署后发现问题，可以手动回滚：

```bash
# 1. 查看可用的镜像标签
docker images | grep finance

# 2. 停止当前服务
docker compose -f docker-compose.prod.yml --env-file .env.production stop api web

# 3. 修改镜像标签（指向旧版本）
docker tag ghcr.io/YOUR_USERNAME/finance-api:OLD_SHA ghcr.io/YOUR_USERNAME/finance-api:latest
docker tag ghcr.io/YOUR_USERNAME/finance-web:OLD_SHA ghcr.io/YOUR_USERNAME/finance-web:latest

# 4. 重启服务
docker compose -f docker-compose.prod.yml --env-file .env.production up -d

# 5. 恢复数据库（如果需要）
./scripts/restore-database.sh backups/finance_TIMESTAMP.sql.gz
```

## 常见问题

### Q: 健康检查失败但服务正常？
A: 检查 `.env.production` 中的 `API_PORT` 是否正确，以及 API 是否实现了 `/health` 端点。

### Q: 镜像拉取失败？
A: 检查 `GHCR_TOKEN` 是否有效，以及是否有 `read:packages` 权限。

### Q: 脚本权限不足？
A: 在服务器上执行 `chmod +x scripts/*.sh`。

### Q: 数据库备份失败？
A: 首次部署时数据库容器不存在，备份会跳过，这是正常的。

## 后续优化建议

1. **添加测试步骤**：在构建后运行单元测试和集成测试
2. **蓝绿部署**：使用两套环境实现零停机部署
3. **监控告警**：集成 Prometheus + Grafana 监控
4. **日志聚合**：使用 ELK 或 Loki 收集日志
5. **自动化测试**：部署前自动运行 E2E 测试

## 相关文档

- [GitHub Secrets 配置](./GITHUB_SECRETS.md)
- [快速开始指南](./QUICKSTART.md)
- [部署检查清单](./CHECKLIST.md)
- [项目概览](./OVERVIEW.md)
