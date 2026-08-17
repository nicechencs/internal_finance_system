# 前端复杂度与页面间耦合清单

状态：Completed  
适用对象：开发 / 架构 / AI  
事实源级别：Secondary  
最后核对日期：2026-03-21

> 生成日期：2026-03-24
> 分析范围：`frontend/src/views/` + `frontend/src/components/`
> 共 37 个 views 文件 + 17 个 components 文件

---

## 1. 复杂度排名表（按行数排序）

| # | 文件 | 行数 | 模块归属 | 使用 composable | 手写分页 | 备注 |
|---|------|------|----------|-----------------|----------|------|
| 1 | `import/ImportPage.vue` | 1041 | reconciliation | useResizableTableColumns | ✅ 手写 | 最复杂页面，预览+确认+历史批次+规则重跑 |
| 2 | `Dashboard.vue` | 763 | reporting | useResizableTableColumns | — | ECharts x3 + StatCard + MaturityAlert |
| 3 | `finance/FinanceManagement.vue` | 757 | finance | useDebounce | — | **import 了 2 个 page**，ECharts x2 |
| 4 | `transactions/TransactionList.vue` | 702 | transactions | useResizableTableColumns | ✅ 手写 | import 6 个组件 + 3 个 store |
| 5 | `settings/UserManagement.vue` | 536 | auth | — | — | 内联 CRUD 逻辑 |
| 6 | `projects/ProjectDetail.vue` | 491 | master-data | useResizableTableColumns | — | ProfitAnalysisCharts + LinkDialog |
| 7 | `receivables/ReceivableList.vue` | 467 | finance | useResizableTableColumns | ✅ 手写 | 双模式（独立/嵌入） |
| 8 | `payables/PayableList.vue` | 467 | finance | useResizableTableColumns | ✅ 手写 | 双模式（独立/嵌入） |
| 9 | `accounts/AccountList.vue` | 461 | master-data | useResizableTableColumns | ✅ 手写 | 定期存款到期判断逻辑 |
| 10 | `customers/CustomerList.vue` | 451 | master-data | useResizableTableColumns | ✅ 手写 | 行内编辑 + ImportDialog + BatchLinkDialog |
| 11 | `projects/ProjectList.vue` | 438 | master-data | useResizableTableColumns | ✅ 手写 | ImportDialog + BatchLinkDialog |
| 12 | `persons/PersonList.vue` | 411 | master-data | useResizableTableColumns | ✅ 手写 | ImportDialog + BatchLinkDialog |
| 13 | `transactions/TransactionForm.vue` | 395 | transactions | — | — | 6 个 API + 分摊逻辑 |
| 14 | `suppliers/SupplierList.vue` | 394 | master-data | useResizableTableColumns | ✅ 手写 | ImportDialog + BatchLinkDialog |
| 15 | `accounts/AccountDetail.vue` | 383 | master-data | useResizableTableColumns | — | BalanceTrendChart + SummaryOverview |
| 16 | `audit-logs/AuditLogList.vue` | 358 | auth | useResizableTableColumns | ✅ 手写 | 纯查询页面 |
| 17 | `transactions/TransactionDetail.vue` | 353 | transactions | useResizableTableColumns | — | 关联应收应付展示 |
| 18 | `categories/CategoryList.vue` | 328 | master-data | **useListPage** ✅ | — | 唯一使用 useListPage 的页面 |
| 19 | `customers/CustomerDetail.vue` | 318 | master-data | useResizableTableColumns | — | LinkDialog + SummaryOverview |
| 20 | `Login.vue` | 293 | auth | — | — | 简单登录页 |
| 21 | `receivables/ReceivableDetail.vue` | 289 | finance | useResizableTableColumns | — | 收款登记表单 |
| 22 | `payables/PayableDetail.vue` | 289 | finance | useResizableTableColumns | — | 付款登记表单 |
| 23 | `rules/RuleList.vue` | 271 | reconciliation | useResizableTableColumns | ✅ 手写 | RuleRerunDialog |
| 24 | `accounts/AccountForm.vue` | 270 | master-data | — | — | SearchableInput |
| 25 | `persons/PersonDetail.vue` | 250 | master-data | useResizableTableColumns | — | LinkDialog + SummaryOverview |
| 26 | `suppliers/SupplierDetail.vue` | 241 | master-data | useResizableTableColumns | — | LinkDialog + SummaryOverview |
| 27 | `persons/PersonForm.vue` | 219 | master-data | — | — | SearchableInput |
| 28 | `rules/RuleForm.vue` | 207 | reconciliation | — | — | SearchableSelect |
| 29 | `suppliers/SupplierForm.vue` | 204 | master-data | — | — | SearchableInput |
| 30 | `customers/CustomerForm.vue` | 196 | master-data | — | — | SearchableInput |
| 31 | `receivables/ReceivableForm.vue` | 192 | finance | — | — | SearchableSelect |
| 32 | `settings/AccountSecurity.vue` | 191 | auth | — | — | 修改密码 |
| 33 | `payables/PayableForm.vue` | 191 | finance | — | — | SearchableSelect |
| 34 | `settings/AccountProfile.vue` | 174 | auth | — | — | 个人资料 |
| 35 | `categories/CategoryForm.vue` | 174 | master-data | — | — | 纯表单 |
| 36 | `projects/ProjectForm.vue` | 170 | master-data | — | — | SearchableInput |
| 37 | `dashboard/components/StatCards.vue` | 61 | reporting | — | — | 已废弃？Dashboard 不使用 |

---

## 2. Page import Page 关系图

```
┌──────────────────────────────────┐
│  finance/FinanceManagement.vue   │  (757 行)
│                                  │
│  import ReceivableList from      │──────▶ receivables/ReceivableList.vue  (467 行)
│    '@/views/receivables/...'     │
│                                  │
│  import PayableList from         │──────▶ payables/PayableList.vue        (467 行)
│    '@/views/payables/...'        │
└──────────────────────────────────┘
```

### 影响分析

- **FinanceManagement** 是唯一存在 page-import-page 耦合的文件
- 它将 `ReceivableList` 和 `PayableList` 作为嵌入组件使用（通过 `embedded` + `externalFilters` props 切换模式）
- **ReceivableList** / **PayableList** 同时作为独立路由页面和嵌入子组件，内部通过 `props.embedded` + `props.externalFilters` 判断运行模式
- **风险**：List 组件既承担独立页面职责，又承担嵌入子组件职责，双重职责增加维护复杂度

### 建议
- 考虑将 ReceivableList/PayableList 中的表格逻辑抽取为纯组件（`ReceivableTable.vue` / `PayableTable.vue`），独立页面和嵌入场景各自组合

---

## 3. 业务组件 → Feature 模块映射表

### 3.1 通用 UI 组件（无业务语义）

| 组件 | 行数 | 说明 |
|------|------|------|
| `StatCard.vue` | 189 | 通用统计卡片 |
| `SearchableSelect.vue` | 81 | 可搜索下拉选择 |
| `SearchableInput.vue` | 73 | 可搜索输入框（自动补全） |
| `SearchableFilterInput.vue` | 41 | 搜索型筛选输入（复合组件，内部引用 SearchableInput） |
| `DetailSummaryCards.vue` | 133 | 通用详情摘要卡片 |
| `SummaryOverview.vue` | 53 | 通用概览容器 |

### 3.2 业务组件（应归入对应 feature 模块）

| 组件 | 行数 | 当前位置 | 应归入模块 | 依赖的 API/类型 |
|------|------|----------|-----------|----------------|
| `TransferDialog.vue` | 236 | components/ | **transactions** | `api/transaction` (createTransfer) |
| `ConvertTransactionToTransferDialog.vue` | 297 | components/ | **transactions** | `api/transaction` (convertTransactionToTransfer) |
| `TransactionStatCards.vue` | 75 | components/ | **transactions** | `types/transaction` |
| `TransactionSummaryCards.vue` | 66 | components/ | **transactions** | `types/transaction`, DetailSummaryCards |
| `BatchLinkDialog.vue` | 258 | components/ | **reconciliation** | `api/link` (previewBatchLink) |
| `RuleRerunDialog.vue` | 245 | components/ | **reconciliation** | `api/link` (previewRuleRerun) |
| `LinkDialog.vue` | 197 | components/ | **reconciliation** | `api/link` (previewLink, confirmLink) |
| `ImportDialog.vue` | 394 | components/ | **reconciliation** | `api/import` (batchImportFile) |
| `MaturityAlert.vue` | 203 | components/ | **master-data** (accounts) | `api/account` (getMaturingAccounts) |
| `BalanceTrendChart.vue` | 254 | components/ | **master-data** (accounts) | `types/account` (BalanceTrendItem) |
| `ProfitAnalysisCharts.vue` | 401 | components/ | **master-data** (projects) | `types/project` (ProfitAnalysisResponse) |

### 3.3 弃用/冗余组件

| 组件 | 行数 | 说明 |
|------|------|------|
| `dashboard/components/StatCards.vue` | 61 | **疑似弃用**：Dashboard.vue 未 import 此文件，直接使用 StatCard 组件 |

---

## 4. Composable / Store 使用一致性分析

### 4.1 Composable 使用情况

| Composable | 使用页面 |
|------------|---------|
| `useListPage` | **仅 CategoryList.vue** 使用 ✅ |
| `useResizableTableColumns` | 几乎所有列表/详情页使用（25+ 页面） |
| `useDebounce` | FinanceManagement.vue |
| `useInlineEdit` | **0 个 views 文件直接使用**（可能被间接使用） |
| `useFormDialog` | **0 个 views 文件使用** |
| `useConfirm` | **0 个 views 文件使用** |
| `useCache` | **0 个 views 文件使用** |
| `useAuth` | **0 个 views 文件直接使用**（可能被 router guard 使用） |

#### 关键发现：`useListPage` 使用率极低

`useListPage` 封装了分页、加载、删除等通用逻辑，但 **37 个 views 中仅 1 个使用**。

**手写分页逻辑的页面（共 12 个）：**

| 页面 | 手写分页模式 |
|------|-------------|
| `TransactionList.vue` | reactive pagination + handleSizeChange/handlePageChange |
| `ReceivableList.vue` | 同上 |
| `PayableList.vue` | 同上 |
| `AccountList.vue` | 同上 |
| `CustomerList.vue` | 同上 |
| `SupplierList.vue` | 同上 |
| `PersonList.vue` | 同上 |
| `ProjectList.vue` | 同上 |
| `RuleList.vue` | 同上 |
| `AuditLogList.vue` | 同上 |
| `ImportPage.vue` | 同上 |
| `UserManagement.vue` | 前端过滤（无后端分页） |

**一致性问题**：12 个页面使用几乎相同的分页模板代码，但未统一使用 `useListPage`。每个页面独立维护 `pagination` reactive、`handleSizeChange`、`handlePageChange`，存在大量重复代码。

### 4.2 Store 使用情况

| Store | 使用页面 |
|-------|---------|
| `user` | Login, TransactionList, AccountList, CustomerList, SupplierList, PersonList, ProjectList, ReceivableList, PayableList, RuleList, AccountProfile |
| `account` | TransactionList |
| `category` | TransactionList, CategoryList |
| `project` | TransactionList, FinanceManagement |
| `customer` | FinanceManagement |
| `supplier` | FinanceManagement |

#### 关键发现：Store 使用不一致

- **TransactionList** 通过 Store 获取 account/category/project 下拉数据
- **ReceivableList / PayableList** 直接调用 API (`getProjects`, `getCustomers`, `getSuppliers`) 而不使用 Store
- **TransactionForm** 直接调用 `getActiveAccounts` 等 6 个 API，未使用任何 Store
- **FinanceManagement** 使用 project/customer/supplier 的 Store
- 同一类数据（如项目列表）在不同页面的获取方式不一致（Store vs 直接 API）

---

## 5. 高风险耦合点清单

### 🔴 高风险

| # | 风险项 | 涉及文件 | 说明 |
|---|--------|----------|------|
| 1 | **Page import Page** | FinanceManagement ← ReceivableList, PayableList | ReceivableList/PayableList 承担双重职责（独立页面 + 嵌入组件），修改任一方可能影响两种使用场景 |
| 2 | **分页逻辑碎片化** | 12 个 List 页面 | 相同的分页模板代码在 12 个页面中复制，`useListPage` 仅被 1 个页面使用，后续统一修改分页行为需要改 12 个地方 |
| 3 | **TransactionForm 扇出过高** | TransactionForm.vue | 直接调用 6 个 API 模块（account, category, project, customer, supplier, person），任何实体模块变更都可能影响此表单 |

### 🟡 中风险

| # | 风险项 | 涉及文件 | 说明 |
|---|--------|----------|------|
| 4 | **Store 与 API 混用** | ReceivableList, PayableList, TransactionForm vs TransactionList, FinanceManagement | 同类数据获取方式不统一（Store vs 直接 API），可能导致缓存不一致、重复请求 |
| 5 | **ImportPage 过度臃肿** | ImportPage.vue (1041 行) | 单文件承担上传预览 + 编辑 + 确认 + 历史批次管理 + 规则重跑等 5+ 个职责 |
| 6 | **formatCurrency 实现分散** | Dashboard, TransactionDetail, FinanceManagement, ReceivableForm 等 | `formatCurrency`/`formatMoney` 在多个页面内联定义，而非统一使用 `@/utils/formatters` |
| 7 | **组件放置位置不合理** | 11 个业务组件在 `components/` 下 | 业务组件（TransferDialog, BatchLinkDialog 等）放在全局 components 目录，但实际仅被特定模块使用 |
| 8 | **未使用的 Composable** | useFormDialog, useConfirm, useCache, useInlineEdit | 4 个 composable 在 views 层无直接使用，可能是残留代码或仅被测试使用 |

### 🟢 低风险 / 建议优化

| # | 优化项 | 涉及文件 | 说明 |
|---|--------|----------|------|
| 9 | **StatCards 疑似弃用** | dashboard/components/StatCards.vue | Dashboard.vue 不引用此文件，建议确认后移除 |
| 10 | **Detail 页面结构相似但未抽象** | CustomerDetail, SupplierDetail, PersonDetail | 三者结构几乎一致（Info + 交易列表 + LinkDialog），可考虑提取通用 EntityDetail 壳组件 |
| 11 | **getStatusType/getStatusText 重复** | ReceivableList, PayableList, TransactionDetail, ReceivableDetail, PayableDetail | 状态映射逻辑在 5 个文件中分别定义，可提取为共享 util |
| 12 | **排序逻辑重复** | TransactionList, ReceivableList, PayableList, AccountList, RuleList | `handleSortChange` + `sortState` 在 5+ 页面中重复定义 |

---

## 6. 各模块 Import 依赖全景

### auth 模块
```
Login.vue
  ← api/auth, stores/user

AccountSecurity.vue
  ← api/auth

AccountProfile.vue
  ← api/auth, stores/user

UserManagement.vue
  ← api/users, utils/formatters
```

### master-data 模块
```
accounts/AccountList.vue
  ← api/account, stores/user, components/{SearchableFilterInput, StatCard, BatchLinkDialog}
  ← ./AccountForm.vue

accounts/AccountDetail.vue
  ← api/{account, transaction}, components/{BalanceTrendChart, SummaryOverview, TransactionSummaryCards}

accounts/AccountForm.vue
  ← api/account, components/SearchableInput

categories/CategoryList.vue
  ← api/category, stores/{user, category}, composables/useListPage ✅
  ← ./CategoryForm.vue, components/StatCard

customers/CustomerList.vue
  ← api/customer, stores/user, components/{ImportDialog, StatCard, SearchableFilterInput, BatchLinkDialog}
  ← ./CustomerForm.vue

customers/CustomerDetail.vue
  ← api/{customer, transaction}, components/{LinkDialog, SummaryOverview, TransactionSummaryCards}

suppliers/SupplierList.vue
  ← api/supplier, stores/user, components/{ImportDialog, StatCard, SearchableFilterInput, BatchLinkDialog}
  ← ./SupplierForm.vue

suppliers/SupplierDetail.vue
  ← api/{supplier, transaction}, components/{LinkDialog, SummaryOverview, TransactionSummaryCards}

persons/PersonList.vue
  ← api/person, stores/user, components/{ImportDialog, StatCard, SearchableFilterInput, BatchLinkDialog}
  ← ./PersonForm.vue

persons/PersonDetail.vue
  ← api/{person, transaction}, components/{LinkDialog, SummaryOverview, TransactionSummaryCards}

projects/ProjectList.vue
  ← api/{project, customer}, stores/user, components/{ImportDialog, BatchLinkDialog, StatCard, SearchableFilterInput}
  ← ./ProjectForm.vue

projects/ProjectDetail.vue
  ← api/{project, transaction}, components/{LinkDialog, DetailSummaryCards, ProfitAnalysisCharts, SummaryOverview}
```

### transactions 模块
```
transactions/TransactionList.vue
  ← api/transaction, stores/{user, account, category, project}
  ← components/{SearchableSelect, StatCard, TransferDialog, ConvertTransactionToTransferDialog, BatchLinkDialog}
  ← ./TransactionForm.vue, ./TransactionDetail.vue

transactions/TransactionForm.vue
  ← api/{transaction, account, category, project, customer, supplier, person}
  ← components/SearchableSelect

transactions/TransactionDetail.vue
  ← api/transaction, composables/useResizableTableColumns
```

### reconciliation 模块
```
import/ImportPage.vue
  ← api/{import, account}, components/SearchableSelect

rules/RuleList.vue
  ← api/rule, stores/user, components/RuleRerunDialog
  ← ./RuleForm.vue

rules/RuleForm.vue
  ← api/{rule, category}, components/SearchableSelect
```

### finance 模块
```
finance/FinanceManagement.vue ⚠️ (page-import-page)
  ← api/{receivable, payable}, stores/{project, customer, supplier}
  ← components/{StatCard, SearchableSelect}
  ← views/receivables/ReceivableList.vue ⚠️
  ← views/payables/PayableList.vue ⚠️

receivables/ReceivableList.vue
  ← api/{receivable, project, customer}, stores/user, components/SearchableSelect
  ← ./ReceivableDetail.vue, ./ReceivableForm.vue

receivables/ReceivableForm.vue
  ← api/{receivable, project, customer}, components/SearchableSelect

payables/PayableList.vue
  ← api/{payable, project, supplier}, stores/user, components/SearchableSelect
  ← ./PayableDetail.vue, ./PayableForm.vue

payables/PayableForm.vue
  ← api/{payable, project, supplier}, components/SearchableSelect
```

### reporting 模块
```
Dashboard.vue
  ← api/dashboard, components/{MaturityAlert, StatCard}
  ← composables/useResizableTableColumns, constants/{table, colors}
```

---

## 7. 重构优先级建议

| 优先级 | 行动 | 影响面 | 工作量 |
|--------|------|--------|--------|
| P0 | 将 ReceivableList/PayableList 的表格部分抽取为独立组件，消除 page-import-page | 3 文件 | 中 |
| P1 | 推广 `useListPage` 到所有 12 个 List 页面，消除分页代码重复 | 12 文件 | 大 |
| P1 | 统一数据获取方式（Store vs 直接 API），制定约定 | 全局 | 中 |
| P2 | 将 11 个业务组件移入对应 feature 目录 | 11 + 引用方 | 小 |
| P2 | 拆分 ImportPage.vue（≥1000 行）为子组件 | 1 文件 | 中 |
| P3 | 提取 getStatusType/getStatusText 等重复工具函数 | 5 文件 | 小 |
| P3 | 清理弃用文件（dashboard/components/StatCards.vue）和未使用 composable | 5 文件 | 小 |
