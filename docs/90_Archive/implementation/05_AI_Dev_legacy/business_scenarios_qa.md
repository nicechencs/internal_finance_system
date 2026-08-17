# 业务场景问答

> 本文档记录了交易管理、应付管理、费用分摊等模块的关键业务场景问题及设计方案。
> 这些问题来自实际业务需求的深度思考，对系统设计有重要指导意义。

---

## 问题 1：如果没有记录应付直接支付的时候呢？

### 业务场景

**典型情况**：
- 小额采购：买办公用品、支付水电费，金额小，不需要提前记录应付
- 即时支付：供应商要求现款现货，没有账期
- 临时支出：突发性支出，来不及提前记录应付

**现实例子**：
- 在淘宝买了 500 元办公用品，直接支付宝付款
- 电费账单 2000 元，收到账单后立即支付
- 临时请人修电脑，现场支付 300 元

### 设计方案

**核心原则**：**应付管理是可选的**

```
判断标准：
✅ 需要记录应付：有账期、分期付款、需要跟踪付款进度、金额较大
❌ 不需要记录应付：即时支付、小额支出、一次性付清

处理方式：
1. 从银行导入流水 → 生成 transaction 记录（expense 类型）
2. 分类为"办公费用"、"水电费"等
3. 关联 supplier_id（如果有供应商）
4. 不创建 payables 记录

数据模型支持：
- transactions.supplier_id 可以直接关联供应商，不需要通过 payables
- payable_details.transaction_id 可以为 NULL（手动记录的应付，还未实际支付）
- transactions 可以独立存在，不依赖 payables
```

**设计决策**：
- 不强制要求所有支出都记录应付
- 交易记录（transaction）是必须的，应付记录（payable）是可选的
- 灵活关联：transaction 可以关联 payable，也可以不关联

---

## 问题 2：如果实际支付已经超过应付的呢？

### 业务场景

**典型情况**：
- 应付金额：10000 元
- 实际支付：10500 元（多付了 500 元）

**可能原因**：
1. 包含额外费用：运费、手续费、税费等
2. 汇率变动：外币支付时汇率波动
3. 预付下次款项：多付的部分算作下次的预付款
4. 操作失误：财务人员输错金额

### 当前设计约束

```sql
-- 当前校验规则（system_modules.md 行 375）
付款金额校验：detail.amount > 0 AND detail.amount <= remaining_amount
```

**问题**：这个校验会阻止超额支付！

### 设计方案

**阶段 1：严格校验（MVP）** - 推荐初期实现

```
校验规则：
- detail.amount <= remaining_amount
- 如果超额，拒绝操作，返回错误

优点：
- 数据严格一致
- 避免错误操作
- 应付金额和实际支付金额完全匹配

处理方式：
- 提示用户先调整应付金额
- 或者拆分支付（应付 10000 + 额外费用 500）
```

**阶段 2：允许超额（可选）** - 未来增强

```
业务规则：
1. 允许 detail.amount > remaining_amount
2. 显示警告："支付金额超过应付金额 500 元，请确认是否包含额外费用"
3. 超额部分记录为"预付款"或"其他费用"

计算公式（超额场景）：
  paid_amount = paid_amount + detail.amount
  remaining_amount = MAX(0, total_amount - paid_amount)
  overpaid_amount = MAX(0, paid_amount - total_amount)

数据记录（需要新增字段）：
payables:
  total_amount: 10000
  paid_amount: 10500
  remaining_amount: 0       // MAX(0, 10000 - 10500) = 0，不会出现负数
  status: settled
  overpaid_amount: 500      // MAX(0, 10500 - 10000) = 500，新增字段

payable_details:
  amount: 10500
  note: "含运费 500 元"
  is_overpaid: true          // 新增字段：标记为超额支付
```

**推荐实施策略**：
1. MVP 阶段：严格校验，不允许超额
2. 增强阶段：显示警告，允许超额（可配置）
3. 完整阶段：支持预付款管理

---

## 问题 3：是否需要把实际支付进行拆分？

### 业务场景

**典型情况**：一笔银行流水支付了多笔应付

**例子**：
- 应付 A：供应商甲，5000 元
- 应付 B：供应商甲，3000 元
- 银行流水：向供应商甲支付 8000 元（一次性支付两笔应付）

### 当前设计支持情况

**数据模型**：
```
transactions (银行流水生成的交易记录)
  id: 1
  amount: 8000
  supplier_id: 甲
  transaction_type: expense

payable_details (应付核销明细)
  id: 1, payable_id: A, transaction_id: 1, amount: 5000
  id: 2, payable_id: B, transaction_id: 1, amount: 3000
```

**关系**：
- ✅ 一笔 transaction 可以关联多笔 payable_details
- ✅ 通过 transaction_id 外键关联
- ✅ 当前数据模型已支持

### 设计方案

**支持一笔交易拆分到多笔应付** ✅

**业务流程**：
```
1. 导入银行流水 → 生成 transaction（amount = 8000）

2. 用户操作"核销应付"：
   - 选择 transaction（8000 元）
   - 选择要核销的应付列表：
     ✓ 应付 A（5000 元）
     ✓ 应付 B（3000 元）
   - 系统校验：5000 + 3000 = 8000 ✅

3. 创建 payable_details：
   - detail 1: payable_id=A, transaction_id=1, amount=5000
   - detail 2: payable_id=B, transaction_id=1, amount=3000

4. 更新应付状态：
   - 应付 A: paid_amount += 5000, status = settled
   - 应付 B: paid_amount += 3000, status = settled
```

**校验规则**：
```
1. 拆分金额不能超过交易金额：
   SUM(payable_details.amount WHERE transaction_id = X) <= transaction.amount

2. 每笔拆分金额不能超过对应应付的剩余金额：
   allocation.amount <= payable.remaining_amount

3. 同一笔交易不能重复核销同一笔应付：
   payableId 不能重复

4. 允许部分核销（MVP 阶段暂不支持，增强版再开放）：
   拆分金额之和可以小于交易金额
   未核销的差额仍留在 transaction 上，作为普通支出处理（如手续费）
   不需要额外追踪机制——差额本身就是一笔独立的支出记录
```

**跨供应商核销**：
```
场景：一笔 8000 元的转账核销了供应商甲的 5000 和供应商乙的 3000
处理：transaction.supplier_id = NULL（不关联单个供应商）
原因：与问题 4 中跨项目时 project_id = NULL 的处理方式一致
```

**API 接口**：
```markdown
POST /api/v1/payables/batch-pay

请求体：
{
  "transactionId": 100,
  "allocations": [
    { "payableId": 1, "amount": 5000.00, "note": "核销应付 A" },
    { "payableId": 2, "amount": 3000.00, "note": "核销应付 B" }
  ]
}

响应：
{
  "transactionId": 100,
  "totalAmount": 8000.00,
  "allocatedAmount": 8000.00,
  "remainingAmount": 0,
  "details": [
    { "payableId": 1, "amount": 5000.00, "newStatus": "settled" },
    { "payableId": 2, "amount": 3000.00, "newStatus": "settled" }
  ]
}
```

---

## 问题 4：如果某一笔支付关联了多个项目、多个需求呢？

### 业务场景

**典型情况**：一笔支付涉及多个项目的费用

**例子**：
- 支付给外包公司 50000 元
- 这笔款项包含：
  - 项目 A 的开发费用：30000 元
  - 项目 B 的开发费用：15000 元
  - 项目 C 的维护费用：5000 元

### 当前设计支持情况

**数据模型分析**：
```
transactions 表：
- 一笔交易只能关联一个 project_id ❌
- 如果需要关联多个项目，需要使用 transaction_allocations（费用分摊）✅

transaction_allocations 表：
- 一笔交易可以分摊到多个项目 ✅
- 每个分摊记录包含：project_id, amount, allocation_rate
```

### 设计方案

**使用费用分摊（transaction_allocations）** ✅

**业务流程**：
```
1. 创建 transaction（amount = 50000，project_id = NULL）

2. 创建费用分摊记录：
   - allocation 1: project_id=A, amount=30000
   - allocation 2: project_id=B, amount=15000
   - allocation 3: project_id=C, amount=5000

3. 校验：30000 + 15000 + 5000 = 50000 ✅

4. 设置 transaction.is_allocated = true
```

**与应付管理的结合**：

**复杂场景**：一笔支付既要核销应付，又要分摊到多个项目

**数据关系**：
```
payables:
  id: 1
  supplier_id: 外包公司
  total_amount: 50000
  project_id: NULL  // 不关联单个项目

transactions:
  id: 100
  amount: 50000
  supplier_id: 外包公司
  project_id: NULL  // 不关联单个项目
  is_allocated: true  // 标记为已分摊

payable_details:
  id: 1, payable_id: 1, transaction_id: 100, amount: 50000

transaction_allocations:
  id: 1, transaction_id: 100, project_id: A, amount: 30000
  id: 2, transaction_id: 100, project_id: B, amount: 15000
  id: 3, transaction_id: 100, project_id: C, amount: 5000
```

**关系链**：
```
payables (应付)
  ↓ (通过 payable_details)
transactions (交易)
  ↓ (通过 transaction_allocations)
projects (项目)
```

**业务规则补充**：

1. **应付可以不关联项目**
   ```
   payables.project_id 可以为 NULL
   - 如果应付涉及多个项目，project_id 设为 NULL
   - 通过 transaction_allocations 记录项目分摊
   ```

2. **费用分摊优先级高于直接关联**
   ```
   如果 transaction.is_allocated = true：
   - 忽略 transaction.project_id 和 transaction.person_id
   - 使用 transaction_allocations 中的分摊记录
   - 项目成本从 transaction_allocations.project_id 聚合
   - 人员成本从 transaction_allocations.person_id 聚合
   ```

3. **`is_allocated` 是关键数据不变量**
   ```
   这个标记是项目利润视图(v_project_profit)和人员成本报表正确性的前提。
   如果不一致，会导致成本重复计算或遗漏。

   不变量规则：
   - 创建 transaction_allocations 记录时，必须同时设置 is_allocated = true
   - 删除所有 allocation 记录时，必须同时恢复 is_allocated = false
   - 重新分摊时，先删旧记录再创建新记录（替换模式），is_allocated 始终为 true
   - 以上操作必须在同一个数据库事务中完成

   违反后果：
   - is_allocated=false 但存在 allocations → 成本被重复计算（直接 + 分摊）
   - is_allocated=true 但 allocations 为空 → 成本丢失（直接被忽略，分摊又没有）
   ```

4. **混合分摊：一笔交易同时分摊到项目和人员**
   ```
   场景：支付外包公司 10000 元，包含开发费 8000（归项目 A）和外包人员个税 2000（归张三）

   transaction_allocations:
     id: 1, project_id=A, person_id=NULL, amount=8000
     id: 2, project_id=NULL, person_id=张三, amount=2000

   校验：8000 + 2000 = 10000 ✅

   一条 allocation 记录也可以同时有 project_id 和 person_id：
     例如：项目 A 中张三的差旅费 3000 → project_id=A, person_id=张三, amount=3000
     这条记录会同时计入项目 A 的成本和张三的人员成本
   ```

5. **应付核销 + 费用分摊的完整流程**
   ```
   1. 创建应付（project_id = NULL）
   2. 导入银行流水 → 生成 transaction
   3. 核销应付：创建 payable_details 关联 transaction
   4. 费用分摊：创建 transaction_allocations 分摊到多个项目/人员
   5. 更新应付状态为 settled
   6. 更新 transaction.is_allocated = true
   注：步骤 3-6 必须在同一个数据库事务中完成
   ```

**API 接口**：
```markdown
POST /api/v1/payables/{id}/pay-and-allocate

请求体：
{
  "transactionId": 100,
  "paymentAmount": 50000.00,
  "allocations": [
    { "projectId": 1, "amount": 30000.00, "note": "项目 A 开发费用" },
    { "projectId": 2, "amount": 15000.00, "note": "项目 B 开发费用" },
    { "projectId": 3, "amount": 5000.00, "note": "项目 C 维护费用" }
  ]
}

校验规则：
- paymentAmount <= payable.remaining_amount
- SUM(allocations.amount) == paymentAmount
- 每个 allocation.amount > 0
- projectId 不能重复

响应：
{
  "payableId": 1,
  "transactionId": 100,
  "paymentAmount": 50000.00,
  "allocatedProjects": [
    { "projectId": 1, "amount": 30000.00 },
    { "projectId": 2, "amount": 15000.00 },
    { "projectId": 3, "amount": 5000.00 }
  ],
  "payableStatus": "settled"
}
```

---

## 问题 5：缴纳个税时，税是否要关联到人？

### 业务场景

**典型情况**：公司统一代缴个人所得税或社保

**例子**：
- 公司向税务局缴纳个税 5000 元
- 银行流水只有一笔：5000 元，对方"XX税务局"
- 但实际上这 5000 元包含：
  - 张三个税：2000 元
  - 李四个税：1500 元
  - 王五个税：1500 元

**核心问题**：如何把一笔税款支出拆分关联到每个员工？

### 设计方案

**使用费用分摊（transaction_allocations）按人员分摊** ✅

`transaction_allocations` 表已扩展支持 `person_id` 字段，与 `project_id` 至少有一个不为空。

**业务流程**：
```
1. 导入银行流水 → 生成 transaction（amount = 5000，分类 = 个税）

2. 创建费用分摊记录（按人员）：
   - allocation 1: person_id=张三, amount=2000
   - allocation 2: person_id=李四, amount=1500
   - allocation 3: person_id=王五, amount=1500

3. 校验：2000 + 1500 + 1500 = 5000 ✅

4. 设置 transaction.is_allocated = true
```

**数据关系**：
```
transactions（个税支出 5000 元）
  ↓ (通过 transaction_allocations)
persons（张三 2000、李四 1500、王五 1500）
```

**人员成本报表影响**：
- 人员成本报表的计算逻辑与 `v_project_profit` 视图对称：
  - 直接成本：`transactions` 中 `is_allocated = false AND person_id = X` 的支出
  - 分摊成本：`transaction_allocations` 中 `person_id = X` 的分摊金额
  - 两者合计为该人员的总成本
- 注意：`is_allocated = true` 的交易不能再通过 `transactions.person_id` 计入，否则会重复计算

**适用场景**：
- 代缴个人所得税
- 代缴社保/公积金
- 其他需要按人分摊的统一支出

---

## 应收与应付的设计差异说明

上述 5 个问题主要围绕应付侧展开。应收侧是否存在同样的场景？

### 差异对比

| 维度 | 应收（receivables） | 应付（payables） | 原因 |
|------|---------------------|------------------|------|
| project_id | NOT NULL（必填） | 可为 NULL | 应收按项目合同收款，每笔一定对应项目；应付可能跨项目 |
| 无记录直接收款 | 同样适用 | ✅ 已设计 | 小额利息收入等也可只记 transaction 不记 receivable |
| 超额收款 | 同样适用 | ✅ 已设计 | 客户多付的场景更少，MVP 阶段严格校验即可 |
| 一笔收款核销多笔应收 | 数据模型已支持 | ✅ 已设计 | receivable_details.transaction_id 同样支持多对一 |
| 收款关联多个项目 | 较少发生 | ✅ 已设计 | 客户通常按项目合同分别付款，跨项目收款罕见 |

### 为什么 receivables.project_id 是 NOT NULL？

```
应收的业务本质：客户按项目合同付款。

- 每笔应收一定对应一个项目合同
- 即使客户一次性支付多个项目的款项，财务上也是按项目分别记录应收
- 合同金额、已收金额、应收金额都是项目维度的统计指标

如果未来出现真正需要跨项目的应收场景，可以参考应付的设计，
将 project_id 改为可空。但目前的业务模型下，NOT NULL 是正确的约束。
```

### 结论

应收侧的同类场景在数据模型层面已具备支持能力，但由于业务特性差异（应收按项目合同管理），不需要像应付那样的复杂灵活设计。**当前的不对称设计是有意为之，不是遗漏**。

---

## 综合设计原则

### 1. 灵活性优先

- **应付管理是可选的**：不是所有支出都需要记录应付
- **应收管理按项目管理**：每笔应收必须关联项目（业务本质决定）
- **支持多种核销方式**：一对一、一对多、多对一
- **支持费用分摊**：一笔交易可以分摊到多个项目和/或多个人员

### 2. 数据一致性

- **严格金额校验**：确保拆分金额之和等于交易金额
- **使用事务**：确保应付核销和费用分摊的原子性
- **`is_allocated` 不变量**：分摊操作必须在同一事务中同步维护此标记
- **完整审计日志**：记录所有操作的详细信息

### 3. 分阶段实施

**MVP（第一版）**：
- ✅ 基础应付管理
- ✅ 严格金额校验
- ✅ 一对一核销
- ✅ 应付管理可选

**增强版（第二版）**：
- ✅ 支持拆分核销（一笔交易核销多笔应付）
- ✅ 支持费用分摊（一笔交易分摊到多个项目）
- ✅ 超额支付警告
- ✅ 批量操作接口

**完整版（第三版）**：
- 🔄 允许超额支付（可配置）
- 🔄 预付款管理
- 🔄 支持部分核销
- 🔄 高级报表和分析

---

## 数据模型调整建议

### 可选新增字段（阶段 3）

#### payables 表
```sql
ALTER TABLE payables ADD COLUMN overpaid_amount DECIMAL(18, 2) DEFAULT 0;
COMMENT ON COLUMN payables.overpaid_amount IS '超额支付金额（实际支付 - 应付金额）';
```

#### payable_details 表
```sql
ALTER TABLE payable_details ADD COLUMN is_overpaid BOOLEAN DEFAULT false;
COMMENT ON COLUMN payable_details.is_overpaid IS '是否为超额支付';

ALTER TABLE payable_details ADD COLUMN allocation_note TEXT;
COMMENT ON COLUMN payable_details.allocation_note IS '拆分说明（如：应付 10000 + 运费 500）';
```

---

## 业务规则更新建议

### 更新：付款金额校验规则

```markdown
# 当前规则（MVP）
4. 付款金额校验：detail.amount > 0 AND detail.amount <= remaining_amount

# 建议更新（增强版）
4. 付款金额校验：
   - 基础校验：detail.amount > 0
   - 超额警告：如果 detail.amount > remaining_amount，显示警告但允许继续（可配置）
   - 超额记录：超额部分记录到 overpaid_amount 字段
   - 拆分校验：如果一笔交易核销多笔应付，SUM(details.amount) <= transaction.amount
```

### 新增：支付拆分规则

```markdown
5. 支付拆分（一笔交易核销多笔应付）：
   - 允许一笔 transaction 关联多笔 payable_details
   - 校验：SUM(payable_details.amount WHERE transaction_id = X) <= transaction.amount
   - 每笔拆分金额必须 > 0
   - 不能重复核销同一笔应付
   - 拆分金额之和可以小于交易金额（剩余部分可能是手续费等）
```

### 新增：费用分摊与应付的关系

```markdown
6. 费用分摊与应付管理的结合：
   - payables.project_id 可以为 NULL（涉及多个项目时）
   - 通过 payable_details 核销应付
   - 通过 transaction_allocations 分摊费用到多个项目
   - 如果 transaction.is_allocated = true，项目成本从 transaction_allocations 聚合
   - 数据流：payables → payable_details → transactions → transaction_allocations → projects
```

---

## 总结

### 五个问题的答案

1. **没有记录应付直接支付** → ✅ 允许，应付管理是可选的
2. **实际支付超过应付** → 阶段 1 严格校验，阶段 2 允许超额（可配置）
3. **实际支付需要拆分** → ✅ 支持，一笔交易可以核销多笔应付
4. **支付关联多个项目** → ✅ 支持，通过 transaction_allocations 费用分摊
5. **个税关联到人** → ✅ 支持，通过 transaction_allocations.person_id 按人分摊

### 核心设计理念

- **灵活性**：支持多种业务场景，不强制单一模式
- **严格性**：金额校验确保数据一致性
- **可选性**：应付管理不是强制的，根据业务需要选择
- **可扩展性**：预留字段和接口支持未来功能

### 实施路径

**当前阶段（MVP）**：
- 基础应付管理 + 严格校验 + 一对一核销

**下一阶段（增强）**：
- 支持拆分核销 + 费用分摊 + 批量操作

**未来阶段（完整）**：
- 允许超额支付 + 预付款管理 + 高级报表
