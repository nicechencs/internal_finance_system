# 财务管理系统 - API 接口文档

## 全局约定

### 基础信息

| 项目 | 说明 |
|------|------|
| 基础路径 | `/api/v1` |
| 协议 | HTTPS |
| 数据格式 | JSON |
| 编码 | UTF-8 |
| 认证方式 | JWT Bearer Token |

### 统一请求头

```
Content-Type: application/json
Authorization: Bearer {token}
```

### 统一响应格式

```json
{
  "code": 200,
  "message": "success",
  "data": {},
  "timestamp": "2026-03-12T10:00:00Z"
}
```

### 错误响应格式

```json
{
  "code": 400,
  "message": "参数错误",
  "errors": [
    { "field": "name", "message": "名称不能为空" }
  ],
  "timestamp": "2026-03-12T10:00:00Z"
}
```

### 错误码定义

| 错误码 | 说明 |
|--------|------|
| 200 | 成功 |
| 400 | 参数错误 |
| 401 | 未认证 |
| 403 | 无权限 |
| 404 | 资源不存在 |
| 409 | 数据冲突（如重复导入） |
| 500 | 服务器内部错误 |

### 分页参数

| 参数 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| page | int | 1 | 页码 |
| pageSize | int | 20 | 每页条数（最大100） |
| sortBy | string | id | 排序字段 |
| sortOrder | string | desc | 排序方向：asc / desc |

### 分页响应

```json
{
  "code": 200,
  "data": {
    "items": [],
    "total": 100,
    "page": 1,
    "pageSize": 20,
    "totalPages": 5
  }
}
```

---

## 1. 认证模块 `/api/v1/auth`

### 1.1 登录

```
POST /api/v1/auth/login
```

请求体：
```json
{
  "username": "admin",
  "password": "admin123"
}
```

响应：
```json
{
  "code": 200,
  "data": {
    "token": "eyJhbGciOiJIUzI1NiIs...",
    "expiresIn": 86400,
    "user": {
      "id": 1,
      "username": "admin",
      "fullName": "系统管理员",
      "role": "admin"
    }
  }
}
```

### 1.2 获取当前用户信息

```
GET /api/v1/auth/me
```

### 1.3 修改密码

```
PUT /api/v1/auth/password
```

请求体：
```json
{
  "oldPassword": "admin123",
  "newPassword": "newPass456"
}
```

---

## 2. 账户管理 `/api/v1/accounts`

### 2.1 获取账户列表

```
GET /api/v1/accounts
```

查询参数：

| 参数 | 类型 | 必填 | 说明 |
|------|------|------|------|
| accountType | string | 否 | bank / alipay |
| isActive | bool | 否 | 是否启用 |
| keyword | string | 否 | 搜索关键词（名称/账号） |

响应 data：
```json
[
  {
    "id": 1,
    "name": "招商银行主账户",
    "accountType": "bank",
    "accountNumber": "6225****1234",
    "bankName": "招商银行",
    "openingBalance": 100000.00,
    "currentBalance": 258000.50,
    "currency": "CNY",
    "isActive": true,
    "createdAt": "2026-01-01T00:00:00Z"
  }
]
```

### 2.2 创建账户

```
POST /api/v1/accounts
```

请求体：
```json
{
  "name": "招商银行主账户",
  "accountType": "bank",
  "accountNumber": "6225880112341234",
  "bankName": "招商银行",
  "openingBalance": 100000.00,
  "currency": "CNY",
  "description": "公司主要对公账户"
}
```

### 2.3 获取账户详情

```
GET /api/v1/accounts/{id}
```

### 2.4 更新账户

```
PUT /api/v1/accounts/{id}
```

### 2.5 删除账户（软删除）

```
DELETE /api/v1/accounts/{id}
```

### 2.6 获取账户余额汇总

```
GET /api/v1/accounts/summary
```

响应 data：
```json
{
  "totalBalance": 500000.00,
  "accounts": [
    { "id": 1, "name": "招商银行主账户", "balance": 258000.50 },
    { "id": 2, "name": "支付宝账户", "balance": 242000.50 }
  ]
}
```

---

## 3. 分类管理 `/api/v1/categories`

### 3.1 获取分类树

```
GET /api/v1/categories/tree
```

查询参数：

| 参数 | 类型 | 必填 | 说明 |
|------|------|------|------|
| categoryType | string | 否 | income / expense |

响应 data：
```json
[
  {
    "id": 1,
    "name": "项目收入",
    "categoryType": "income",
    "level": 1,
    "children": [
      { "id": 11, "name": "软件开发收入", "categoryType": "income", "level": 2, "children": [] }
    ]
  }
]
```

### 3.2 创建分类

```
POST /api/v1/categories
```

请求体：
```json
{
  "name": "软件开发收入",
  "parentId": 1,
  "categoryType": "income",
  "description": ""
}
```

### 3.3 更新分类

```
PUT /api/v1/categories/{id}
```

### 3.4 删除分类

```
DELETE /api/v1/categories/{id}
```

---

## 4. 客户管理 `/api/v1/customers`

### 4.1 获取客户列表

```
GET /api/v1/customers?keyword=&isActive=true&page=1&pageSize=20
```

### 4.2 创建客户

```
POST /api/v1/customers
```

请求体：
```json
{
  "name": "XX科技有限公司",
  "shortName": "XX科技",
  "contactPerson": "[name]",
  "contactPhone": "[phone_number]",
  "contactEmail": "[email]",
  "address": "[address]",
  "taxNumber": "91110000XXXXXXXX",
  "description": ""
}
```

### 4.3 获取客户详情

```
GET /api/v1/customers/{id}
```

响应 data 包含关联信息：
```json
{
  "id": 1,
  "name": "XX科技有限公司",
  "shortName": "XX科技",
  "contactPerson": "[name]",
  "projects": [
    { "id": 1, "name": "OA系统开发", "contractAmount": 500000 }
  ],
  "totalReceivable": 150000.00,
  "totalReceived": 350000.00
}
```

### 4.4 更新客户

```
PUT /api/v1/customers/{id}
```

### 4.5 删除客户

```
DELETE /api/v1/customers/{id}
```

---

## 5. 供应商管理 `/api/v1/suppliers`

### 5.1 获取供应商列表

```
GET /api/v1/suppliers?keyword=&isActive=true&page=1&pageSize=20
```

### 5.2 创建供应商

```
POST /api/v1/suppliers
```

请求体：
```json
{
  "name": "XX外包服务公司",
  "shortName": "XX外包",
  "contactPerson": "[name]",
  "contactPhone": "[phone_number]",
  "contactEmail": "[email]",
  "address": "[address]",
  "taxNumber": "91110000XXXXXXXX",
  "bankAccount": "6225880112345678",
  "bankName": "工商银行",
  "description": ""
}
```

### 5.3 获取供应商详情

```
GET /api/v1/suppliers/{id}
```

响应 data 包含关联信息：
```json
{
  "id": 1,
  "name": "XX外包服务公司",
  "totalPayable": 80000.00,
  "totalPaid": 120000.00,
  "recentTransactions": []
}
```

### 5.4 更新供应商

```
PUT /api/v1/suppliers/{id}
```

### 5.5 删除供应商

```
DELETE /api/v1/suppliers/{id}
```

---

## 6. 人员管理 `/api/v1/persons`

### 6.1 获取人员列表

```
GET /api/v1/persons?personType=employee&isActive=true&page=1&pageSize=20
```

### 6.2 创建人员

```
POST /api/v1/persons
```

请求体：
```json
{
  "name": "[name]",
  "personType": "employee",
  "idNumber": "[id_number]",
  "phone": "[phone_number]",
  "email": "[email]",
  "bankAccount": "6225880112345678",
  "bankName": "招商银行",
  "joinDate": "2025-01-01"
}
```

### 6.3 获取人员详情

```
GET /api/v1/persons/{id}
```

### 6.4 更新人员

```
PUT /api/v1/persons/{id}
```

### 6.5 删除人员

```
DELETE /api/v1/persons/{id}
```

### 6.6 获取人员成本汇总

```
GET /api/v1/persons/{id}/cost-summary?startDate=2026-01-01&endDate=2026-12-31
```

响应 data：
```json
{
  "personId": 1,
  "personName": "[name]",
  "salary": 120000.00,
  "commission": 15000.00,
  "reimbursement": 5000.00,
  "dividend": 0,
  "totalCost": 140000.00
}
```

---

## 7. 项目管理 `/api/v1/projects`

### 7.1 获取项目列表

```
GET /api/v1/projects?status=active&customerId=1&keyword=&page=1&pageSize=20
```

### 7.2 创建项目

```
POST /api/v1/projects
```

请求体：
```json
{
  "name": "OA系统开发项目",
  "projectCode": "PRJ-2026-001",
  "customerId": 1,
  "contractAmount": 500000.00,
  "startDate": "2026-01-15",
  "endDate": "2026-06-30",
  "description": "XX科技OA系统定制开发"
}
```

### 7.3 获取项目详情

```
GET /api/v1/projects/{id}
```

响应 data：
```json
{
  "id": 1,
  "name": "OA系统开发项目",
  "projectCode": "PRJ-2026-001",
  "customer": { "id": 1, "name": "XX科技有限公司" },
  "contractAmount": 500000.00,
  "receivedAmount": 350000.00,
  "receivableAmount": 150000.00,
  "totalCost": 180000.00,
  "profitAmount": 170000.00,
  "profitRate": 48.57,
  "status": "active",
  "costBreakdown": [
    { "categoryName": "开发成本", "amount": 120000.00 },
    { "categoryName": "运维成本", "amount": 30000.00 },
    { "categoryName": "售前成本", "amount": 30000.00 }
  ]
}
```

### 7.4 更新项目

```
PUT /api/v1/projects/{id}
```

### 7.5 删除项目

```
DELETE /api/v1/projects/{id}
```

### 7.6 获取项目利润报表

```
GET /api/v1/projects/profit-report?startDate=2026-01-01&endDate=2026-12-31
```

---

## 8. Excel 导入 `/api/v1/import`

### 8.1 上传 Excel 文件

```
POST /api/v1/import/upload
Content-Type: multipart/form-data
```

表单参数：

| 参数 | 类型 | 必填 | 说明 |
|------|------|------|------|
| file | File | 是 | Excel 文件（仅支持 .xlsx 格式） |
| accountId | long | 是 | 目标账户 ID |

注意：不支持 .xls 格式（Excel 97-2003），请先转换为 .xlsx 格式。

响应 data：
```json
{
  "batchId": 1,
  "fileName": "招商银行202603.xlsx",
  "previewRows": [
    {
      "rowIndex": 1,
      "transactionDate": "2026-03-01",
      "amount": 50000.00,
      "direction": "in",
      "counterparty": "XX科技有限公司",
      "memo": "OA系统开发款",
      "isDuplicate": false,
      "suggestedCategory": "项目收入",
      "suggestedProject": "OA系统开发项目"
    }
  ],
  "totalRows": 50,
  "duplicateRows": 3
}
```

### 8.2 确认导入

```
POST /api/v1/import/{batchId}/confirm
```

请求体：
```json
{
  "skipDuplicates": true,
  "rowOverrides": [
    {
      "rowIndex": 1,
      "categoryId": 1,
      "projectId": 1,
      "customerId": 1
    }
  ]
}
```

响应 data：
```json
{
  "batchId": 1,
  "successCount": 47,
  "duplicateCount": 3,
  "errorCount": 0
}
```

### 8.3 获取导入历史

```
GET /api/v1/import/history?accountId=1&page=1&pageSize=20
```

### 8.4 获取批次详情

```
GET /api/v1/import/{batchId}
```

### 8.5 撤销导入

```
DELETE /api/v1/import/{batchId}
```

---

## 9. 交易管理 `/api/v1/transactions`

### 9.1 获取交易列表

```
GET /api/v1/transactions
```

查询参数：

| 参数 | 类型 | 必填 | 说明 |
|------|------|------|------|
| startDate | date | 否 | 开始日期 |
| endDate | date | 否 | 结束日期 |
| transactionType | string | 否 | income / expense |
| categoryId | long | 否 | 分类 ID |
| accountId | long | 否 | 账户 ID |
| projectId | long | 否 | 项目 ID |
| customerId | long | 否 | 客户 ID |
| supplierId | long | 否 | 供应商 ID |
| personId | long | 否 | 人员 ID |
| keyword | string | 否 | 搜索关键词 |
| status | string | 否 | pending / confirmed / cancelled |
| minAmount | decimal | 否 | 最小金额 |
| maxAmount | decimal | 否 | 最大金额 |

### 9.2 创建交易（手动录入）

```
POST /api/v1/transactions
```

请求体：
```json
{
  "transactionDate": "2026-03-10",
  "amount": 50000.00,
  "transactionType": "income",
  "categoryId": 1,
  "accountId": 1,
  "projectId": 1,
  "customerId": 1,
  "description": "OA系统第二期款项"
}
```

### 9.3 获取交易详情

```
GET /api/v1/transactions/{id}
```

### 9.4 更新交易

```
PUT /api/v1/transactions/{id}
```

### 9.5 删除交易

```
DELETE /api/v1/transactions/{id}
```

### 9.6 批量更新分类

```
PUT /api/v1/transactions/batch-categorize
```

请求体：
```json
{
  "transactionIds": [1, 2, 3],
  "categoryId": 5,
  "projectId": 1
}
```

### 9.7 交易分摊

```
POST /api/v1/transactions/{id}/allocate
```

请求体：
```json
{
  "allocations": [
    { "projectId": 1, "amount": 3000.00, "allocationRate": 50.00 },
    { "projectId": 2, "amount": 3000.00, "allocationRate": 50.00 }
  ]
}
```

说明：每条分摊记录可指定 `projectId`（按项目分摊）或 `personId`（按人员分摊，如个税/社保），至少有一个不为空。

按人员分摊示例：
```json
{
  "allocations": [
    { "personId": 1, "amount": 800.00, "note": "张三个税" },
    { "personId": 2, "amount": 1200.00, "note": "李四个税" }
  ]
}
```

校验规则：
- `SUM(allocations.amount)` 必须等于该交易的 `amount`
- 每个 `allocation.amount` 必须大于 0
- 同一分摊目标不能重复（`projectId` 或 `personId`）
- 每条分摊记录的 `projectId` 和 `personId` 至少有一个不为空
- 分摊比例 `allocationRate` 为可选字段，仅用于记录，不参与金额校验

响应 data：
```json
{
  "transactionId": 1,
  "totalAmount": 6000.00,
  "allocations": [
    { "id": 1, "projectId": 1, "projectName": "OA系统开发", "amount": 3000.00, "allocationRate": 50.00 },
    { "id": 2, "projectId": 2, "projectName": "CRM系统开发", "amount": 3000.00, "allocationRate": 50.00 }
  ]
}
```

说明：响应中 `projectId`/`projectName` 和 `personId`/`personName` 按实际分摊目标返回。
```

---

## 10. 应收管理 `/api/v1/receivables`

### 10.1 获取应收列表

```
GET /api/v1/receivables?customerId=&projectId=&status=pending&page=1&pageSize=20
```

### 10.2 创建应收

```
POST /api/v1/receivables
```

请求体：
```json
{
  "projectId": 1,
  "customerId": 1,
  "totalAmount": 150000.00,
  "dueDate": "2026-06-30",
  "description": "OA系统尾款"
}
```

### 10.3 获取应收详情

```
GET /api/v1/receivables/{id}
```

### 10.4 登记收款

```
POST /api/v1/receivables/{id}/receive
```

请求体：
```json
{
  "transactionId": 100,
  "paymentDate": "2026-03-15",
  "amount": 50000.00,
  "paymentMethod": "银行转账",
  "description": "第一笔回款"
}
```

### 10.5 应收汇总

```
GET /api/v1/receivables/summary
```

**说明**：
- `overdueAmount` 和 `overdueCount` 是查询时动态计算的结果
- 逾期判定规则：`due_date < CURRENT_DATE AND status IN ('pending', 'partial')`
- 数据库中不存储 `overdue` 状态

响应 data：
```json
{
  "totalReceivable": 500000.00,
  "overdueAmount": 80000.00,
  "overdueCount": 2,
  "byCustomer": [
    { "customerId": 1, "customerName": "XX科技", "amount": 150000.00 }
  ]
}
```

---

## 11. 应付管理 `/api/v1/payables`

### 11.1 获取应付列表

```
GET /api/v1/payables?supplierId=&projectId=&status=pending&page=1&pageSize=20
```

### 11.2 创建应付

```
POST /api/v1/payables
```

请求体：
```json
{
  "supplierId": 1,
  "projectId": 1,
  "totalAmount": 80000.00,
  "dueDate": "2026-04-30",
  "description": "外包开发费用"
}
```

### 11.3 获取应付详情

```
GET /api/v1/payables/{id}
```

### 11.4 登记付款

```
POST /api/v1/payables/{id}/pay
```

请求体：
```json
{
  "transactionId": 101,
  "paymentDate": "2026-03-20",
  "amount": 40000.00,
  "paymentMethod": "银行转账",
  "description": "第一笔付款"
}
```

### 11.5 应付汇总

```
GET /api/v1/payables/summary
```

---

## 12. 分类规则 `/api/v1/rules`

### 12.1 获取规则列表

```
GET /api/v1/rules?isActive=true
```

### 12.2 创���规则

```
POST /api/v1/rules
```

请求体：
```json
{
  "ruleName": "XX科技收款自动分类",
  "priority": 10,
  "matchField": "counterparty",
  "matchOperator": "contains",
  "matchValue": "XX科技",
  "categoryId": 1,
  "projectId": 1,
  "customerId": 1
}
```

### 12.3 更新规则

```
PUT /api/v1/rules/{id}
```

### 12.4 删除规则

```
DELETE /api/v1/rules/{id}
```

### 12.5 测试规则匹配

```
POST /api/v1/rules/test
```

请求体：
```json
{
  "counterparty": "XX科技有限公司",
  "memo": "OA系统开发款",
  "amount": 50000.00
}
```

响应 data：
```json
{
  "matchedRule": {
    "id": 1,
    "ruleName": "XX科技收款自动分类"
  },
  "suggestedCategory": { "id": 1, "name": "项目收入" },
  "suggestedProject": { "id": 1, "name": "OA系统开发项目" },
  "suggestedCustomer": { "id": 1, "name": "XX科技有限公司" }
}
```

---

## 13. 报表系统 `/api/v1/reports`

### 13.1 月度利润报表

```
GET /api/v1/reports/monthly-profit?year=2026&month=3
```

响应 data：
```json
{
  "year": 2026,
  "month": 3,
  "totalIncome": 500000.00,
  "totalExpense": 320000.00,
  "netProfit": 180000.00,
  "profitRate": 36.00,
  "incomeByCategory": [
    { "categoryName": "项目收入", "amount": 480000.00 },
    { "categoryName": "利息收入", "amount": 20000.00 }
  ],
  "expenseByCategory": [
    { "categoryName": "开发成本", "amount": 200000.00 },
    { "categoryName": "运维成本", "amount": 50000.00 },
    { "categoryName": "行政成本", "amount": 70000.00 }
  ]
}
```

### 13.2 现金流报表

```
GET /api/v1/reports/cashflow?startDate=2026-01-01&endDate=2026-03-31&accountId=
```

响应 data：
```json
{
  "startDate": "2026-01-01",
  "endDate": "2026-03-31",
  "openingBalance": 100000.00,
  "totalIncome": 1500000.00,
  "totalExpense": 960000.00,
  "closingBalance": 640000.00,
  "monthlyDetail": [
    {
      "month": "2026-01",
      "openingBalance": 100000.00,
      "income": 500000.00,
      "expense": 320000.00,
      "closingBalance": 280000.00
    }
  ]
}
```

### 13.3 项目利润报表

```
GET /api/v1/reports/project-profit?startDate=2026-01-01&endDate=2026-12-31&projectId=
```

响应 data：
```json
{
  "projects": [
    {
      "projectId": 1,
      "projectName": "OA系统开发项目",
      "customerName": "XX科技有限公司",
      "contractAmount": 500000.00,
      "receivedAmount": 350000.00,
      "totalCost": 180000.00,
      "profitAmount": 170000.00,
      "profitRate": 48.57
    }
  ],
  "summary": {
    "totalContract": 1200000.00,
    "totalReceived": 800000.00,
    "totalCost": 450000.00,
    "totalProfit": 350000.00,
    "avgProfitRate": 43.75
  }
}
```

### 13.4 人员成本分析

```
GET /api/v1/reports/person-cost?startDate=2026-01-01&endDate=2026-12-31&personType=employee
```

响应 data：
```json
{
  "persons": [
    {
      "personId": 1,
      "personName": "[name]",
      "personType": "employee",
      "salary": 120000.00,
      "commission": 15000.00,
      "reimbursement": 5000.00,
      "dividend": 0,
      "totalCost": 140000.00
    }
  ],
  "summary": {
    "totalSalary": 600000.00,
    "totalCommission": 50000.00,
    "totalReimbursement": 20000.00,
    "totalCost": 670000.00
  }
}
```

### 13.5 供应商支出统计

```
GET /api/v1/reports/supplier-expense?startDate=2026-01-01&endDate=2026-12-31
```

响应 data：
```json
{
  "suppliers": [
    {
      "supplierId": 1,
      "supplierName": "XX外包服务公司",
      "totalExpense": 200000.00,
      "transactionCount": 5,
      "rank": 1
    }
  ],
  "summary": {
    "totalExpense": 500000.00,
    "supplierCount": 8
  }
}
```

### 13.6 年度经营概览

```
GET /api/v1/reports/annual-overview?year=2026
```

响应 data：
```json
{
  "year": 2026,
  "totalIncome": 3000000.00,
  "totalExpense": 1800000.00,
  "netProfit": 1200000.00,
  "profitRate": 40.00,
  "totalReceivable": 500000.00,
  "totalPayable": 200000.00,
  "monthlyTrend": [
    { "month": 1, "income": 250000, "expense": 150000, "profit": 100000 }
  ],
  "topProjects": [],
  "topCustomers": [],
  "topSuppliers": []
}
```

---

## 14. 仪表盘 `/api/v1/dashboard`

### 14.1 获取仪表盘数据

```
GET /api/v1/dashboard
```

响应 data：
```json
{
  "totalBalance": 640000.00,
  "monthIncome": 500000.00,
  "monthExpense": 320000.00,
  "monthProfit": 180000.00,
  "totalReceivable": 500000.00,
  "totalPayable": 200000.00,
  "activeProjects": 5,
  "recentTransactions": [],
  "overdueReceivables": [],
  "overduePayables": [],
  "incomeExpenseTrend": []
}
```

**说明**：
- `overdueReceivables` 和 `overduePayables` 是查询时动态计算的结果
- 逾期判定规则：`due_date < CURRENT_DATE AND status IN ('pending', 'partial')`

---

## 15. 审计日志 `/api/v1/audit-logs`

### 15.1 获取操作日志

```
GET /api/v1/audit-logs?entityType=transaction&action=create&startDate=&endDate=&page=1&pageSize=20
```

---

## 16. 系统配置 `/api/v1/configs`

### 16.1 获取所有配置

```
GET /api/v1/configs
```

### 16.2 更新配置

```
PUT /api/v1/configs/{key}
```

请求体：
```json
{
  "configValue": "新值"
}
```
