# 核心数据库设计

## 账户表

Table: accounts

字段：
- id
- name
- type（bank / alipay）
- account_number
- opening_balance
- current_balance
- created_at

## 银行流水表

Table: bank_transactions

字段：
- id
- account_id
- transaction_date
- amount
- direction
- counterparty
- memo
- import_batch_id

## 交易表（核心）

Table: transactions

字段：
- id
- date
- amount
- type（income / expense）
- category_id
- account_id
- project_id
- customer_id
- supplier_id
- person_id
- description
- status

## 分类表

Table: categories

字段：
- id
- name
- parent_id
- type

示例结构：

收入
- 项目收入
- 利息收入
- 理财收益

支出
- 开发成本
- 运维成本
- 行政成本
- 营销成本
- 售前成本