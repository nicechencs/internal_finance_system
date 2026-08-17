# 本地账号认证与用户管理设计

## 1. 目标与约束

适用场景：

- 小团队、个人、家庭内部使用
- 可能部署到公网
- 需要账号密码登录

明确约束：

- 不开放公开注册
- 不做邮箱验证
- 不做忘记密码 / 邮件重置密码
- 管理员可以在后台管理其他用户
- 服务器命令行可以直接设置或重置密码
- 需要登录限流、失败锁定、避免用户名枚举
- 需要前端密码修改界面和管理员用户管理界面

## 2. 方案选择

采用“本地账号 + 服务端 Cookie 会话”方案，而不是 JWT 持久化到前端。

原因：

- 对这个系统来说，账号来源完全可控，不需要开放注册和外部身份平台接入
- 服务端 Cookie 会话更适合浏览器端内部系统，不需要前端存储 token
- 改密、禁用、管理员重置密码后，可以通过 `SecurityStamp` 让旧会话自动失效
- 配合 `SameSite`、`HttpOnly` 和服务端校验，整体更稳、更省心

## 3. 用户模型

当前版本将认证字段保存在 `users` 表中，不额外拆分 `user_credentials` 表，优先保证落地简单、运维直接。

关键字段：

- `username`：显示用户名
- `normalized_username`：登录时使用的大写标准化用户名
- `password_hash`：BCrypt 哈希
- `security_stamp`：会话失效戳
- `role`：`Admin` / `Accountant` / `Viewer`
- `is_active`：是否启用
- `must_change_password`：下次登录后是否必须改密
- `access_failed_count`：连续失败次数
- `lockout_end_at`：锁定截止时间
- `last_login_at`：最近登录时间
- `password_changed_at`：最近改密时间

## 4. 登录安全策略

### 4.1 密码存储

- 使用 BCrypt 存储密码
- 最小密码长度 10
- 不在前端或本地存储明文密码

### 4.2 登录流程

1. 用户输入用户名和密码
2. 服务端把用户名标准化为 `normalized_username`
3. 如果用户不存在，执行一次 dummy hash 校验，避免明显时间差
4. 如果用户存在，检查是否在锁定期
5. 校验密码
6. 成功时重置失败计数并写入 `last_login_at`
7. 通过 Cookie 建立会话

### 4.3 防暴力破解

- `POST /api/auth/login` 增加 IP 级别限流
- 同一账号连续失败达到阈值后临时锁定
- 匿名登录失败统一返回“用户名或密码错误”
- 不区分“不存在 / 密码错误 / 已禁用 / 已锁定”的外部文案

### 4.4 会话失效

每次请求都会校验 Cookie 中的 `SecurityStamp`：

- 用户改密后，旧会话失效
- 管理员重置密码后，旧会话失效
- 用户被禁用后，旧会话失效

## 5. 后端接口设计

### 5.1 认证接口

- `POST /api/auth/login`
  - 输入：`username`、`password`
  - 输出：`user`、`mustChangePassword`
  - 行为：写入 Cookie

- `POST /api/auth/logout`
  - 行为：清理 Cookie 会话

- `GET /api/auth/me`
  - 行为：获取当前登录用户

- `POST /api/auth/change-password`
  - 输入：`currentPassword`、`newPassword`
  - 行为：校验当前密码，改密，刷新当前会话

### 5.2 管理员用户管理接口

- `GET /api/users`
  - 获取用户列表和安全状态

- `POST /api/users`
  - 创建用户

- `PUT /api/users/{id}/password`
  - 管理员设置用户密码

- `PUT /api/users/{id}/status`
  - 启用 / 禁用用户

- `POST /api/users/{id}/unlock`
  - 手动解锁用户

### 5.3 命令行接口

通过：

```bash
dotnet run --project backend/FinanceApp.Api -- auth-cli <command>
```

支持：

- `create-user`
- `set-password`
- `unlock-user`
- `set-active`

## 6. 前端交互设计

### 6.1 登录页

页面元素：

- 用户名输入框
- 密码输入框
- 登录按钮

交互规则：

- 不显示“注册”
- 不显示“忘记密码”
- 登录成功后如果 `mustChangePassword = true`，直接跳转“账号安全”

### 6.2 用户自己修改密码

入口：

- 右上角头像菜单
- 菜单项名称：`账号安全`

页面内容：

- 当前密码
- 新密码
- 确认新密码
- 密码安全提示

交互规则：

- 当前密码必须正确
- 新密码至少 10 位
- 两次新密码必须一致
- 修改成功后保留当前会话，但其他旧会话自动失效

### 6.3 管理员管理其他用户

入口：

- 菜单分组：`系统设置`
- 页面：`用户管理`

页面内容建议：

- 用户列表
- 搜索框
- 状态标签：启用 / 禁用、已锁定 / 正常
- 最近登录时间
- 操作按钮：新建用户、设置密码、解锁、启用 / 禁用

新建用户弹窗：

- 用户名
- 姓名
- 邮箱（可选）
- 角色
- 初始密码
- 是否立即启用
- 是否首次登录强制改密

设置密码弹窗：

- 新密码
- 确认新密码
- 是否下次登录强制改密

管理规则：

- 管理员不能禁用当前正在登录的自己
- 至少保留一个启用的管理员账号
- 管理员重置他人密码后，对方旧会话失效

## 7. 初始化与运维

推荐生产初始化方式：

1. 首次部署时临时启用 `Auth:BootstrapAdmin`
2. 启动后创建正式管理员
3. 关闭 `BootstrapAdmin`

或直接用命令行创建首个管理员：

```bash
dotnet run --project backend/FinanceApp.Api -- auth-cli create-user --username admin --password "ChangeMe123!" --full-name "系统管理员" --role Admin
```

## 8. 当前实现状态

已落地：

- Cookie 会话认证
- 登录限流
- 失败锁定
- 避免用户名枚举
- 自助改密页面
- 管理员用户管理页面
- 管理员命令行
- 集成测试与前端关键测试通过

暂未做：

- 忘记密码
- 邮箱验证
- MFA / TOTP
- 外部 SSO / OAuth

## 9. 后续可选增强

- 管理员编辑用户资料与角色
- 登录审计列表
- MFA
- 设备会话列表与手动下线
- 首次部署引导页
