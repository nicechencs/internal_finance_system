# 快速开始指南

本指南帮助你在 10 分钟内完成生产环境部署配置。

## 前置条件

- ✅ 拥有一台 Ubuntu 服务器（已安装 Docker）
- ✅ 拥有服务器 SSH 访问权限
- ✅ 拥有 GitHub 仓库管理权限

## 部署流程（3 步）

### 第 1 步：服务器准备（5 分钟）

SSH 登录到服务器，执行以下命令：

```bash
# 1. 创建部署目录
sudo mkdir -p /opt/finance
cd /opt/finance

# 2. 下载配置文件
sudo wget https://raw.githubusercontent.com/your-github-username/finance_system/production/deploy/docker-compose.prod.yml
sudo wget https://raw.githubusercontent.com/your-github-username/finance_system/production/deploy/.env.production.example

# 3. 创建脚本目录并下载脚本
sudo mkdir -p scripts
cd scripts
sudo wget https://raw.githubusercontent.com/your-github-username/finance_system/production/deploy/scripts/deploy.sh
sudo wget https://raw.githubusercontent.com/your-github-username/finance_system/production/deploy/scripts/backup-database.sh
sudo wget https://raw.githubusercontent.com/your-github-username/finance_system/production/deploy/scripts/restore-database.sh
sudo wget https://raw.githubusercontent.com/your-github-username/finance_system/production/deploy/scripts/health-check.sh
sudo wget https://raw.githubusercontent.com/your-github-username/finance_system/production/deploy/scripts/view-logs.sh
sudo wget https://raw.githubusercontent.com/your-github-username/finance_system/production/deploy/scripts/restart.sh
sudo chmod +x *.sh
cd ..

# 4. 创建日志和备份目录
sudo mkdir -p logs/backend logs/frontend backups

# 5. 配置环境变量
sudo cp .env.production.example .env.production
sudo vim .env.production  # 编辑配置（见下方说明）
sudo chmod 600 .env.production

# 6. 生成 SSH 密钥（用于 GitHub Actions）
ssh-keygen -t ed25519 -C "github-actions" -f ~/.ssh/github_actions_key -N ""
cat ~/.ssh/github_actions_key.pub >> ~/.ssh/authorized_keys
chmod 600 ~/.ssh/authorized_keys

# 7. 显示私钥（复制用于 GitHub Secrets）
cat ~/.ssh/github_actions_key
```

**编辑 `.env.production` 时需要修改的关键配置：**

```bash
GITHUB_REPO_OWNER=your-github-username                    # 你的 GitHub 用户名
DB_PASSWORD=your_secure_password               # 数据库密码（强密码）
BOOTSTRAP_ADMIN_PASSWORD=your_admin_password   # 管理员密码
CORS_ALLOWED_ORIGIN=http://your-domain.com     # 你的域名或 IP
```

### 第 2 步：GitHub 配置（3 分钟）

#### 2.1 创建 production 分支

在本地仓库执行：

```bash
git checkout -b production
git push -u origin production
```

#### 2.2 配置 GitHub Secrets

访问：`https://github.com/your-github-username/your-repo/settings/secrets/actions`

点击 `New repository secret`，添加以下 4 个 Secrets：

| Secret 名称 | 值 |
|------------|-----|
| `SSH_HOST` | 服务器 IP 地址（如 `192.168.1.100`） |
| `SSH_PORT` | SSH 端口（通常是 `22`） |
| `SSH_USER` | SSH 用户名（如 `ubuntu`） |
| `SSH_PRIVATE_KEY` | 第 1 步生成的私钥（完整内容） |

> 数据库密码等运行时配置统一在服务器 `.env.production` 文件中管理，无需配置为 GitHub Secrets。

**提示**：详细配置说明见 [GITHUB_SECRETS.md](./GITHUB_SECRETS.md)

### 第 3 步：触发部署（2 分钟）

在本地仓库执行：

```bash
# 确保在 production 分支
git checkout production

# 合并 main 分支的代码
git merge main

# 推送到远程（自动触发部署）
git push origin production
```

然后：
1. 访问 `https://github.com/your-github-username/your-repo/actions`
2. 查看 `Deploy to Production` 工作流运行状态
3. 等待部署完成（约 5-10 分钟）

## 验证部署

部署完成后，在服务器上执行：

```bash
cd /opt/finance

# 运行健康检查
./scripts/health-check.sh

# 查看容器状态
docker ps

# 查看日志
./scripts/view-logs.sh all
```

访问服务：
- **前端**: `http://YOUR_SERVER_IP`
- **API**: `http://YOUR_SERVER_IP:5000`
- **Swagger**: `http://YOUR_SERVER_IP:5000/swagger`

默认登录账户：
- 用户名：`admin`
- 密码：`.env.production` 中配置的 `BOOTSTRAP_ADMIN_PASSWORD`

## 常用命令

```bash
# 查看日志
./scripts/view-logs.sh api        # API 日志
./scripts/view-logs.sh web        # 前端日志
./scripts/view-logs.sh all        # 所有日志

# 重启服务
./scripts/restart.sh api          # 重启 API
./scripts/restart.sh all          # 重启所有服务

# 备份数据库
./scripts/backup-database.sh

# 健康检查
./scripts/health-check.sh
```

## 后续更新

每次更新代码时，只需：

```bash
git checkout production
git merge main
git push origin production
```

GitHub Actions 会自动完成构建和部署。

## 故障排查

### 部署失败

1. 检查 GitHub Actions 日志
2. 确认 GitHub Secrets 配置正确
3. 测试 SSH 连接：`ssh -p 22 ubuntu@YOUR_SERVER_IP`

### 服务无法访问

```bash
# 检查容器状态
docker ps -a

# 查看错误日志
./scripts/view-logs.sh all

# 检查防火墙
sudo ufw status
```

### 数据库连接失败

```bash
# 检查数据库容器
docker exec finance_db pg_isready -U postgres

# 查看数据库日志
docker logs finance_db
```

## 获取帮助

- 📖 完整文档：[deploy/README.md](./README.md)
- 🔐 Secrets 配置：[deploy/GITHUB_SECRETS.md](./GITHUB_SECRETS.md)
- 🐛 问题反馈：[GitHub Issues](https://github.com/your-github-username/your-repo/issues)

---

**祝部署顺利！** 🚀
