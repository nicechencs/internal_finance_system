# 账户交易汇总功能

## 功能概述

在账户详情页面增加了交易记录的收支汇总展示，让用户能够直观地查看该账户的资金流动情况。

## 实现内容

### 1. 后端 API

#### 新增接口

**GET** `/api/transactions/account/{accountId}/statistics`

获取指定账户的交易统计数据。

**权限**: Admin, Accountant, Viewer

**响应示例**:
```json
{
  "success": true,
  "data": {
    "totalIncome": 150000.00,
    "totalExpense": 80000.00,
    "netProfit": 70000.00,
    "totalTransfer": 20000.00,
    "incomeCount": 25,
    "expenseCount": 40,
    "transferCount": 5,
    "totalCount": 70
  }
}
```

#### 实现文件

- `ITransactionStatisticsService.cs` - 添加 `GetAccountStatisticsAsync` 方法
- `TransactionStatisticsService.cs` - 实现账户统计逻辑
- `ITransactionService.cs` - 添加委托方法
- `TransactionService.cs` - 委托到 StatisticsService
- `TransactionsController.cs` - 添加 API 端点

### 2. 前端实现

#### 新增组件

**TransactionSummaryChart.vue**

位置: `frontend/src/components/TransactionSummaryChart.vue`

功能:
- 显示总收入、总支出、净收益三个统计卡片
- 使用 ECharts 饼图展示收支分布
- 支持加载状态

Props:
- `statistics`: TransactionStatistics | null - 统计数据
- `loading`: boolean - 加载状态

#### API 调用

新增函数: `getAccountTransactionStatistics(accountId: number)`

位置: `frontend/src/api/transaction.ts`

#### 页面集成

**AccountDetail.vue**

在余额趋势图下方增加了收支汇总图表，页面加载时自动获取统计数据。

## 数据权限

- 遵循现有的数据权限控制机制
- Viewer 只能看到自己创建的交易统计
- Admin 和 Accountant 可以看到所有交易统计

## 视觉设计

### 统计卡片

- 总收入: 绿色渐变背景 (#10B981 → #059669)
- 总支出: 红色渐变背景 (#EF4444 → #DC2626)
- 净收益: 蓝色渐变背景 (#3B82F6 → #2563EB)

### 饼图

- 收入: 绿色 (#10B981)
- 支出: 红色 (#EF4444)
- 环形图样式，中心显示数值

## 测试建议

1. 访问账户详情页面，验证统计数据正确显示
2. 检查不同角色的权限控制
3. 验证空数据情况的处理
4. 测试加载状态的显示

## 相关文件

### 后端
- `backend/FinanceApp.Application/Interfaces/ITransactionStatisticsService.cs`
- `backend/FinanceApp.Application/Services/TransactionStatisticsService.cs`
- `backend/FinanceApp.Application/Interfaces/ITransactionService.cs`
- `backend/FinanceApp.Application/Services/TransactionService.cs`
- `backend/FinanceApp.Api/Controllers/TransactionsController.cs`

### 前端
- `frontend/src/components/TransactionSummaryChart.vue`
- `frontend/src/api/transaction.ts`
- `frontend/src/views/accounts/AccountDetail.vue`

## 更新日期

2026-03-15
