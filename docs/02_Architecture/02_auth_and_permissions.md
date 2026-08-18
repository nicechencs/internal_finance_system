# 认证与权限

状态：Active
适用对象：开发 / 测试 / 运维 / AI
事实源级别：Primary
最后核对日期：2026-04-24
代码依据：[`backend/FinanceApp.Api/Program.cs`](../../backend/FinanceApp.Api/Program.cs), [`backend/FinanceApp.Api/Controllers/Identity/AuthController.cs`](../../backend/FinanceApp.Api/Controllers/Identity/AuthController.cs), [`frontend/src/shared/utils/request.ts`](../../frontend/src/shared/utils/request.ts)

## 关键提示

- 当前主认证方案是“本地账号 + 服务端 Cookie 会话”。
- 不开放公开注册。
- 不再以前端持久化 JWT 作为当前事实。
- `GET /api/public/brand` 是匿名可读的品牌接口，只返回站点名称字段。

## 认证模型

- 用户使用用户名和密码登录
- 服务端写入 Cookie 会话
- 前端请求统一 `withCredentials: true`
- 会话会结合 `SecurityStamp`、锁定状态、启停用状态进行校验

## 用户与角色

- `Admin`
- `Accountant`
- `Viewer`

## 权限边界

- `Admin`：系统管理与全部业务访问
- `Accountant`：大多数业务写操作与查看权限
- `Viewer`：以查看为主，数据范围受限

## 数据权限

- 当前系统存在按创建人和角色控制可见范围的能力
- 权限规则应以代码实现和当前测试为准，不以旧计划文档为准

## 安全策略

- 登录限流
- 连续失败锁定
- 避免用户名枚举
- 改密后旧会话失效
- 管理员重置密码后旧会话失效

## BootstrapAdmin 与 CLI

### BootstrapAdmin

- 用于首次部署初始化管理员
- 首次部署完成后应关闭

### CLI

- 支持创建用户、设置密码、解锁、启停用等管理操作

## 当前事实源修正

- 旧文档中的 JWT 主认证口径已失效
- 旧文档中的 `/api/v1/auth/*` 路由口径已失效

## 相关文档

- [API 约定](../03_API/01_api_conventions.md)
- [生产部署](../05_Operations/01_deployment.md)
