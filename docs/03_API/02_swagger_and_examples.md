# Swagger 与接口示例

状态：Active
适用对象：开发 / 测试 / AI
事实源级别：Secondary
最后核对日期：2026-04-24
代码依据：[`backend/FinanceApp.Api/Controllers`](../../backend/FinanceApp.Api/Controllers), [`frontend/src/features`](../../frontend/src/features), [`frontend/src/shared/utils/request.ts`](../../frontend/src/shared/utils/request.ts)

## Swagger

- 本地开发默认地址：`http://localhost:5187/swagger`
- 生产环境通常通过 `web` 代理后的 `/api` 访问后端

## 关键接口分组

### 认证

- `POST /api/auth/login`
- `POST /api/auth/logout`
- `GET /api/auth/me`
- `POST /api/auth/change-password`
- `PUT /api/auth/profile`

### 导入与批次

- `POST /api/import/preview`
- `POST /api/import/confirm`
- `GET /api/import/batches`
- `GET /api/import/batches/{id}`
- `GET /api/import/batches/{id}/preview`

### 历史关联与规则重跑

- `POST /api/link/preview`
- `POST /api/link/confirm`
- `POST /api/link/batch-preview`
- `POST /api/link/batch-confirm`
- `POST /api/link/rule-rerun/preview`
- `POST /api/link/rule-rerun/confirm`

### 交易统计

- `GET /api/transactions/statistics`
- `GET /api/transactions/account/{accountId}/statistics`
- `GET /api/transactions/customer/{customerId}/statistics`

## 示例说明

- 详细字段以 Swagger 当前输出为准
- 当前示例页只保留“容易误写”的接口组，不再重复所有 CRUD

## 相关文档

- [API 约定](01_api_conventions.md)
- [导入规则与使用限制](../01_Product/03_import_rules.md)
