# 交易记录详情访问增强 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 所有展示交易记录的页面都支持点击行查看详情弹窗 + 精简操作列"详情"按钮，保持交互一致性。

**Architecture:** 复用现有 `TransactionDetailPage` 组件（modal 模式，接收 `visible` + `transactionId` props）。每个页面新增 `detailVisible` / `currentTransactionId` 状态，el-table 增加 `@row-click` + 操作列。标签分析页特殊处理：点击行跳转到交易管理页并传 `tagId` query。

**Tech Stack:** Vue 3 + Element Plus + TypeScript

**Spec:** `docs/superpowers/specs/2026-04-09-transaction-detail-access-design.md`

---

### Task 1: DashboardPage — 增加交易详情弹窗

**Files:**
- Modify: `frontend/src/features/dashboard/pages/DashboardPage.vue`

- [ ] **Step 1: 添加 import 和状态**

在 `<script setup>` 中，`import dayjs from 'dayjs'` 之后（line 202），添加 TransactionDetail 导入：

```typescript
import TransactionDetail from '@/features/transactions/pages/TransactionDetailPage.vue'
```

在 `const recentTransactions = ref<RecentTransaction[]>([])` 之后（line 236），添加：

```typescript
const detailVisible = ref(false)
const currentTransactionId = ref(0)

const handleViewTransaction = (row: RecentTransaction) => {
  currentTransactionId.value = row.id
  detailVisible.value = true
}
```

- [ ] **Step 2: 给 el-table 添加 @row-click + 操作列**

修改 line 131 的 `<el-table>` 开标签，添加 `@row-click` 和 cursor 样式：

```vue
<el-table :data="recentTransactions" class="modern-table resizable-table clickable-rows" style="width: 100%" border allow-drag-last-column @header-dragend="handleHeaderDragend" @row-click="handleViewTransaction">
```

在 `description` 列（line 167 `</el-table-column>`）之后、`</el-table>` 之前，添加操作列：

```vue
        <el-table-column label="操作" width="80" fixed="right">
          <template #default="{ row }">
            <el-button link type="primary" size="small" @click.stop="handleViewTransaction(row)">详情</el-button>
          </template>
        </el-table-column>
```

- [ ] **Step 3: 挂载 TransactionDetail 组件**

在 `</el-table>` 所在的 `</div>`（line 169）之后，模板末尾 `</div></template>` 之前，添加：

```vue
    <TransactionDetail
      v-model:visible="detailVisible"
      :transaction-id="currentTransactionId"
    />
```

- [ ] **Step 4: 添加 clickable-rows 样式**

在文件的 `<style>` 区域添加：

```css
.clickable-rows :deep(tr) {
  cursor: pointer;
}
```

- [ ] **Step 5: 验证**

```bash
cd frontend && npx vue-tsc --noEmit --project tsconfig.app.json 2>&1 | head -20
```

预期：无新增类型错误。

- [ ] **Step 6: 提交**

```bash
git add frontend/src/features/dashboard/pages/DashboardPage.vue
git commit -m "feat: add transaction detail access to dashboard"
```

---

### Task 2: AccountDetailPage — 增加交易详情弹窗

**Files:**
- Modify: `frontend/src/features/master-data/accounts/pages/AccountDetailPage.vue`

- [ ] **Step 1: 添加 import**

在 line 226 `import TagDisplay from '@/components/tags/TagDisplay.vue'` 之后添加：

```typescript
import TransactionDetail from '@/features/transactions/pages/TransactionDetailPage.vue'
```

- [ ] **Step 2: 添加状态和处理函数**

在 `const fixedDepositsLoaded = ref(false)` 之后（约 line 250），添加：

```typescript
const detailVisible = ref(false)
const currentTransactionId = ref(0)

const handleViewTransaction = (row: Transaction) => {
  currentTransactionId.value = row.id
  detailVisible.value = true
}
```

- [ ] **Step 3: 修改 el-table 添加 @row-click 和操作列**

修改 line 151 的 `<el-table>` 开标签，添加 `@row-click` 和 class：

```vue
              <el-table
                :data="transactions"
                v-loading="transactionsLoading"
                class="resizable-table clickable-rows"
                border
                allow-drag-last-column
                @header-dragend="handleHeaderDragend"
                @row-click="handleViewTransaction"
              >
```

在 `description` 列（line 204 `</el-table-column>`）之后、`</el-table>` 之前，添加操作列：

```vue
                <el-table-column label="操作" width="80" fixed="right">
                  <template #default="{ row }">
                    <el-button link type="primary" size="small" @click.stop="handleViewTransaction(row)">详情</el-button>
                  </template>
                </el-table-column>
```

- [ ] **Step 4: 挂载 TransactionDetail 组件**

在 `</template>` 前（模板最末尾），`</div></div>` 之前添加：

```vue
    <TransactionDetail
      v-model:visible="detailVisible"
      :transaction-id="currentTransactionId"
    />
```

- [ ] **Step 5: 添加 clickable-rows 样式**

在 `<style>` 区域添加：

```css
.clickable-rows :deep(tr) {
  cursor: pointer;
}
```

- [ ] **Step 6: 提交**

```bash
git add frontend/src/features/master-data/accounts/pages/AccountDetailPage.vue
git commit -m "feat: add transaction detail access to account detail page"
```

---

### Task 3: ProjectDetailPage — 增加交易详情弹窗

**Files:**
- Modify: `frontend/src/features/master-data/projects/pages/ProjectDetailPage.vue`

- [ ] **Step 1: 添加 import**

在 line 303 `import TagDisplay from '@/components/tags/TagDisplay.vue'` 之后添加：

```typescript
import TransactionDetail from '@/features/transactions/pages/TransactionDetailPage.vue'
```

- [ ] **Step 2: 添加状态和处理函数**

在 `const payablesLoaded = ref(false)` 之后（约 line 321），添加：

```typescript
const detailVisible = ref(false)
const currentTransactionId = ref(0)

const handleViewTransaction = (row: Transaction) => {
  currentTransactionId.value = row.id
  detailVisible.value = true
}
```

- [ ] **Step 3: 修改 el-table 添加 @row-click 和操作列**

修改 line 109 的 `<el-table>` 开标签，添加 `@row-click` 和 class：

```vue
            <el-table
              :data="transactions"
              v-loading="transactionsLoading"
              class="resizable-table clickable-rows"
              border
              allow-drag-last-column
              @header-dragend="handleHeaderDragend"
              @row-click="handleViewTransaction"
            >
```

在 `description` 列（line 170 `</el-table-column>`）之后、`</el-table>` 之前，添加操作列：

```vue
              <el-table-column label="操作" width="80" fixed="right">
                <template #default="{ row }">
                  <el-button link type="primary" size="small" @click.stop="handleViewTransaction(row)">详情</el-button>
                </template>
              </el-table-column>
```

- [ ] **Step 4: 挂载 TransactionDetail 组件**

在 `</template>` 前的模板末尾添加：

```vue
    <TransactionDetail
      v-model:visible="detailVisible"
      :transaction-id="currentTransactionId"
    />
```

- [ ] **Step 5: 添加 clickable-rows 样式**

在 `<style>` 区域添加：

```css
.clickable-rows :deep(tr) {
  cursor: pointer;
}
```

- [ ] **Step 6: 提交**

```bash
git add frontend/src/features/master-data/projects/pages/ProjectDetailPage.vue
git commit -m "feat: add transaction detail access to project detail page"
```

---

### Task 4: CustomerDetailPage — 增加交易详情弹窗

**Files:**
- Modify: `frontend/src/features/master-data/customers/pages/CustomerDetailPage.vue`

- [ ] **Step 1: 添加 import**

在 line 192 `import { formatDateTime, formatTransactionAmount, getTransactionAmountColor, formatCurrency } from '@/shared/utils/formatters'` 之后添加：

```typescript
import TransactionDetail from '@/features/transactions/pages/TransactionDetailPage.vue'
```

- [ ] **Step 2: 添加状态和处理函数**

在 `const payablesLoaded = ref(false)` 之后（约 line 210），添加：

```typescript
const detailVisible = ref(false)
const currentTransactionId = ref(0)

const handleViewTransaction = (row: Transaction) => {
  currentTransactionId.value = row.id
  detailVisible.value = true
}
```

- [ ] **Step 3: 修改 el-table 添加 @row-click 和操作列**

修改 line 85 的 `<el-table>` 开标签，添加 `@row-click` 和 class：

```vue
            <el-table
              :data="transactions"
              v-loading="transactionsLoading"
              class="resizable-table clickable-rows"
              border
              allow-drag-last-column
              @header-dragend="handleHeaderDragend"
              @row-click="handleViewTransaction"
            >
```

在 `description` 列（line 156 `</el-table-column>`）之后、`</el-table>` 之前，添加操作列：

```vue
              <el-table-column label="操作" width="80" fixed="right">
                <template #default="{ row }">
                  <el-button link type="primary" size="small" @click.stop="handleViewTransaction(row)">详情</el-button>
                </template>
              </el-table-column>
```

- [ ] **Step 4: 挂载 TransactionDetail 组件**

在 `</template>` 前的模板末尾添加：

```vue
    <TransactionDetail
      v-model:visible="detailVisible"
      :transaction-id="currentTransactionId"
    />
```

- [ ] **Step 5: 添加 clickable-rows 样式**

在 `<style>` 区域添加：

```css
.clickable-rows :deep(tr) {
  cursor: pointer;
}
```

- [ ] **Step 6: 提交**

```bash
git add frontend/src/features/master-data/customers/pages/CustomerDetailPage.vue
git commit -m "feat: add transaction detail access to customer detail page"
```

---

### Task 5: SupplierDetailPage — 增加交易详情弹窗

**Files:**
- Modify: `frontend/src/features/master-data/suppliers/pages/SupplierDetailPage.vue`

- [ ] **Step 1: 添加 import**

在 line 155 `import { formatDateTime, formatTransactionAmount, getTransactionAmountColor, formatCurrency } from '@/shared/utils/formatters'` 之后添加：

```typescript
import TransactionDetail from '@/features/transactions/pages/TransactionDetailPage.vue'
```

- [ ] **Step 2: 添加状态和处理函数**

在 `const payablesLoaded = ref(false)` 之后（约 line 170），添加：

```typescript
const detailVisible = ref(false)
const currentTransactionId = ref(0)

const handleViewTransaction = (row: Transaction) => {
  currentTransactionId.value = row.id
  detailVisible.value = true
}
```

- [ ] **Step 3: 修改 el-table 添加 @row-click 和操作列**

修改 line 65 的 `<el-table>` 开标签，添加 `@row-click` 和 class：

```vue
            <el-table
              :data="transactions"
              v-loading="transactionsLoading"
              class="resizable-table clickable-rows"
              border
              allow-drag-last-column
              @header-dragend="handleHeaderDragend"
              @row-click="handleViewTransaction"
            >
```

在 `description` 列（line 119 `</el-table-column>`）之后、`</el-table>` 之前，添加操作列：

```vue
              <el-table-column label="操作" width="80" fixed="right">
                <template #default="{ row }">
                  <el-button link type="primary" size="small" @click.stop="handleViewTransaction(row)">详情</el-button>
                </template>
              </el-table-column>
```

- [ ] **Step 4: 挂载 TransactionDetail 组件**

在 `</template>` 前的模板末尾添加：

```vue
    <TransactionDetail
      v-model:visible="detailVisible"
      :transaction-id="currentTransactionId"
    />
```

- [ ] **Step 5: 添加 clickable-rows 样式**

在 `<style>` 区域添加：

```css
.clickable-rows :deep(tr) {
  cursor: pointer;
}
```

- [ ] **Step 6: 提交**

```bash
git add frontend/src/features/master-data/suppliers/pages/SupplierDetailPage.vue
git commit -m "feat: add transaction detail access to supplier detail page"
```

---

### Task 6: PersonDetailPage — 增加交易详情弹窗

**Files:**
- Modify: `frontend/src/features/master-data/persons/pages/PersonDetailPage.vue`

- [ ] **Step 1: 添加 import**

在 line 155 `import { formatDateTime, formatTransactionAmount, getTransactionAmountColor, formatCurrency } from '@/shared/utils/formatters'` 之后添加：

```typescript
import TransactionDetail from '@/features/transactions/pages/TransactionDetailPage.vue'
```

- [ ] **Step 2: 添加状态和处理函数**

在 `const payablesLoaded = ref(false)` 之后（约 line 170），添加：

```typescript
const detailVisible = ref(false)
const currentTransactionId = ref(0)

const handleViewTransaction = (row: Transaction) => {
  currentTransactionId.value = row.id
  detailVisible.value = true
}
```

- [ ] **Step 3: 修改 el-table 添加 @row-click 和操作列**

修改 line 65 的 `<el-table>` 开标签，添加 `@row-click` 和 class：

```vue
            <el-table
              :data="transactions"
              v-loading="transactionsLoading"
              class="resizable-table clickable-rows"
              border
              allow-drag-last-column
              @header-dragend="handleHeaderDragend"
              @row-click="handleViewTransaction"
            >
```

在 `description` 列（line 119 `</el-table-column>`）之后、`</el-table>` 之前，添加操作列：

```vue
              <el-table-column label="操作" width="80" fixed="right">
                <template #default="{ row }">
                  <el-button link type="primary" size="small" @click.stop="handleViewTransaction(row)">详情</el-button>
                </template>
              </el-table-column>
```

- [ ] **Step 4: 挂载 TransactionDetail 组件**

在 `</template>` 前的模板末尾添加：

```vue
    <TransactionDetail
      v-model:visible="detailVisible"
      :transaction-id="currentTransactionId"
    />
```

- [ ] **Step 5: 添加 clickable-rows 样式**

在 `<style>` 区域添加：

```css
.clickable-rows :deep(tr) {
  cursor: pointer;
}
```

- [ ] **Step 6: 提交**

```bash
git add frontend/src/features/master-data/persons/pages/PersonDetailPage.vue
git commit -m "feat: add transaction detail access to person detail page"
```

---

### Task 7: UnallocatedTransactionsPage — 增加交易详情弹窗

**Files:**
- Modify: `frontend/src/features/transactions/pages/UnallocatedTransactionsPage.vue`

- [ ] **Step 1: 添加 import**

在 line 218 `import { dateRangeShortcuts } from '@/shared/utils/dateShortcuts'` 之后添加：

```typescript
import TransactionDetail from '@/features/transactions/pages/TransactionDetailPage.vue'
```

- [ ] **Step 2: 添加状态和处理函数**

在 `const processing = ref(false)` 之后（line 230），添加：

```typescript
const detailVisible = ref(false)
const currentTransactionId = ref(0)

const handleViewTransaction = (row: Transaction) => {
  currentTransactionId.value = row.id
  detailVisible.value = true
}
```

- [ ] **Step 3: 修改 el-table 添加 @row-click**

修改 line 62 的 `<el-table>` 开标签，添加 `@row-click` 和 class：

```vue
      <el-table
        :data="transactions"
        v-loading="loading"
        class="clickable-rows"
        @selection-change="handleSelectionChange"
        @sort-change="handleSortChange"
        @row-click="handleViewTransaction"
      >
```

- [ ] **Step 4: 在"处理"按钮旁添加"详情"按钮**

修改 line 105-111 的操作列，将宽度从 120 改为 160，并添加"详情"按钮：

```vue
        <el-table-column label="操作" width="160" fixed="right">
          <template #default="{ row }">
            <el-button link type="primary" @click.stop="handleViewTransaction(row)">
              详情
            </el-button>
            <el-button link type="primary" @click.stop="handleProcess(row)">
              处理
            </el-button>
          </template>
        </el-table-column>
```

- [ ] **Step 5: 挂载 TransactionDetail 组件**

在 `</template>` 前的模板末尾添加（在现有 `el-dialog` 之后）：

```vue
    <TransactionDetail
      v-model:visible="detailVisible"
      :transaction-id="currentTransactionId"
    />
```

- [ ] **Step 6: 添加 clickable-rows 样式**

在 `<style>` 区域添加：

```css
.clickable-rows :deep(tr) {
  cursor: pointer;
}
```

- [ ] **Step 7: 提交**

```bash
git add frontend/src/features/transactions/pages/UnallocatedTransactionsPage.vue
git commit -m "feat: add transaction detail access to unallocated transactions page"
```

---

### Task 8: TagAnalyticsPage — 点击标签行跳转交易管理

**Files:**
- Modify: `frontend/src/features/master-data/tags/pages/TagAnalyticsPage.vue`

- [ ] **Step 1: 添加 router import**

在 line 160 `import { ref, computed, onMounted } from 'vue'` 之后添加：

```typescript
import { useRouter } from 'vue-router'
```

在 `use([...])` 之后（line 171），添加：

```typescript
const router = useRouter()

const handleTagRowClick = (row: TagSummaryDto['items'][number]) => {
  router.push({ name: 'Transactions', query: { tagId: String(row.tagId) } })
}
```

- [ ] **Step 2: 给标签明细表格添加 @row-click**

修改 line 66 的 `<el-table>` 开标签：

```vue
      <el-table :data="summaryData.items" stripe size="small" class="clickable-rows" @row-click="handleTagRowClick">
```

- [ ] **Step 3: 添加 clickable-rows 样式**

在 `<style>` 区域添加：

```css
.clickable-rows :deep(tr) {
  cursor: pointer;
}
```

- [ ] **Step 4: 提交**

```bash
git add frontend/src/features/master-data/tags/pages/TagAnalyticsPage.vue
git commit -m "feat: add tag-to-transaction navigation in tag analytics page"
```

---

### Task 9: 验证与最终提交

- [ ] **Step 1: TypeScript 类型检查**

```bash
cd frontend && npx vue-tsc --noEmit --project tsconfig.app.json
```

预期：无新增类型错误。

- [ ] **Step 2: 构建验证**

```bash
cd frontend && npm run build
```

预期：构建成功。

- [ ] **Step 3: 手动验证清单**

启动开发服务器后逐一验证：

1. **Dashboard** `/` — 点击最近交易行 → 弹出交易详情；点击"详情"按钮 → 同样弹出
2. **账户详情** `/accounts/:id` — 交易记录 Tab 中点击行/按钮 → 弹出交易详情
3. **项目详情** `/projects/:id` — 同上
4. **客户详情** `/customers/:id` — 同上
5. **供应商详情** `/suppliers/:id` — 同上
6. **人员详情** `/persons/:id` — 同上
7. **待分配交易** `/unallocated-transactions` — 点击行 → 弹出详情；"详情"和"处理"按钮分别工作正常
8. **标签分析** `/tag-analytics` — 点击标签行 → 跳转到 `/transactions?tagId=xxx`，交易列表正确筛选
9. 所有页面：点击操作列中的 el-link（账户、分类、项目链接）不会同时触发行点击（`@click.stop` 生效）
