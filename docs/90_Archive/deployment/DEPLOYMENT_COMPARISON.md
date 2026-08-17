# 部署方式选择指南

本项目支持两种部署方式，根据你的需求选择：

## 快速对比

| 维度 | GitHub Actions | Dokploy |
|------|---------------|---------|
| **难度** | ⭐⭐⭐ 中等 | ⭐ 简单 |
| **配置时间** | 30-60 分钟 | 10-20 分钟 |
| **服务器要求** | 任意 Linux | Ubuntu/Debian/CentOS |
| **管理方式** | SSH + 命令行 | Web UI |
| **监控** | 需自行配置 | 内置 |
| **日志查看** | SSH 登录 | Web UI 实时查看 |
| **SSL 证书** | 手动配置 | 自动申请 |
| **回滚** | 手动脚本 | 一键回滚 |
| **适合场景** | 大型团队、复杂流程 | 小团队、快速上线 |

---

## 方案 A：GitHub Actions（已配置）

### 优点
- ✅ 完全自动化 CI/CD
- ✅ 灵活的部署流程控制
- ✅ 支持多环境部署
- ✅ 与 GitHub 深度集成
- ✅ 免费（GitHub Actions 额度）

### 缺点
- ❌ 需要配置 SSH 密钥
- ❌ 需要手动管理服务器
- ❌ 日志查看需要 SSH 登录
- ❌ 监控需要额外配置

### 适合你如果
- 团队有 DevOps 经验
- 需要复杂的部署流程（测试、审批等）
- 已有服务器监控方案
- 希望完全控制部署过程

### 快速开始
1. 配置 GitHub Secrets（6 个）
2. 推送到 `production` 分支
3. 自动触发部署

📖 详细文档：[DEPLOYMENT_FIXES.md](./DEPLOYMENT_FIXES.md)

---

## 方案 B：Dokploy（推荐新手）

### 优点
- ✅ 一键安装，开箱即用
- ✅ Web UI 管理，无需 SSH
- ✅ 内置监控和日志查看
- ✅ 自动 SSL 证书
- ✅ 一键回滚
- ✅ Git 集成自动部署

### 缺点
- ❌ 需要安装 Dokploy（占用资源）
- ❌ 灵活性不如 GitHub Actions
- ❌ 社区相对较小

### 适合你如果
- 小团队或个人项目
- 希望快速上线
- 不想管理复杂的 CI/CD
- 需要 Web UI 管理

### 快速开始
1. 一键安装 Dokploy
2. 连接 GitHub 仓库
3. 配置环境变量
4. 点击部署

📖 详细文档：[DOKPLOY_DEPLOYMENT.md](./DOKPLOY_DEPLOYMENT.md)

---

## 方案 C：混合方案（最佳实践）

结合两者优势：

1. **GitHub Actions** 负责：
   - 构建 Docker 镜像
   - 运行测试
   - 推送到 GHCR

2. **Dokploy** 负责：
   - 拉取镜像
   - 部署到服务器
   - 监控和日志
   - 回滚管理

### 配置步骤

#### 1. 保留 GitHub Actions 构建流程

修改 `.github/workflows/deploy-production.yml`，仅保留 `build` job：

```yaml
jobs:
  build:
    runs-on: ubuntu-latest
    steps:
      - name: Build and push images
        # ... 保留构建步骤
```

移除 `deploy` job（由 Dokploy 接管）。

#### 2. 在 Dokploy 中配置

- 使用 **Docker Compose** 方式
- 镜像来源：GHCR（GitHub Actions 构建）
- 启用 Webhook 自动部署

#### 3. 工作流程

```
代码推送 → GitHub Actions 构建镜像 → 推送到 GHCR
                                          ↓
                                    Dokploy 检测到新镜像
                                          ↓
                                    自动拉取并部署
```

---

## 推荐选择

### 如果你是...

**个人开发者 / 小团队（1-3人）**
→ 选择 **Dokploy**
- 理由：快速上线，Web UI 管理方便

**中型团队（4-10人）**
→ 选择 **混合方案**
- 理由：兼顾自动化和易用性

**大型团队（10人以上）**
→ 选择 **GitHub Actions**
- 理由：完全控制，支持复杂流程

**首次部署 / 学习阶段**
→ 选择 **Dokploy**
- 理由：降低学习曲线，快速看到效果

---

## 成本对比

### GitHub Actions
- **GitHub Actions 额度**：免费账户 2000 分钟/月
- **服务器成本**：$5-20/月（VPS）
- **总成本**：$5-20/月

### Dokploy
- **Dokploy**：开源免费
- **服务器成本**：$10-30/月（需要更多资源）
- **总成本**：$10-30/月

### 混合方案
- **GitHub Actions 额度**：免费账户 2000 分钟/月
- **服务器成本**：$10-30/月
- **总成本**：$10-30/月

---

## 迁移指南

### 从 GitHub Actions 迁移到 Dokploy

1. 安装 Dokploy
2. 在 Dokploy 中创建应用
3. 配置环境变量（复制自 `.env.production`）
4. 部署
5. 验证成功后，禁用 GitHub Actions workflow

### 从 Dokploy 迁移到 GitHub Actions

1. 配置 GitHub Secrets
2. 在服务器上创建部署目录 `/opt/finance`
3. 复制 `.env.production` 到服务器
4. 推送到 `production` 分支触发部署
5. 验证成功后，停止 Dokploy 应用

---

## 常见问题

### Q: 可以同时使用两种方式吗？

A: 不建议。会导致部署冲突和资源浪费。选择一种方式或使用混合方案。

### Q: 哪种方式更稳定？

A: 两者都很稳定。GitHub Actions 依赖 GitHub 服务，Dokploy 依赖你的服务器。

### Q: 哪种方式更快？

A: Dokploy 部署更快（无需 SSH 连接），但 GitHub Actions 构建可能更快（GitHub 服务器性能好）。

### Q: 我应该先学哪个？

A: 建议先学 Dokploy（更简单），熟悉部署流程后再学 GitHub Actions。

---

## 下一步

1. 根据上述对比选择部署方式
2. 阅读对应的详细文档
3. 按照步骤完成部署
4. 验证应用运行正常

---

## 相关文档

- [GitHub Actions 部署](./DEPLOYMENT_FIXES.md)
- [Dokploy 部署](./DOKPLOY_DEPLOYMENT.md)
- [部署 Review 报告](./REVIEW_REPORT.md)
- [GitHub Secrets 配置](./GITHUB_SECRETS.md)

---

**最后更新**：2026-03-18
