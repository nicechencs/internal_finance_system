# 数据模型

状态：Active
适用对象：开发 / 测试 / AI
事实源级别：Primary
最后核对日期：2026-04-09
代码依据：[`database/schema/01_database_schema.sql`](../../database/schema/01_database_schema.sql), [`backend/FinanceApp.Domain/Entities`](../../backend/FinanceApp.Domain/Entities)

## 核心实体

- `users`
- `accounts`
- `categories`
- `customers`
- `suppliers`
- `persons`
- `projects`
- `import_batches`
- `bank_transactions`
- `transactions`
- `transaction_allocations`
- `receivables` / `receivable_details`
- `payables` / `payable_details`
- `classification_rules`
- `tags`
- `tag_bindings`
- `tag_daily_summaries`
- `tag_rules`
- `tag_rule_tags`
- `fixed_deposit_records`
- `payable_types`
- `audit_logs`
- `system_configs`

## 核心关系

- 账户与银行流水：1:N
- 银行流水与交易：1:1/1:N
- 交易与分摊：1:N
- 客户与项目：1:N
- 项目、客户、供应商、人员都可以与交易建立关联
- 应收应付通过明细与交易关联
- 标签通过 `tag_bindings` 多态绑定到交易、项目、人员、客户、供应商（`scope` 字段区分）
- 标签规则通过 `tag_rule_tags` 关联待应用的标签
- 应付款通过 `payable_types` 管理类型定义

## 关键约定

- 软删除：核心业务表使用 `is_deleted` / `deleted_at`
- 时间戳：保留 `created_at` / `updated_at`
- 金额精度：统一使用高精度金额字段
- 去重：银行流水基于唯一哈希去重

## 命名修正

- 当前事实应统一使用 `bank_transactions`
- 不再把 `bank_statements` 当成当前表名

## 参考资产

- Schema：[`database/schema/01_database_schema.sql`](../../database/schema/01_database_schema.sql)
- ER 图：[`database/reference/er_diagram.md`](../../database/reference/er_diagram.md)
- 历史手工补丁：`database/manual_sql/legacy/`

## 相关文档

- [模块与业务规则](../01_Product/02_modules_and_rules.md)
- [导入规则与使用限制](../01_Product/03_import_rules.md)
