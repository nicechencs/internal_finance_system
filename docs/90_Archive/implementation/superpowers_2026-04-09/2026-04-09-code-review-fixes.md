# Code Review Fixes Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Fix 9 Critical + 19 high-impact Important issues found in full-stack code review, organized into 3 phases with maximum parallelism.

**Architecture:** Phase 1 fixes Critical bugs in isolated files (6 parallel agents). Phase 2 unifies cross-cutting concerns (format functions, shared CSS). Phase 3 improves code reuse (composable adoption, backend dedup).

**Tech Stack:** Vue 3 + TypeScript + Element Plus (frontend), ASP.NET Core (backend)

---

## Phase 1: Critical Fixes (6 parallel tasks, no file conflicts)

### Task 1: Dashboard Cleanup

**Files:**
- Modify: `frontend/src/features/dashboard/pages/DashboardPage.vue`

- [ ] **Step 1: Delete dead code block (lines 90-124)**

Remove the entire `legacy-secondary-stats` el-row block:

```html
<!-- DELETE from line 90 to line 124 -->
```

- [ ] **Step 2: Delete dead CSS (lines 590-637)**

Remove `.legacy-secondary-stats`, `.mini-stat-card`, `.mini-stat-card:hover`, `.mini-stat-icon`, `.mini-stat-info`, `.mini-stat-value`, `.mini-stat-label` style rules.

- [ ] **Step 3: Extract pie chart factory function**

Replace the duplicate `expensePieOption` (lines 401-451) and `incomePieOption` (lines 453-503) with:

```typescript
const createPieOption = (data: { categoryName: string; amount: number }[]) => ({
  tooltip: {
    trigger: 'item',
    formatter: '{b}: {c} ({d}%)',
    backgroundColor: CHART_TOOLTIP.bg,
    borderColor: CHART_TOOLTIP.border,
    borderWidth: 1,
    textStyle: { color: CHART_TOOLTIP.text },
    extraCssText: `box-shadow: ${CHART_TOOLTIP.shadow}`,
    padding: [8, 12]
  },
  legend: {
    orient: 'vertical',
    right: '4%',
    top: 'center',
    type: 'scroll',
    textStyle: { color: CHART_AXIS.axisLabel, fontSize: 11 },
    itemWidth: 8, itemHeight: 8, icon: 'circle', itemGap: 10
  },
  color: chartColors.palette,
  series: [{
    type: 'pie',
    radius: ['42%', '72%'],
    center: ['32%', '50%'],
    avoidLabelOverlap: false,
    itemStyle: {
      borderRadius: 6,
      borderColor: 'var(--bg-card)',
      borderWidth: 2
    },
    label: { show: false },
    emphasis: { label: { show: true, fontSize: 13, fontWeight: '600' }, scaleSize: 6 },
    labelLine: { show: false },
    data: data.map(c => ({ name: c.categoryName, value: c.amount }))
  }]
})

const expensePieOption = computed(() => createPieOption(expenseByCategory.value))
const incomePieOption = computed(() => createPieOption(incomeByCategory.value))
```

Note: `borderColor` changed from `'#FFFFFF'` to `'var(--bg-card)'` (fixes I7).

- [ ] **Step 4: Add loading state and empty state**

Add `const dashboardLoading = ref(true)` and set it in `loadDashboardData`. Wrap main content with `v-loading="dashboardLoading"`. Add `<template #empty>` to the recent transactions table with Chinese text.

- [ ] **Step 5: Fix transaction type display**

Replace line 174's ternary with a type map:

```typescript
const transactionTypeMap: Record<string, string> = {
  'Income': '收入',
  'Expense': '支出',
  'Transfer': '转账'
}
// In template:
{{ transactionTypeMap[row.transactionType] || row.transactionType }}
```

---

### Task 2: Auth Pages Fixes

**Files:**
- Modify: `frontend/src/features/auth/pages/LoginPage.vue`
- Modify: `frontend/src/features/auth/pages/AccountProfilePage.vue`
- Modify: `frontend/src/features/auth/pages/AccountSecurityPage.vue`

- [ ] **Step 1: Fix validate pattern in LoginPage (C2)**

Replace lines 94-117:

```typescript
try {
  await formRef.value.validate()
} catch {
  return
}

loading.value = true
try {
  const response = await login(loginForm)
  userStore.setUser(response.data.data.user)
  userStore.markSessionInitialized()
  if (response.data.data.mustChangePassword) {
    ElMessage.success('登录成功，请先修改密码')
    router.push('/account-security')
  } else {
    ElMessage.success('登录成功')
    router.push('/')
  }
} catch (error) {
  console.error('登录失败:', error)
  ElMessage.error('登录失败，请检查用户名和密码')
} finally {
  loading.value = false
}
```

- [ ] **Step 2: Fix validate pattern in AccountProfilePage (C2 + I20)**

Replace lines 109-123:

```typescript
try {
  await formRef.value.validate()
} catch {
  return
}

saving.value = true
try {
  await updateProfile({
    fullName: form.fullName,
    email: form.email || undefined
  })
  await userStore.fetchCurrentUser()
  ElMessage.success('个人资料更新成功')
} catch (error) {
  console.error('更新失败:', error)
  ElMessage.error('个人资料更新失败')
} finally {
  saving.value = false
}
```

- [ ] **Step 3: Fix validate pattern in AccountSecurityPage (C2)**

Replace lines 127-143:

```typescript
try {
  await formRef.value.validate()
} catch {
  return
}

saving.value = true
try {
  await changePassword({
    currentPassword: form.currentPassword,
    newPassword: form.newPassword
  })
  ElMessage.success('密码修改成功')
  resetForm()
  formRef.value?.clearValidate()
} catch (error) {
  console.error('修改密码失败:', error)
} finally {
  saving.value = false
}
```

---

### Task 3: CustomerFormPage API Fix

**Files:**
- Modify: `frontend/src/features/master-data/customers/pages/CustomerFormPage.vue`

- [ ] **Step 1: Fix API response destructuring (C3)**

Replace lines 217-226:

```typescript
const { data } = await getCustomerById(id)
const customer = data.data
formData.name = customer.name
formData.shortName = customer.shortName || ''
formData.contactPerson = customer.contactPerson || ''
formData.contactPhone = customer.contactPhone || ''
formData.contactEmail = customer.contactEmail || ''
formData.address = customer.address || ''
formData.taxNumber = customer.taxNumber || ''
formData.description = customer.description || ''
formData.isActive = customer.isActive
```

---

### Task 4: PayableTypeManagementPage Fixes

**Files:**
- Modify: `frontend/src/features/finance/pages/PayableTypeManagementPage.vue`
- Modify: `frontend/src/features/finance/api/payable.ts`

- [ ] **Step 1: Add permission guards (C5)**

Add `import { useUserStore } from '@/stores/user'` and `const userStore = useUserStore()`.

Wrap "新增类型" button with `v-if="userStore.canEdit"`.
Wrap "编辑" button with `v-if="userStore.canEdit"`.
Wrap "删除" button with `v-if="userStore.canDelete"`.

- [ ] **Step 2: Fix API to use /active endpoint (C6)**

In `payable.ts`, the `getPayableTypes` calls GET `/payable-types` which hits CrudControllerBase's `GetPaged` (returns paginated result). The management page needs all types. Change to use the `/active` endpoint for the dropdown, and add a proper `getAllPayableTypes` for management:

```typescript
// For management page - use paged endpoint
export const getPayableTypesPaged = (params?: { page?: number; pageSize?: number }) =>
  request<ApiResponse<PageResponse<PayableType>>>({ url: '/payable-types', method: 'get', params: params || { page: 1, pageSize: 200 } })

// Keep existing for active dropdown
export const getPayableTypes = () =>
  request<ApiResponse<PayableType[]>>({ url: '/payable-types/active', method: 'get' })
```

Update `PayableTypeManagementPage.vue` `loadPayableTypes`:

```typescript
const response = await getPayableTypesPaged()
payableTypes.value = response.data.data.items
```

- [ ] **Step 3: Fix UI consistency (I3)**

Update CSS to match project standards:
- `.page-container { padding: 0 }` (not 20px)
- `.page-title { font-size: 20px }` (not 24px)
- `.page-desc { color: var(--text-placeholder) }` (not #909399)
- `.table-section { background: var(--bg-card); border-radius: 12px }` (not 4px)

---

### Task 5: Import + AuditLog Type Fixes

**Files:**
- Modify: `frontend/src/features/import/api/import.ts`
- Modify: `frontend/src/features/system/utils/auditLogHelpers.ts`

- [ ] **Step 1: Fix ImportBatchQuery type (C8)**

Add missing fields:

```typescript
export interface ImportBatchQuery extends PageRequest {
  accountId?: number
  status?: string
  fileName?: string
  startDate?: string
  endDate?: string
}
```

- [ ] **Step 2: Fix import statement position (C9)**

Move `import { formatDateTime } from '@/shared/utils/formatters'` from line 329 to line 1 of the file.

---

### Task 6: UnallocatedTransactionsPage Fixes

**Files:**
- Modify: `frontend/src/features/transactions/pages/UnallocatedTransactionsPage.vue`

Note: This page has `hidden: true` in its route meta so it's not in the menu, but is still accessible via URL. Many features are stubs (C7).

- [ ] **Step 1: Add development status banner**

Add an `el-alert` at the top of the page content:

```html
<el-alert
  title="此页面部分功能正在开发中"
  type="warning"
  :closable="false"
  show-icon
  style="margin-bottom: 16px"
/>
```

- [ ] **Step 2: Fix UI to use CSS variables (I2)**

Replace all hardcoded colors and inconsistent styles:
- `color: #666` → `color: var(--text-placeholder)`
- `background: white` → `background: var(--bg-card)`
- Remove Tailwind classes (`text-green-600`, `text-red-600`, `mt-4 flex justify-between`), replace with CSS variables and project `.pagination` class
- `el-card` wrapping table → `div.table-section`
- `padding: 24px` → `padding: 0` on `.page-container`
- `font-size: 24px` → `font-size: 20px` on `.page-title`

- [ ] **Step 3: Use shared formatDate**

Replace local `formatDate` with import from `@/shared/utils/formatters`:
```typescript
import { formatDateTime } from '@/shared/utils/formatters'
// In template: formatDateTime(row.transactionDate, 'date')
```

---

## Phase 2: Cross-Cutting Unification (3 tasks, after Phase 1)

### Task 7: Unify Amount/Date Formatting Across All Pages

**Files:**
- Modify: Multiple page files that use local format functions

Audit all pages for:
- Local `formatAmount` / `formatCurrency` / `formatDate` definitions → replace with shared `formatMoney` / `formatDateTime`
- Inconsistent `¥` / `CNY` prefixes → standardize to `formatRMB` for display with symbol
- Remove unused imports (`formatMoney`, `formatCurrency`, `computed` etc.)

Key files to fix:
- `AccountListPage.vue` - local `formatAmount` → `formatMoney`
- `AccountDetailPage.vue` - local `formatCurrency` → `formatMoney`
- `TransactionListPage.vue` - remove unused `formatMoney` import
- `ReceivableListPage.vue` - remove unused `formatCurrency` import
- `PayableListPage.vue` - remove unused `formatCurrency` import
- All 5 detail pages - unify transaction amount format to use `formatTransactionAmount`

### Task 8: Fix useListPage composable + handleSizeChange

**Files:**
- Modify: `frontend/src/shared/composables/useListPage.ts`

Fix `handleSizeChange` to reset page to 1:

```typescript
const handleSizeChange = () => {
  pagination.page = 1
  loadData()
}
```

This fixes the issue for ALL pages using the composable (S29).

### Task 9: Shared Page CSS Extraction

**Files:**
- Create: `frontend/src/assets/page-layout.css`
- Modify: `frontend/src/assets/main.css` (import the new file)
- Modify: ~6 pages to remove duplicate CSS

Extract common `.page-container`, `.page-header`, `.page-title`, `.page-desc`, `.search-section`, `.table-section`, `.pagination` rules that are currently duplicated in RuleListPage, TagRuleListPage, AuditLogListPage, ImportPage, etc.

Note: `main.css` already has `.page-container` and `.page-header` styles (found in exploration). Verify what's already global vs what's duplicated in scoped styles, then remove the scoped duplicates.

---

## Phase 3: Code Reuse + Backend (3 tasks)

### Task 10: Adopt Shared Composables

- `AccountListPage.vue` → adopt `useListPageStatistics` + `useRouteFilters`
- `RuleListPage.vue` + `TagRuleListPage.vue` → adopt `useListPage`
- `CustomerDetailPage.vue` + `AccountDetailPage.vue` → adopt `useDetailPageStatistics`

### Task 11: Backend Consistency

- `ProjectsController.cs` → evaluate renaming to `ProjectController` (check API consumers first)
- `AuditLogController.cs` → add `{id:long}` route constraint
- Backend BatchImport in 4 controllers → extract shared `ImportHelper<T>` utility

### Task 12: UI Polish

- `UserManagementPage.vue` → adopt standard page layout (h2 20px, TABLE_COLUMN_WIDTH, resizable table)
- `TagAnalyticsPage.vue` → replace hardcoded colors with CSS variables
- `FixedDepositListPage.vue` → align with standard `.page-container` layout
- `SupplierListPage.vue` → add enable/disable toggle (parity with CustomerListPage)
