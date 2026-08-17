# GitHub Secrets 配置指南

本文档说明如何配置 GitHub Actions 所需的 Secrets。

## 配置步骤

1. 访问 GitHub 仓库页面
2. 点击 `Settings` → `Secrets and variables` → `Actions`
3. 点击 `New repository secret`
4. 按照下表逐个添加 Secret

## 必需的 Secrets

| Secret 名称 | 说明 | 如何获取 | 示例值 |
|------------|------|---------|--------|
| `SSH_HOST` | 服务器 IP 地址或域名 | 服务器提供商控制台 | `192.168.1.100` 或 `server.example.com` |
| `SSH_PORT` | SSH 端口 | 默认为 22，如已修改则填写新端口 | `22` 或 `2222` |
| `SSH_USER` | SSH 登录用户名 | 服务器用户名 | `ubuntu` 或 `root` |
| `SSH_PRIVATE_KEY` | SSH 私钥 | 见下方"获取 SSH 私钥"部分 | `-----BEGIN OPENSSH PRIVATE KEY-----\n...` |
| `GHCR_USER` | GitHub Container Registry 用户名 | 你的 GitHub 用户名 | `your-github-username` |
| `GHCR_TOKEN` | GitHub Personal Access Token | 见下方"获取 GHCR Token"部分 | `ghp_xxxxxxxxxxxx` |

> 数据库密码等运行时配置统一在服务器 `.env.production` 文件中管理，无需配置为 GitHub Secrets。

## 获取 GHCR Token

GitHub Container Registry 需要 Personal Access Token (PAT) 来拉取私有镜像。

1. 访问 GitHub Settings → Developer settings → Personal access tokens → Tokens (classic)
2. 点击 "Generate new token" → "Generate new token (classic)"
3. 设置 Token 名称：`finance-deploy`
4. 设置过期时间：建议 90 天或更长
5. 勾选权限：
   - `read:packages` - 读取容器镜像
   - `write:packages` - 推送容器镜像（可选，仅 Actions 需要）
6. 点击 "Generate token"
7. **立即复制 Token**（只显示一次）
8. 将 Token 添加到 GitHub Secrets：
   - `GHCR_USER`: 你的 GitHub 用户名
   - `GHCR_TOKEN`: 刚才生成的 Token

**重要提示**：
- Token 只在生成时显示一次，请妥善保存
- 如果 Token 泄露，立即在 GitHub 设置中撤销并重新生成
- 定期轮换 Token（建议每 3-6 个月）

## 获取 SSH 私钥

### 方法一：使用现有密钥

如果服务器已有 SSH 密钥对：

```bash
# 在服务器上查看私钥
cat ~/.ssh/id_rsa
# 或
cat ~/.ssh/id_ed25519
```

复制完整输出（包括 `-----BEGIN` 和 `-----END` 行）。

### 方法二：生成新密钥

在服务器上执行：

```bash
# 生成 ED25519 密钥（推荐）
ssh-keygen -t ed25519 -C "github-actions-deploy" -f ~/.ssh/github_actions_key -N ""

# 将公钥添加到 authorized_keys
cat ~/.ssh/github_actions_key.pub >> ~/.ssh/authorized_keys

# 设置权限
chmod 600 ~/.ssh/authorized_keys
chmod 600 ~/.ssh/github_actions_key

# 显示私钥（用于配置 GitHub Secret）
cat ~/.ssh/github_actions_key
```

**重要提示**：
- 复制私钥时，确保包含完整内容（包括开头和结尾的标记行）
- 不要在私钥中添加额外的空格或换行
- 私钥应该类似这样：
  ```
  -----BEGIN OPENSSH PRIVATE KEY-----
  b3BlbnNzaC1rZXktdjEAAAAABG5vbmUAAAAEbm9uZQAAAAAAAAABAAAAMwAAAAtzc2gtZW
  ...（多行）...
  -----END OPENSSH PRIVATE KEY-----
  ```

### 方法三：从本地生成并上传

在本地计算机上：

```bash
# 生成密钥对
ssh-keygen -t ed25519 -C "github-actions-deploy" -f ./github_actions_key

# 将公钥上传到服务器
ssh-copy-id -i ./github_actions_key.pub user@server_ip

# 查看私钥（用于配置 GitHub Secret）
cat ./github_actions_key
```

## 生成强密码

### 数据库密码

```bash
# Linux/Mac
openssl rand -base64 32

# 或使用在线工具
# https://passwordsgenerator.net/
```

## 验证配置

配置完成后，可以通过以下方式验证：

### 1. 测试 SSH 连接

在本地计算机上：

```bash
# 将私钥保存到临时文件
echo "YOUR_PRIVATE_KEY" > /tmp/test_key
chmod 600 /tmp/test_key

# 测试连接
ssh -i /tmp/test_key -p SSH_PORT SSH_USER@SSH_HOST "echo 'SSH connection successful'"

# 清理临时文件
rm /tmp/test_key
```

### 2. 手动触发工作流

1. 访问 GitHub 仓库的 `Actions` 标签页
2. 选择 `Deploy to Production` 工作流
3. 点击 `Run workflow` → 选择 `production` 分支 → `Run workflow`
4. 观察执行日志，检查是否有错误

## 安全建议

1. **定期轮换密钥**
   - 建议每 3-6 个月更换一次 SSH 密钥和密码
   - 更换后需同步更新 GitHub Secrets

2. **最小权限原则**
   - SSH 用户应使用非 root 账户
   - 仅授予部署所需的最小权限

3. **审计日志**
   - 定期检查 GitHub Actions 执行日志
   - 监控服务器登录日志：`sudo tail -f /var/log/auth.log`

4. **备份 Secrets**
   - 将 Secrets 信息安全存储（如密码管理器）
   - 不要将 Secrets 提交到代码仓库

5. **限制分支保护**
   - 为 `production` 分支启用保护规则
   - 要求代码审查后才能合并

## 常见问题

### Q: SSH 连接失败怎么办？

A: 检查以下几点：
- SSH_HOST、SSH_PORT、SSH_USER 是否正确
- SSH_PRIVATE_KEY 是否完整（包括开头和结尾）
- 服务器防火墙是否开放 SSH 端口
- 服务器 `~/.ssh/authorized_keys` 是否包含对应公钥

### Q: 如何更新已配置的 Secret？

A:
1. 进入 `Settings` → `Secrets and variables` → `Actions`
2. 找到要更新的 Secret
3. 点击 `Update` 按钮
4. 输入新值并保存

### Q: Secret 会在日志中显示吗？

A: 不会。GitHub Actions 会自动屏蔽 Secrets 的值，在日志中显示为 `***`。

### Q: 可以在多个仓库共享 Secrets 吗？

A: 可以使用 Organization Secrets，但需要 GitHub 组织账户。

---

**配置完成后，请参考 [deploy/README.md](./README.md) 进行首次部署。**
