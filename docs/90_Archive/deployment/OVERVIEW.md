# CI/CD 自动部署配置总览

## 📁 文件清单

### GitHub Actions 工作流
```
.github/workflows/
└── deploy-production.yml          # 自动部署工作流（推送到 production 分支时触发）
```

### 部署配置文件
```
deploy/
├── docker-compose.prod.yml        # 生产环境 Docker Compose 配置
├── .env.production.example        # 环境变量模板
├── README.md                      # 完整部署文档
├── QUICKSTART.md                  # 快速开始指南
├── GITHUB_SECRETS.md              # GitHub Secrets 配置指南
├── CHECKLIST.md                   # 部署检查清单
└── scripts/                       # 运维脚本
    ├── deploy.sh                  # 部署脚本
    ├── backup-database.sh         # 数据库备份脚本
    ├── restore-database.sh        # 数据库恢复脚本
    ├── health-check.sh            # 健康检查脚本
    ├── view-logs.sh               # 日志查看脚本
    ├── restart.sh                 # 服务重启脚本
    ├── init-server.sh             # 服务器初始化脚本
    └── simulate-deployment.sh     # 部署模拟演练脚本
```

### 更新的现有文件
```
README.md                          # 添加了生产环境部署章节
```

## 📊 统计信息

| 类型 | 数量 | 说明 |
|------|------|------|
| 新增文件 | 14 | 包括工作流、配置、脚本、文档 |
| 修改文件 | 1 | README.md |
| 脚本文件 | 8 | 全部可执行的 Shell 脚本 |
| 文档文件 | 4 | Markdown 格式的指南文档 |
| 配置文件 | 2 | Docker Compose 和环境变量 |
| 总代码行数 | ~1200 | 包括脚本、配置、文档 |

## 🔄 部署流程

### 自动部署流程（推荐）

```mermaid
graph LR
    A[推送到 production 分支] --> B[GitHub Actions 触发]
    B --> C[构建 Docker 镜像]
    C --> D[推送到 GHCR]
    D --> E[SSH 连接服务器]
    E --> F[备份数据库]
    F --> G[拉取最新镜像]
    G --> H[重启服务]
    H --> I[健康检查]
    I --> J[部署完成]
```

### 手动部署流程（备用）

```bash
# 在服务器上执行
cd /opt/finance
./scripts/backup-database.sh
docker-compose -f docker-compose.prod.yml pull
./scripts/deploy.sh
./scripts/health-check.sh
```

## 🎯 核心特性

### 1. 模块化设计
- ✅ 所有脚本独立可执行
- ✅ 环境变量集中管理
- ✅ 配置与代码分离

### 2. 安全性
- ✅ 敏感信息使用 GitHub Secrets
- ✅ SSH 密钥认证
- ✅ 环境变量文件权限控制
- ✅ 自动备份数据库

### 3. 可维护性
- ✅ 完整的文档体系
- ✅ 详细的日志输出
- ✅ 健康检查机制
- ✅ 故障排查指南

### 4. 易用性
- ✅ 一键部署脚本
- ✅ 快速开始指南
- ✅ 部署检查清单
- ✅ 模拟演练工具

## 📝 使用指南

### 首次部署

1. **阅读文档**
   ```bash
   cat deploy/QUICKSTART.md
   ```

2. **服务器准备**
   ```bash
   # 在服务器上执行
   sudo ./deploy/scripts/init-server.sh
   ```

3. **配置 GitHub**
   - 创建 `production` 分支
   - 配置 GitHub Secrets（参考 deploy/GITHUB_SECRETS.md）

4. **触发部署**
   ```bash
   git checkout production
   git merge main
   git push origin production
   ```

### 日常运维

```bash
# 查看日志
./scripts/view-logs.sh all

# 重启服务
./scripts/restart.sh api

# 备份数据库
./scripts/backup-database.sh

# 健康检查
./scripts/health-check.sh
```

### 故障排查

```bash
# 查看容器状态
docker ps -a

# 查看详细日志
./scripts/view-logs.sh api 200

# 检查配置
cat .env.production

# 测试连接
curl http://localhost:5000/health
```

## 🔧 配置说明

### 必需的 GitHub Secrets

| Secret | 说明 | 示例 |
|--------|------|------|
| SSH_HOST | 服务器 IP | 192.168.1.100 |
| SSH_PORT | SSH 端口 | 22 |
| SSH_USER | SSH 用户 | ubuntu |
| SSH_PRIVATE_KEY | SSH 私钥 | -----BEGIN... |

### 必需的环境变量（.env.production）

```bash
GITHUB_REPO_OWNER=your-github-username
DB_PASSWORD=your_secure_password
BOOTSTRAP_ADMIN_PASSWORD=your_admin_password
CORS_ALLOWED_ORIGIN=http://your-domain.com
API_PORT=5000
WEB_PORT=80
```

## 🧪 测试与验证

### 本地测试

```bash
# 运行模拟演练
bash deploy/scripts/simulate-deployment.sh

# 检查 Docker Compose 配置
docker-compose -f deploy/docker-compose.prod.yml config

# 检查脚本语法
bash -n deploy/scripts/*.sh
```

### 部署后验证

```bash
# 健康检查
./scripts/health-check.sh

# 访问测试
curl http://localhost:5000/health
curl http://localhost/

# 查看日志
./scripts/view-logs.sh all
```

## 📚 文档索引

| 文档 | 用途 | 适用人群 |
|------|------|----------|
| [QUICKSTART.md](./QUICKSTART.md) | 快速部署 | 首次部署 |
| [README.md](./README.md) | 完整指南 | 运维人员 |
| [GITHUB_SECRETS.md](./GITHUB_SECRETS.md) | Secrets 配置 | 配置人员 |
| [CHECKLIST.md](./CHECKLIST.md) | 检查清单 | 所有人 |

## 🚀 下一步

1. **立即开始**：阅读 [QUICKSTART.md](./QUICKSTART.md)
2. **深入了解**：阅读 [README.md](./README.md)
3. **配置 Secrets**：参考 [GITHUB_SECRETS.md](./GITHUB_SECRETS.md)
4. **执行检查**：使用 [CHECKLIST.md](./CHECKLIST.md)

## 💡 最佳实践

1. **部署前**
   - 在测试环境验证
   - 备份生产数据
   - 通知相关人员

2. **部署时**
   - 选择低峰时段
   - 监控部署日志
   - 准备回滚方案

3. **部署后**
   - 执行健康检查
   - 验证核心功能
   - 监控系统指标

## 🔒 安全建议

- ✅ 定期更新系统和依赖
- ✅ 使用强密码和密钥
- ✅ 启用防火墙和 SSH 密钥认证
- ✅ 定期备份数据库
- ✅ 监控异常访问
- ✅ 配置 SSL 证书（生产环境）

## 📞 获取帮助

- 📖 查看文档：deploy/ 目录下的所有 .md 文件
- 🐛 报告问题：GitHub Issues
- 💬 技术支持：联系项目维护者

---

**最后更新**: 2026-03-17
**版本**: 1.0.0
**维护者**: your-github-username
