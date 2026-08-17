# 财务管理系统 - ER 关系图

## 系统 ER 全局关系图

```
┌─────────────────────────────────────────────────────────────────────────────────────────────┐
│                              财务管理系统 ER 关系图                                           │
└─────────────────────────────────────────────────────────────────────────────────────────────┘

┌──────────────┐         ┌──────────────────┐         ┌──────────────┐
│   users      │         │  system_configs   │         │ audit_logs   │
│──────────────│         │──────────────────│         │──────────────│
│ id (PK)      │◄────────│ id (PK)          │         │ id (PK)      │
│ username     │         │ config_key       │         │ user_id (FK) │──┐
│ password_hash│         │ config_value     │         │ action       │  │
│ full_name    │         │ config_type      │         │ entity_type  │  │
│ role         │         └──────────────────┘         │ entity_id    │  │
│ is_active    │                                      │ old_value    │  │
└──────┬───────┘                                      │ new_value    │  │
       │                                              └──────────────┘  │
       │  1:N (创建者)                                                   │
       ├────────────────────────────────────────────────────────────────┘
       │
       │         ┌──────────────────┐
       │         │   accounts       │
       │         │──────────────────│
       │         │ id (PK)          │◄──────────────────────────────────┐
       │         │ name             │                                    │
       │         │ account_type     │                                    │
       │         │ account_number   │                                    │
       │         │ opening_balance  │                                    │
       │         │ current_balance  │                                    │
       │         └────────┬─────────┘                                    │
       │                  │                                              │
       │                  │ 1:N                                          │
       │                  ▼                                              │
       │         ┌──────────────────┐         ┌──────────────────┐      │
       │         │ import_batches   │         │ bank_transactions│      │
       │         │──────────────────│    1:N  ���──────────────────│      │
       │         │ id (PK)          │────────►│ id (PK)          │      │
       │         │ account_id (FK)  │         │ account_id (FK)  │──────┘
       │         │ file_name        │         │ import_batch_id  │
       │         │ record_count     │         │ transaction_date │
       │         │ success_count    │         │ amount           │
       │         │ status           │         │ direction        │
       │         └──────────────────┘         │ counterparty     │
       │                                      │ unique_hash (UQ) │
       │                                      │ is_processed     │
       │                                      └────────┬─────────┘
       │                                               │
       │                                               │ 1:1 / 1:N
       │                                               ▼
       │                                      ┌──────────────────┐
       │  1:N (created_by)                    │  transactions    │
       └─────────────────────────────────────►│──────────────────│
                                              │ id (PK)          │
              ┌──────────────────┐            │ bank_txn_id (FK) │
              │   categories     │◄───────────│ category_id (FK) │
              │──────────────────│            │ account_id (FK)  │
              │ id (PK)          │            │ project_id (FK)  │───────┐
              │ name             │            │ customer_id (FK) │──┐    │
              │ parent_id (FK)   │──┐ self   │ supplier_id (FK) │  │    │
              │ category_type    │◄─┘ ref    │ person_id (FK)   │  │    │
              │ level            │            │ amount           │  │    │
              └──────────────────┘            │ transaction_type │  │    │
                                              │ status           │  │    │
                                              │ is_allocated     │  │    │
                                              └───────┬──────────┘  │    │
                                                      │             │    │
                                                      │ 1:N         │    │
                                                      ▼             │    │
                                              ┌──────────────────┐  │    │
                                              │ transaction_     │  │    │
                                              │ allocations      │  │    │
                                              │──────────────────│  │    │
                                              │ id (PK)          │  │    │
                                              │ transaction_id   │  │    │
                                              │ project_id (FK)  │──┼────┤
                                              │ amount           │  │    │
                                              │ allocation_rate  │  │    │
                                              └──────────────────┘  │    │
                                                                    │    │
       ┌────────────────────────────────────────────────────────────┘    │
       │                                                                 │
       ▼                                                                 ▼
┌──────────────┐         ┌──────────────┐         ┌──────────────────┐
│  customers   │         │  suppliers   │         │    projects      │
│──────────────│         │──────────────│         │──────────────────│
│ id (PK)      │         │ id (PK)      │         │ id (PK)          │
│ name         │         │ name         │         │ name             │
│ short_name   │         │ short_name   │         │ project_code (UQ)│
│ contact_*    │         │ contact_*    │         │ customer_id (FK) │──┐
│ tax_number   │         │ bank_account │         │ contract_amount  │  │
└──────┬───────┘         └──────┬───────┘         │ received_amount  │  │
       │                        │                  │ total_cost       │  │
       │ 1:N                    │ 1:N              │ profit_amount    │  │
       ▼                        ▼                  │ status           │  │
┌──────────────┐         ┌──────────────┐         └──────────────────┘  │
│ receivables  │         │  payables    │                │               │
│──────────────│         │──────────────│                │               │
│ id (PK)      │         │ id (PK)      │                │ 1:N           │
│ project_id   │         │ supplier_id  │                │               │
│ customer_id  │         │ project_id   │                │               │
│ total_amount │         │ total_amount │                │               │
│ received_amt │         │ paid_amount  │                │               │
│ remaining    │         │ remaining    │                │               │
│ status       │         │ status       │                │               │
└──────┬───────┘         └──────┬───────┘                │               │
       │ 1:N                    │ 1:N                    │               │
       ▼                        ▼                        │               │
┌──────────────┐         ┌──────────────┐                │               │
│ receivable_  │         │ payable_     │                │               │
│ details      │         │ details      │                │               │
│──────────────│         │──────────────│                │               │
│ id (PK)      │         │ id (PK)      │                │               │
│ receivable_id│         │ payable_id   │                │               │
│ txn_id (FK)  │         │ txn_id (FK)  │                │               │
│ amount       │         │ amount       │                │               │
└──────────────┘         └──────────────┘                │               │
                                                         │               │
                         ┌──────────────┐                │               │
                         │   persons    │◄───────────────┘               │
                         │──────────────│  N:1 (person_id)              │
                         │ id (PK)      │                                │
                         │ name         │         projects.customer_id   │
                         │ person_type  │◄───────────────────────────────┘
                         │ phone        │         (实际指向 customers)
                         │ join_date    │
                         └──────────────┘

              ┌──────────────────────┐
              │classification_rules  │
              │──────────────────────│
              │ id (PK)              │
              │ rule_name            │
              │ priority             │
              │ match_field          │
              │ match_operator       │
              │ match_value          │
              │ category_id (FK)     │──► categories
              │ project_id (FK)      │──► projects
              │ customer_id (FK)     │──► customers
              │ supplier_id (FK)     │──► suppliers
              │ person_id (FK)       │──► persons
              └──────────────────────┘
```

## 核心关系说明

### 表关系汇总

| 关系 | 类型 | 说明 |
|------|------|------|
| accounts → bank_transactions | 1:N | 一个账户有多条银行流水 |
| accounts → transactions | 1:N | 一个账户有多条交易记录 |
| import_batches → bank_transactions | 1:N | 一次导入产生多条流水 |
| bank_transactions → transactions | 1:1/1:N | 一条流水可生成一条或多条交易（拆分场景） |
| categories → transactions | 1:N | 一个分类下有多条交易 |
| categories → categories (self) | 1:N | 分类支持父子层级 |
| projects → transactions | 1:N | 一个项目关联多条交易 |
| customers → projects | 1:N | 一个客户有多个项目 |
| customers → receivables | 1:N | 一个客户有多条应收 |
| suppliers → payables | 1:N | 一个供应商有多条应付 |
| persons → transactions | 1:N | 一个人员关联多条交易 |
| transactions → transaction_allocations | 1:N | 一条交易可分摊到多个项目 |
| receivables → receivable_details | 1:N | 一条应收有多次收款明细 |
| payables → payable_details | 1:N | 一条应付有多次付款明细 |
| classification_rules → categories/projects/... | N:1 | 规则匹配后自动关联到对应实体 |
| users → audit_logs | 1:N | 一个用户有多条操作日志 |

### 核心业务流转

```
Excel文件
    │
    ▼
import_batches (记录导入批次)
    │
    ▼
bank_transactions (原始银行流水)
    │
    ├──► classification_rules (自动匹配规则)
    │
    ▼
transactions (核心交易记录)
    │
    ├──► 关联 project / customer / supplier / person
    │
    ├──► transaction_allocations (多项目分摊)
    │
    ├──► receivable_details (核销应收)
    │
    └──► payable_details (核销应付)
           │
           ▼
      报表视图 (v_project_profit / v_account_balance)
```

### 数据完整性约束

| 约束类型 | 说明 |
|----------|------|
| 唯一约束 | bank_transactions.unique_hash 防止重复导入 |
| 唯一约束 | projects.project_code 项目编号唯一 |
| 唯一约束 | users.username 用户名唯一 |
| 外键约束 | 所有 FK 字段均建立外键关系 |
| 软删除 | 核心业务表使用 is_deleted + deleted_at 软删除 |
| 自动更新 | updated_at 通过触发器自动更新 |
| 条件索引 | 大部分索引添加 WHERE is_deleted = false 条件 |
