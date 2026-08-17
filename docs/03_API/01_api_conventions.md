# API 约定

状态：Active
适用对象：开发 / 测试 / AI
事实源级别：Primary
最后核对日期：2026-04-24
代码依据：[`backend/FinanceApp.Api/Controllers`](../../backend/FinanceApp.Api/Controllers), [`backend/FinanceApp.Application/Common/ApiResponse.cs`](../../backend/FinanceApp.Application/Common/ApiResponse.cs), [`frontend/src/shared/utils/request.ts`](../../frontend/src/shared/utils/request.ts)

## 关键提示

- 当前基础路径是 `/api/*`，不是 `/api/v1/*`。
- 当前主认证方式是 Cookie 会话，不是前端持久化 JWT。
- Swagger 是接口事实源，本文档只保留约定和关键示例。

## 基础路径

- 大多数控制器采用 `api/[controller]`
- 少数控制器为显式路径，例如 `api/reports`、`api/audit-logs`、`api/configs`

## 认证方式

- 登录：`POST /api/auth/login`
- 当前用户：`GET /api/auth/me`
- 退出：`POST /api/auth/logout`
- 前端通过 Cookie 会话和 `withCredentials` 访问受保护接口

## 统一响应结构

当前应统一记录以下字段：

- `success`
- `code`
- `message`
- `data`
- `timestamp`
- `errors`

## 命名约定

- 后端类和属性：PascalCase
- JSON 字段：camelCase
- 数据库字段：snake_case
- 路由：当前以控制器名衍生为主，不强行套用旧版复数 REST 口径

## 分页与排序

- 分页和排序能力应以具体接口实现为准
- 文档中不再笼统写“全部接口都统一支持某种参数”，除非代码已统一

## Swagger 与类型生成

- 本地 Swagger 默认入口：`http://localhost:5187/swagger`
- 前端可基于 Swagger 生成类型
- 生成流程应与当前实际端口和路径保持一致

## 当前已失效的旧口径

- JWT Bearer 为主认证
- `/api/v1/*` 为主路径
- 纯手写大而全接口书

## 相关文档

- [Swagger 与接口示例](02_swagger_and_examples.md)
- [认证与权限](../02_Architecture/02_auth_and_permissions.md)
