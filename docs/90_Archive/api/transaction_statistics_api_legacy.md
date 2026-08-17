# 交易统计 API 文档

## 概述

本文档描述交易统计相关的 API 端点，用于获取全局、账户级别和客户级别的交易统计数据。

## API 端点

### 1. 获取全局交易统计

获取系统中所有交易的统计数据。

**端点**: `GET /api/transactions/statistics`

**权限**: Admin, Accountant, Viewer

**请求参数**: 无

**响应格式**:

```json
{
  "success": true,
  "message": "操作成功",
  "data": {
    "totalIncome": 1500000.00,
    "totalExpense": 800000.00,
    "netProfit": 700000.00,
    "totalTransfer": 200000.00,
    "incomeCount": 45,
    "expenseCount": 120,
    "transferCount": 15,
    "totalCount": 180
  }
}
```

**使用示例**:

```typescript
import { getTransactionStatistics } from '@/api/transaction'

// 获取全局统计数据
const fetchGlobalStats = async () => {
  try {
    const response = await getTransactionStatistics()
    if (response.data.success) {
      const stats = response.data.data
      console.log(`总收入: ¥${stats.totalIncome.toLocaleString()}`)
      console.log(`总支出: ¥${stats.totalExpense.toLocaleString()}`)
      console.log(`净利润: ¥${stats.netProfit.toLocaleString()}`)
    }
  } catch (error) {
    console.error('获取统计数据失败:', error)
  }
}
```

---

### 2. 获取账户交易统计

获取指定账户的交易统计数据。

**端点**: `GET /api/transactions/account/{accountId}/statistics`

**权限**: Admin, Accountant, Viewer

**路径参数**:
- `accountId` (number, 必填): 账户 ID

**响应格式**:

```json
{
  "success": true,
  "message": "操作成功",
  "data": {
    "totalIncome": 500000.00,
    "totalExpense": 300000.00,
    "netProfit": 200000.00,
    "totalTransfer": 50000.00,
    "incomeCount": 15,
    "expenseCount": 40,
    "transferCount": 5,
    "totalCount": 60
  }
}
```

**使用示例**:

```typescript
import { getAccountTransactionStatistics } from '@/api/transaction'

// 获取账户统计数据
const fetchAccountStats = async (accountId: number) => {
  try {
    const response = await getAccountTransactionStatistics(accountId)
    if (response.data.success) {
      const stats = response.data.data
      console.log(`账户 ${accountId} 统计:`)
      console.log(`- 收入: ¥${stats.totalIncome.toLocaleString()} (${stats.incomeCount} 笔)`)
      console.log(`- 支出: ¥${stats.totalExpense.toLocaleString()} (${stats.expenseCount} 笔)`)
      console.log(`- 净额: ¥${stats.netProfit.toLocaleString()}`)
    }
  } catch (error) {
    console.error('获取账户统计数据失败:', error)
  }
}

// 在 Vue 组件中使用
const accountId = ref(1)
const statistics = ref<TransactionStatistics | null>(null)

const loadStatistics = async () => {
  const response = await getAccountTransactionStatistics(accountId.value)
  if (response.data.success) {
    statistics.value = response.data.data
  }
}
```

---

### 3. 获取客户交易统计

获取指定客户的交易统计数据。

**端点**: `GET /api/transactions/customer/{customerId}/statistics`

**权限**: Admin, Accountant, Viewer

**路径参数**:
- `customerId` (number, 必填): 客户 ID

**响应格式**:

```json
{
  "success": true,
  "message": "操作成功",
  "data": {
    "totalIncome": 800000.00,
    "totalExpense": 0.00,
    "netProfit": 800000.00,
    "totalTransfer": 0.00,
    "incomeCount": 25,
    "expenseCount": 0,
    "transferCount": 0,
    "totalCount": 25
  }
}
```

**使用示例**:

```typescript
import { getCustomerTransactionStatistics } from '@/api/transaction'

// 获取客户统计数据
const fetchCustomerStats = async (customerId: number) => {
  try {
    const response = await getCustomerTransactionStatistics(customerId)
    if (response.data.success) {
      const stats = response.data.data
      console.log(`客户 ${customerId} 交易统计:`)
      console.log(`- 总收入: ¥${stats.totalIncome.toLocaleString()}`)
      console.log(`- 交易次数: ${stats.totalCount} 笔`)
    }
  } catch (error) {
    console.error('获取客户统计数据失败:', error)
  }
}

// 在客户详情页中使用
import { ref, onMounted } from 'vue'
import type { TransactionStatistics } from '@/types/transaction'

const customerId = ref(1)
const customerStats = ref<TransactionStatistics>()

onMounted(async () => {
  const response = await getCustomerTransactionStatistics(customerId.value)
  if (response.data.success) {
    customerStats.value = response.data.data
  }
})
```

---

## 数据模型

### TransactionStatistics

交易统计数据类型定义（TypeScript）:

```typescript
export interface TransactionStatistics {
  totalIncome: number      // 总收入金额
  totalExpense: number     // 总支出金额
  netProfit: number        // 净利润（收入 - 支出）
  totalTransfer: number    // 转账总额
  incomeCount: number      // 收入交易数量
  expenseCount: number     // 支出交易数量
  transferCount: number    // 转账交易数量
  totalCount: number       // 总交易数量
}
```

后端 DTO 定义（C#）:

```csharp
public class TransactionStatisticsDto
{
    public decimal TotalIncome { get; set; }
    public decimal TotalExpense { get; set; }
    public decimal NetProfit { get; set; }
    public decimal TotalTransfer { get; set; }
    public int IncomeCount { get; set; }
    public int ExpenseCount { get; set; }
    public int TransferCount { get; set; }
    public int TotalCount { get; set; }
}
```

---

## 字段说明

| 字段名 | 类型 | 说明 |
|--------|------|------|
| totalIncome | number/decimal | 收入类型交易的总金额 |
| totalExpense | number/decimal | 支出类型交易的总金额 |
| netProfit | number/decimal | 净利润，计算公式: totalIncome - totalExpense |
| totalTransfer | number/decimal | 转账类型交易的总金额 |
| incomeCount | number/int | 收入类型交易的数量 |
| expenseCount | number/int | 支出类型交易的数量 |
| transferCount | number/int | 转账类型交易的数量 |
| totalCount | number/int | 所有交易的总数量 |

---

## 注意事项

1. **权限控制**: 所有统计 API 端点都需要身份认证，支持 Admin、Accountant 和 Viewer 角色访问。

2. **金额精度**:
   - 前端使用 `number` 类型（JavaScript）
   - 后端使用 `decimal` 类型（C#），精度为 18 位，小数点后 2 位
   - 显示时建议使用 `toLocaleString()` 格式化

3. **数据范围**:
   - 全局统计：包含系统中所有未删除的交易记录
   - 账户统计：仅包含指定账户的交易记录
   - 客户统计：仅包含关联到指定客户的交易记录（通常为收入类型）

4. **软删除**: 统计数据不包含已软删除（`is_deleted = true`）的交易记录。

5. **实时性**: 统计数据实时计算，反映当前数据库状态。

---

## 错误处理

所有 API 端点遵循统一的错误响应格式：

```json
{
  "success": false,
  "message": "错误描述信息",
  "data": null
}
```

常见错误场景：

- **401 Unauthorized**: 未登录或 Token 过期
- **403 Forbidden**: 权限不足
- **404 Not Found**: 账户或客户不存在
- **500 Internal Server Error**: 服务器内部错误

错误处理示例：

```typescript
import { ElMessage } from 'element-plus'

const fetchStatistics = async (accountId: number) => {
  try {
    const response = await getAccountTransactionStatistics(accountId)
    if (response.data.success) {
      return response.data.data
    } else {
      ElMessage.error(response.data.message || '获取统计数据失败')
    }
  } catch (error: any) {
    if (error.response?.status === 404) {
      ElMessage.error('账户不存在')
    } else if (error.response?.status === 403) {
      ElMessage.error('权限不足')
    } else {
      ElMessage.error('网络错误，请稍后重试')
    }
    console.error('API 错误:', error)
  }
}
```

---

## 相关文件

### 前端

- **API 封装**: `frontend/src/api/transaction.ts`
- **类型定义**: `frontend/src/types/transaction.ts`
- **使用示例**:
  - `frontend/src/views/account/AccountDetail.vue` (账户统计)
  - `frontend/src/views/customer/CustomerDetail.vue` (客户统计)
  - `frontend/src/views/dashboard/DashboardPage.vue` (全局统计)

### 后端

- **Controller**: `backend/FinanceApp.Api/Controllers/TransactionsController.cs`
- **Service**: `backend/FinanceApp.Application/Services/TransactionService.cs`
- **DTO**: `backend/FinanceApp.Application/DTOs/Transaction/TransactionStatisticsDto.cs`

---

## 更新日志

| 日期 | 版本 | 说明 |
|------|------|------|
| 2026-03-15 | 1.0 | 初始版本，包含全局、账户和客户统计 API |

