# 生产环境部署指南

## 目录
- [概述](#概述)
- [前置要求](#前置要求)
- [服务器准备](#服务器准备)
- [GitHub 配置](#github-配置)
- [首次部署](#首次部署)
- [日常运维](#日常运维)
- [故障排查](#故障排查)

---

## 概述

本项目使用 GitHub Actions + Docker 实现自动化部署。当代码推送到 `production` 分支时，会自动触发以下流程：

1. 构建 Docker 镜像（前端 + 后端）
2. 推送镜像到 GitHub Container Registry
3. SSH 连接到服务器
4. 拉取最新镜像并重启服务

---

## 前置要求

### 服务器要求
- **操作系统**: Ubuntu 20.04+ / Debian 11+
- **内存**: 至少 2GB RAM
- **磁盘**: 至少 20GB 可用空间
- **软件**: Docker 20.10+, Docker Compose 2.0+

### 本地要求
- Git
- SSH 客户端
- 服务器 SSH 访问权限

---

## 服务器准备

### 1. 初始化服务器环境

在服务器上执行：

```bash
# 下载初始化脚本
wget https://raw.githubusercontent.com/your-github-username/finance_system/production/deploy/scripts/init-server.sh

# 赋予执行权限
chmod +x init-server.sh

# 执行初始化（需要 sudo 权限）
sudo ./init-server.sh
```

此脚本会自动：
- 安装 Docker 和 Docker Compose
- 创建部署目录 `/opt/finance`
- 配置防火墙规则
- 设置必要的权限

### 2. 创建部署目录结构

```bash
cd /opt/finance

# 创建必要的目录
mkdir -p logs/backend logs/frontend backups scripts

# 下载部署脚本
cd scripts
wget https://raw.githubusercontent.com/your-github-username/finance_system/production/deploy/scripts/deploy.sh
wget https://raw.githubusercontent.com/your-github-username/finance_system/production/deploy/scripts/backup-database.sh
wget https://raw.githubusercontent.com/your-github-username/finance_system/production/deploy/scripts/restore-database.sh
wget https://raw.githubusercontent.com/your-github-username/finance_system/production/deploy/scripts/health-check.sh
wget https://raw.githubusercontent.com/your-github-username/finance_system/production/deploy/scripts/view-logs.sh
wget https://raw.githubusercontent.com/your-github-username/finance_system/production/deploy/scripts/restart.sh

# 赋予执行权限
chmod +x *.sh

cd ..
```

### 3. 下载配置文件

```bash
# 下载 docker-compose 配置
wget https://raw.githubusercontent.com/your-github-username/finance_system/production/deploy/docker-compose.prod.yml

# 下载环境变量模板
wget https://raw.githubusercontent.com/your-github-username/finance_system/production/deploy/.env.production.example

# 复制并编辑环境变量
cp .env.production.example .env.production
vim .env.production
```

### 4. 配置环境变量

编辑 `.env.production` 文件，填写以下关键配置：

```bash
# GitHub 仓库所有者（你的 GitHub 用户名）
GITHUB_REPO_OWNER=your-github-username

# 数据库密码（强密码）
DB_PASSWORD=your_secure_password_here

# 初始管理员账户（首次部署时启用）
BOOTSTRAP_ADMIN_ENABLED=true
BOOTSTRAP_ADMIN_USERNAME=admin
BOOTSTRAP_ADMIN_PASSWORD=your_admin_password_here
BOOTSTRAP_ADMIN_EMAIL=admin@example.com

# CORS 配置（你的域名）
CORS_ALLOWED_ORIGIN=http://your-domain.com

# 端口配置
API_PORT=5000
WEB_PORT=80
```

**安全提示**：
```bash
# 设置文件权限（仅所有者可读写）
chmod 600 .env.production
```

### 5. 生成 SSH 密钥（如果还没有）

在服务器上：

```bash
# 生成 SSH 密钥对
ssh-keygen -t ed25519 -C "github-actions-deploy"

# 将公钥添加到 authorized_keys
cat ~/.ssh/id_ed25519.pub >> ~/.ssh/authorized_keys

# 显示私钥（用于配置 GitHub Secrets）
cat ~/.ssh/id_ed25519
```

**重要**：复制私钥内容（包括 `-----BEGIN` 和 `-----END` 行），稍后配置 GitHub Secrets 时使用。

---

## GitHub 配置

### 1. 创建 production 分支

在本地仓库：

```bash
# 基于 main 分支创建 production 分支
git checkout -b production

# 推送到远程
git push -u origin production
```

### 2. 配置 GitHub Secrets

进入 GitHub 仓库页面：`Settings` → `Secrets and variables` → `Actions` → `New repository secret`

添加以下 Secrets：

| Secret 名称 | 说明 | 示例值 |
|------------|------|--------|
| `SSH_HOST` | 服务器 IP 地址 | `192.168.1.100` |
| `SSH_PORT` | SSH 端口 | `22` |
| `SSH_USER` | SSH 用户名 | `ubuntu` |
| `SSH_PRIVATE_KEY` | SSH 私钥（完整内容） | `-----BEGIN OPENSSH PRIVATE KEY-----...` |

> 数据库密码等运行时配置统一在服务器 `.env.production` 文件中管理，无需配置为 GitHub Secrets。

### 3. 配置分支保护规则（可选）

`Settings` → `Branches` → `Add branch protection rule`

- Branch name pattern: `production`
- 勾选 `Require a pull request before merging`
- 勾选 `Require status checks to pass before merging`

---

## 首次部署

### 方式一：通过 GitHub Actions 自动部署（推荐）

```bash
# 确保在 production 分支
git checkout production

# 合并 main 分支的最新代码
git merge main

# 推送到远程（触发自动部署）
git push origin production
```

然后：
1. 访问 GitHub 仓库的 `Actions` 标签页
2. 查看 `Deploy to Production` 工作流运行状态
3. 等待部署完成（约 5-10 分钟）

### 方式二：手动部署

在服务器上：

```bash
cd /opt/finance

# 登录 GitHub Container Registry
echo "YOUR_GITHUB_TOKEN" | docker login ghcr.io -u YOUR_GITHUB_USERNAME --password-stdin

# 拉取最新镜像
docker pull ghcr.io/your-github-username/finance-api:latest
docker pull ghcr.io/your-github-username/finance-web:latest

# 启动服务
docker-compose -f docker-compose.prod.yml --env-file .env.production up -d

# 查看容器状态
docker-compose -f docker-compose.prod.yml ps
```

### 验证部署

```bash
# 运行健康检查
./scripts/health-check.sh

# 查看日志
./scripts/view-logs.sh all
```

访问服务：
- 前端: `http://YOUR_SERVER_IP`
- API: `http://YOUR_SERVER_IP:5000`
- Swagger: `http://YOUR_SERVER_IP:5000/swagger`

---

## 日常运维

### 查看日志

```bash
# 查看所有服务日志
./scripts/view-logs.sh all

# 查看 API 日志（最后 50 行）
./scripts/view-logs.sh api 50

# 查看前端日志（实时跟踪）
./scripts/view-logs.sh web
```

### 重启服务

```bash
# 重启所有服务
./scripts/restart.sh all

# 仅重启 API
./scripts/restart.sh api

# 仅重启前端
./scripts/restart.sh web
```

### 数据库备份

```bash
# 手动备份
./scripts/backup-database.sh

# 设置定时备份（每天凌晨 2 点）
crontab -e
# 添加以下行：
0 2 * * * cd /opt/finance && ./scripts/backup-database.sh >> logs/backup.log 2>&1
```

### 数据库恢复

```bash
# 列出可用备份
ls -lh backups/

# 恢复指定备份
./scripts/restore-database.sh backups/finance_20260317_120000.sql.gz
```

### 更新部署

推送代码到 `production` 分支即可自动触发部署：

```bash
git checkout production
git merge main
git push origin production
```

---

## 故障排查

### 容器无法启动

```bash
# 查看容器状态
docker ps -a

# 查看容器日志
docker logs finance_api
docker logs finance_web
docker logs finance_db

# 检查配置文件
cat .env.production
```

### 数据库连接失败

```bash
# 检查数据库容器状态
docker exec finance_db pg_isready -U postgres

# 进入数据库容器
docker exec -it finance_db psql -U postgres

# 检查数据库是否存在
\l

# 检查连接字符串
grep ConnectionStrings .env.production
```

### API 无响应

```bash
# 检查 API 容器日志
docker logs --tail 100 finance_api

# 检查端口占用
netstat -tlnp | grep 5000

# 重启 API 服务
./scripts/restart.sh api
```

### 前端无法访问

```bash
# 检查 Nginx 配置
docker exec finance_web cat /etc/nginx/conf.d/default.conf

# 检查前端日志
docker logs finance_web

# 测试 API 连接
curl http://localhost:5000/health
```

### 磁盘空间不足

```bash
# 检查磁盘使用
df -h

# 清理 Docker 资源
docker system prune -a --volumes

# 清理旧备份（保留最近 7 天）
find backups/ -name "*.sql.gz" -mtime +7 -delete

# 清理日志
find logs/ -name "*.log" -mtime +30 -delete
```

### GitHub Actions 部署失败

1. 检查 GitHub Secrets 是否配置正确
2. 检查服务器 SSH 连接是否正常
3. 查看 Actions 日志中的错误信息
4. 确认服务器磁盘空间充足

---

## 安全建议

### 1. 修改 SSH 端口

```bash
sudo vim /etc/ssh/sshd_config
# 修改 Port 22 为其他端口（如 2222）
sudo systemctl restart sshd

# 更新防火墙规则
sudo ufw allow 2222/tcp
sudo ufw delete allow 22/tcp
```

### 2. 禁用密码登录

```bash
sudo vim /etc/ssh/sshd_config
# 设置以下选项：
# PasswordAuthentication no
# PubkeyAuthentication yes
sudo systemctl restart sshd
```

### 3. 配置 SSL 证书

使用 Let's Encrypt 免费证书：

```bash
# 安装 Certbot
sudo apt-get install certbot

# 获取证书
sudo certbot certonly --standalone -d your-domain.com

# 配置 Nginx（需要修改 frontend/nginx.conf）
```

### 4. 定期更新系统

```bash
# 更新系统包
sudo apt-get update && sudo apt-get upgrade -y

# 更新 Docker 镜像
docker pull ghcr.io/your-github-username/finance-api:latest
docker pull ghcr.io/your-github-username/finance-web:latest
```

---

## 监控与告警

### 设置资源监控

```bash
# 安装 htop
sudo apt-get install htop

# 查看系统资源
htop

# 查看 Docker 容器资源使用
docker stats
```

### 日志监控

```bash
# 监控错误日志
tail -f logs/backend/*.log | grep -i error
tail -f logs/frontend/error.log
```

---

## 联系支持

如遇到问题，请：
1. 查看本文档的故障排查部分
2. 检查 GitHub Actions 日志
3. 查看服务器日志文件
4. 提交 GitHub Issue

---

**最后更新**: 2026-03-17
