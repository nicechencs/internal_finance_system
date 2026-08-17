# 问题修复总结

## 修复日期
2026-04-03

## 复核结论
以下结论已基于当前代码实际实现重新核对：

- 问题 1：已修复
- 问题 2：已修复，但实际实现方式与旧说明不同
- 问题 3：本次已完成修复
- 问题 4：已修复
- 问题 5：已修复
- 问题 6：已修复
- 问题 7：已修复

---

## 已完成修复

### ✅ 问题1：项目创建自动生成应收
**状态：已修复**

**实际实现：**
- 项目创建后自动生成默认应收记录
- 前端不再要求用户额外手工初始化收款计划

**影响范围：**
- 项目创建流程
- 默认应收记录生成

---

### ✅ 问题2：项目金额双数据源
**状态：已修复**

**复核说明：**
- 旧文档描述为“收款/付款时手工同步 Project 表字段”
- 实际代码并不是手工逐字段同步，而是在收款/付款后统一调用 `IProjectFinancialRecalculationService.RecalculateAsync(...)`
- 该实现会基于应收、应付、交易等数据重新汇总项目财务字段，效果上已经修复数据源不一致问题

**实际实现位置：**
- `ReceivableService.ReceivePaymentAsync(...)`
- `PayableService.PayPaymentAsync(...)`
- `ProjectFinancialRecalculationService`

**影响范围：**
- 项目详情页头部金额
- 项目统计
- 利润相关展示
- 收付款后的项目汇总数据

---

### ✅ 问题3：交易绑定强制规则
**状态：已修复**

**问题描述：**
- 过去 `ReceivableDetail.TransactionId` 和 `PayableDetail.TransactionId` 允许为空
- 会导致未绑定交易也能登记收款/付款，破坏可追溯性和分配状态一致性

**本次修复内容：**
1. **后端契约收紧**
   - `ReceivePaymentRequest.TransactionId` 改为必填
   - `PayPaymentRequest.TransactionId` 改为必填
   - `ReceivableDetailDto.TransactionId` 改为必填
   - `PayableDetailDto.TransactionId` 改为必填

2. **领域模型与校验收紧**
   - `ReceivableDetail.TransactionId` 改为非空
   - `PayableDetail.TransactionId` 改为非空
   - `SettlementTransactionBindingService` 不再按可空值处理
   - 当 `TransactionId <= 0` 时，直接抛出“必须关联交易记录”的校验异常

3. **服务链路修正**
   - `ReceivableService` / `PayableService` 改为按非空 `TransactionId` 更新交易分配状态
   - `TransactionService.GetLinkedFinanceTransactionIdsAsync()` 改为按非空字段直接查询

4. **前端契约与交互修正**
   - 应收/应付详情页请求类型中的 `transactionId` 改为必填
   - 页面表单默认使用 `0` 作为“未选择”哨兵值
   - 表单规则改为 `number` 且 `min: 1`
   - 应付款交易下拉移除 `clearable`
   - 交易下拉 API 返回值类型改为 `ApiResponse<Transaction[]>`，并统一返回归一化后的数组

5. **数据库迁移**
   - 新增迁移：`20260403123923_MakeSettlementTransactionIdRequired`
   - 将 `receivable_details.transaction_id` 与 `payable_details.transaction_id` 改为 `NOT NULL`
   - 迁移前增加保护检查：若仍存在未删除且 `transaction_id IS NULL` 的历史记录，迁移会直接失败并提示清理

**影响范围：**
- 应收收款登记
- 应付付款登记
- 交易与核销的关联关系
- 前端详情页交易选择交互
- 数据库约束一致性

---

### ✅ 问题4：交易下拉运行时错误
**状态：已修复**

**实际实现：**
- 交易下拉接口改为按 `ApiResponse<Transaction[]>` 读取 `response.data.data`
- 返回前统一执行 `normalizeTransactions(...)`

**影响范围：**
- 应收详情页交易下拉
- 应付详情页交易下拉

---

### ✅ 问题5：应付款匹配支持三类对方
**状态：已修复**

**实际实现：**
- 应付款可绑定交易接口支持供应商、客户、人员三类对方
- 绑定校验会校验项目与对方一致性

**影响范围：**
- 应付款创建和绑定流程
- 交易匹配准确性

---

### ✅ 问题6：待分配交易页面占位实现
**状态：已修复**

**实际实现：**
- 待分配交易入口已从菜单中隐藏，避免用户进入未完成页面

**影响范围：**
- 菜单可见性

---

### ✅ 问题7：收款计划初始化幂等性
**状态：已修复**

**实际实现：**
- 项目已有收款计划时，重复初始化会被显式拦截

**影响范围：**
- 收款计划初始化接口

---

## 测试验证

### 本次实际执行
```bash
dotnet test backend/tests/FinanceApp.Application.Tests/FinanceApp.Application.Tests.csproj --filter "ReceivePaymentAsync_WithZeroTransactionId_ShouldThrowValidationException|PayPaymentAsync_WithZeroTransactionId_ShouldThrowValidationException|ReceivePaymentAsync_WithValidRequest_ShouldUpdateReceivable|PayPaymentAsync_WithValidRequest_ShouldUpdatePayable"

dotnet test backend/tests/FinanceApp.Api.Tests/FinanceApp.Api.Tests.csproj --filter "ReceivePayment_ValidRequest_ReturnsOkWithUpdatedReceivable|ReceivePayment_PartialPayment_ReturnsOkWithPartialStatus|PayPayment_ValidRequest_ReturnsOkWithUpdatedPayable|PayPayment_PartialPayment_ReturnsOkWithPartialStatus"

npm run type-check
```

### 结果
- 后端应用层针对性测试通过
- 后端 API 控制器针对性测试通过
- 前端 TypeScript 类型检查通过

---

## 数据库变更

### 新增迁移
`20260403123923_MakeSettlementTransactionIdRequired`

### 部署前注意
- 执行迁移前，请确认生产环境不存在未删除且 `transaction_id IS NULL` 的 `receivable_details` / `payable_details` 记录
- 若存在，迁移会按设计直接失败，不会静默写入 `0`

---

## 配置变更
无

---

## 部署注意事项
1. 前后端需同时部署
2. 需执行数据库迁移
3. 若迁移失败，应先清理历史空绑定数据后再重试

---

## 后续建议
1. 在测试/预发环境先执行一次迁移演练
2. 增加数据库层巡检脚本，定期检查历史脏数据
3. 继续完善待分配交易页面正式功能
