# 导入规则与使用限制

状态：Active
适用对象：产品 / 开发 / 测试 / 运维 / AI
事实源级别：Primary
最后核对日期：2026-04-24
代码依据：[`backend/FinanceApp.Api/Controllers/Reconciliation/ImportController.cs`](../../backend/FinanceApp.Api/Controllers/Reconciliation/ImportController.cs), [`backend/FinanceApp.Api/Controllers/MasterData/CustomerController.cs`](../../backend/FinanceApp.Api/Controllers/MasterData/CustomerController.cs), [`frontend/src/features/import/pages/ImportPage.vue`](../../frontend/src/features/import/pages/ImportPage.vue)

## 关键提示

- 只有银行流水导入强制要求 `.xlsx`。
- 客户、供应商、人员、项目的批量导入当前支持 `.xlsx`、`.xls`、`.csv`。
- 先维护基础数据，再导入流水，能显著降低后续手工修正成本。

## 导入类型矩阵

| 类型 | 当前入口 | 支持格式 | 说明 |
| --- | --- | --- | --- |
| 银行流水导入 | `/api/import/preview` | `.xlsx` | 必须先预览，再确认导入 |
| 客户批量导入 | `/api/customer/batch-import` | `.xlsx` / `.xls` / `.csv` | 主数据导入 |
| 供应商批量导入 | `/api/supplier/batch-import` | `.xlsx` / `.xls` / `.csv` | 主数据导入 |
| 人员批量导入 | `/api/person/batch-import` | `.xlsx` / `.xls` / `.csv` | 主数据导入 |
| 项目批量导入 | `/api/projects/batch-import` | `.xlsx` / `.xls` / `.csv` | 主数据导入 |

## 银行流水导入主流程

1. 上传 `.xlsx` 文件并指定账户
2. 解析流水并生成预览结果
3. 根据唯一哈希检测重复
4. 按规则引擎尝试自动分类和自动关联
5. 用户确认选中的记录
6. 写入导入批次与银行流水
7. 生成交易记录

## 去重机制

- 当前核心思路是基于日期、金额、对方、摘要等信息生成唯一哈希
- 去重用于防止重复导入同一流水

## 导入后能力

- 可查看导入批次列表
- 可查看导入批次详情
- 可读取批次预览缓存继续处理
- 可对历史交易进行一键关联或批量智能关联
- 可对历史交易执行规则重跑预览与确认

## 使用限制

### 操作顺序限制

- 如果先导入流水，后创建客户/供应商/项目等基础数据，历史记录不会自动全量回填关联。
- 当前已有人工或半自动关联工具，但仍不等于“默认自动回填”。

### 重名歧义

- 对方名称重复时，自动匹配可能出现歧义。

### 凭证号

- `description` 与 `memo` 已拆分保存。
- `voucher_no` 暂未确认已落地，仍按未完成能力记录。

## 相关文档

- [已知问题](05_known_issues.md)
- [业务决策记录](04_business_decisions.md)
- [Swagger 与接口示例](../03_API/02_swagger_and_examples.md)
