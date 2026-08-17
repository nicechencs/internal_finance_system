# 客户详情页面增强

## 概述

为客户详情页面添加了汇总数据和图表展示功能，参考账户管理和项目管理页面的设计模式。

## 实现内容

### 后端改动

1. **TransactionsController.cs**
   - 新增 `GetCustomerStatistics` 端点：`GET /api/transactions/customer/{customerId}/statistics`
   - 返回指定客户的交易统计数据

2. **ITransactionStatisticsService.cs**
   - 新增接口方法：`Task<TransactionStatisticsDto> GetCustomerStatisticsAsync(long customerId)`

3. **TransactionStatisticsService.cs**
   - 实现 `GetCustomerStatisticsAsync` 方法
   - 统计客户相关的收入、支出、净利润等数据
   - 应用数据权限过滤

### 前端改动

1. **transaction.ts API**
   - 新增 `getCustomerTransactionStatistics(customerId: number)` 方法
   - 调用后端统计 API

2. **CustomerDetail.vue**
   - 添加财务摘要卡片区域（4 个卡片）：
     - 总收入（绿色）
     - 总支出（红色）
     - 净利润（根据正负显示颜色）
     - 交易总数（蓝色）
   - 集成 `TransactionSummaryChart` 组件展示收支汇总图表
   - 页面加载时自动获取统计数据
   - 添加相应的样式定义

## 页面布局

```
客户详情页面
├── 页面头部（返回按钮 + 标题）
├── 财务摘要卡片（4 列响应式布局）
│   ├── 总收入
│   ├── 总支出
│   ├── 净利润
│   └── 交易总数
├── 基本信息卡片（客户详细信息）
├── 收支汇总图表（TransactionSummaryChart）
└── 交易记录 Tab
    └── 交易列表表格
```

## API 端点

### GET /api/transactions/customer/{customerId}/statistics

获取指定客户的交易统计数据。

**权限**: Admin, Accountant, Viewer

**响应示例**:
```json
{
  "success": true,
  "data": {
    "totalIncome": 150000.00,
    "totalExpense": 50000.00,
    "netProfit": 100000.00,
    "totalTransfer": 0.00,
    "incomeCount": 15,
    "expenseCount": 8,
    "transferCount": 0,
    "totalCount": 23
  }
}
```

## 设计模式

参考了以下页面的设计：
- **账户详情页面**: 汇总卡片 + 余额趋势图 + 收支汇总图
- **项目详情页面**: 财务摘要卡片 + 利润分析图表

保持了统一的视觉风格和交互模式。

## 测试

- 前端编译通过：`npm run build`
- 后端编译通过：`dotnet build`
- 单元测试已存在于 `TransactionStatisticsServiceTests.cs`

## 相关文件

### 后端
- `backend/FinanceApp.Api/Controllers/TransactionsController.cs`
- `backend/FinanceApp.Application/Interfaces/ITransactionStatisticsService.cs`
- `backend/FinanceApp.Application/Services/TransactionStatisticsService.cs`

### 前端
- `frontend/src/api/transaction.ts`
- `frontend/src/views/customers/CustomerDetail.vue`
- `frontend/src/components/TransactionSummaryChart.vue` (复用)

## 日期

2026-03-15
