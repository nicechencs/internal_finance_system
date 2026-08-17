<template>
  <div class="page-container">
    <div class="page-header">
      <div class="page-header-left">
        <el-button text @click="router.back()">
          <el-icon><ArrowLeft /></el-icon> 返回
        </el-button>
        <h2 class="page-title">客户详情</h2>
      </div>
      <el-button type="warning" @click="linkDialogVisible = true" v-if="customer">
        <el-icon><Link /></el-icon> 一键关联
      </el-button>
    </div>

    <LinkDialog
      v-model="linkDialogVisible"
      :link-type="LinkType.Customer"
      :entity-id="Number(route.params.id)"
      :entity-name="customer?.name || ''"
      @success="handleLinkSuccess"
    />

    <div v-loading="loading">
      <div class="info-card">
        <el-descriptions :column="3" border>
          <el-descriptions-item label="客户名称">
            {{ customer?.name || '-' }}
          </el-descriptions-item>
          <el-descriptions-item label="简称">
            {{ customer?.shortName || '-' }}
          </el-descriptions-item>
          <el-descriptions-item label="联系人">
            {{ customer?.contactPerson || '-' }}
          </el-descriptions-item>
          <el-descriptions-item label="联系电话">
            {{ customer?.contactPhone || '-' }}
          </el-descriptions-item>
          <el-descriptions-item label="联系邮箱">
            {{ customer?.contactEmail || '-' }}
          </el-descriptions-item>
          <el-descriptions-item label="地址">
            {{ customer?.address || '-' }}
          </el-descriptions-item>
          <el-descriptions-item label="税号">
            {{ customer?.taxNumber || '-' }}
          </el-descriptions-item>
          <el-descriptions-item label="银行账号">
            {{ customer?.bankAccount || '-' }}
          </el-descriptions-item>
          <el-descriptions-item label="开户行">
            {{ customer?.bankName || '-' }}
          </el-descriptions-item>
          <el-descriptions-item label="备注" :span="3">
            {{ customer?.description || '-' }}
          </el-descriptions-item>
          <el-descriptions-item label="状态">
            <el-tag v-if="customer" :type="customer.isActive ? 'success' : 'danger'">
              {{ customer.isActive ? '启用' : '禁用' }}
            </el-tag>
          </el-descriptions-item>
          <el-descriptions-item label="创建时间">
            {{ customer ? formatDateTime(customer.createdAt) : '-' }}
          </el-descriptions-item>
        </el-descriptions>
      </div>

      <div class="tab-section">
        <el-tabs v-model="activeTab" @tab-change="handleTabChange">
          <el-tab-pane label="交易记录" name="transactions">
            <div class="transactions-summary mb-4">
              <el-descriptions :column="4" border size="small">
                <el-descriptions-item label="收入总额">
                  <span class="amount-success">{{ formatCurrency(transactionSummary.income) }}</span>
                </el-descriptions-item>
                <el-descriptions-item label="支出总额">
                  <span class="amount-danger">{{ formatCurrency(transactionSummary.expense) }}</span>
                </el-descriptions-item>
                <el-descriptions-item label="转账总额">
                  <span>{{ formatCurrency(transactionSummary.transfer) }}</span>
                </el-descriptions-item>
                <el-descriptions-item label="净额">
                  <span :class="transactionSummary.net >= 0 ? 'amount-success' : 'amount-danger'">
                    {{ formatCurrency(transactionSummary.net) }}
                  </span>
                </el-descriptions-item>
              </el-descriptions>
            </div>
            <div class="type-filter-row">
              <TransactionTypeFilter v-model="typeFilter" />
            </div>
            <el-table
              :data="filteredTransactions"
              v-loading="transactionsLoading"
              class="resizable-table clickable-rows"
              border
              allow-drag-last-column
              @header-dragend="handleHeaderDragend"
              @row-click="handleViewTransaction"
            >
              <el-table-column prop="transactionDate" label="日期" :width="getColumnWidth('transactionDate', TABLE_COLUMN_WIDTH.date)">
                <template #default="{ row }">{{ formatDate(row.transactionDate) }}</template>
              </el-table-column>
              <el-table-column prop="transactionType" label="类型" :width="getColumnWidth('transactionType', TABLE_COLUMN_WIDTH.type)">
                <template #default="{ row }">
                  <TransactionTypeTag
                    :transaction-type="row.transactionType"
                    :transfer-direction="row.transferDirection"
                  />
                </template>
              </el-table-column>
              <el-table-column prop="amount" label="金额" :width="getColumnWidth('amount', TABLE_COLUMN_WIDTH.amount)" align="right">
                <template #default="{ row }">
                  <span :style="{ color: getTransactionAmountColor(row.transactionType) }">
                    {{ formatTransactionAmount(row.amount, row.transactionType) }}
                  </span>
                </template>
              </el-table-column>
              <el-table-column label="标签" :width="getColumnWidth('tags', 180)">
                <template #default="{ row }">
                  <TagDisplay :tags="row.tags || []" size="small" :max-display="2" />
                </template>
              </el-table-column>
              <el-table-column prop="accountName" label="账户" :width="getColumnWidth('accountName', TABLE_COLUMN_WIDTH.account)">
                <template #default="{ row }">
                  <el-link
                    v-if="row.accountId"
                    type="primary"
                    @click.stop="router.push({ name: 'AccountDetail', params: { id: row.accountId } })"
                  >
                    {{ row.accountName }}
                  </el-link>
                  <span v-else>{{ row.accountName || '-' }}</span>
                </template>
              </el-table-column>
              <el-table-column prop="categoryName" label="分类" :width="getColumnWidth('categoryName', TABLE_COLUMN_WIDTH.category)">
                <template #default="{ row }">
                  <el-link
                    v-if="row.categoryId"
                    type="primary"
                    @click.stop="router.push({ name: 'Transactions', query: { categoryId: String(row.categoryId) } })"
                  >
                    {{ row.categoryName }}
                  </el-link>
                  <span v-else>{{ row.categoryName || '-' }}</span>
                </template>
              </el-table-column>
              <el-table-column prop="projectName" label="项目" :width="getColumnWidth('projectName', TABLE_COLUMN_WIDTH.project)">
                <template #default="{ row }">
                  <el-link
                    v-if="row.projectId"
                    type="primary"
                    @click.stop="router.push({ name: 'ProjectDetail', params: { id: row.projectId } })"
                  >
                    {{ row.projectName }}
                  </el-link>
                  <span v-else>{{ row.projectName || '-' }}</span>
                </template>
              </el-table-column>
              <el-table-column
                prop="description"
                label="描述"
                :min-width="getColumnMinWidth('description', TABLE_COLUMN_WIDTH.description)"
                show-overflow-tooltip
              />
              <el-table-column label="操作" width="80" fixed="right">
                <template #default="{ row }">
                  <el-button link type="primary" size="small" @click.stop="handleViewTransaction(row)">详情</el-button>
                </template>
              </el-table-column>
            </el-table>
          </el-tab-pane>
          <el-tab-pane label="应收记录" name="receivables">
            <ReceivableRecordsTable :records="receivables" :loading="receivablesLoading" />
          </el-tab-pane>
          <el-tab-pane label="应付记录" name="payables">
            <PayableRecordsTable :records="payables" :loading="payablesLoading" />
          </el-tab-pane>
        </el-tabs>
      </div>

    </div>

    <TransactionDetail
      v-model:visible="detailVisible"
      :transaction-id="currentTransactionId"
    />
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { ElMessage } from 'element-plus'
import { ArrowLeft, Link } from '@element-plus/icons-vue'
import { getCustomerById } from '@/features/master-data/customers/api/customer'
import { getTransactionsByCustomer } from '@/features/transactions/api/transaction'
import { getReceivablesByCustomer } from '@/features/finance/api/receivable'
import { getPayablesByCustomer } from '@/features/finance/api/payable'
import LinkDialog from '@/features/transactions/components/LinkDialog.vue'
import ReceivableRecordsTable from '@/shared/ui/ReceivableRecordsTable.vue'
import PayableRecordsTable from '@/shared/ui/PayableRecordsTable.vue'
import TransactionTypeTag from '@/shared/ui/TransactionTypeTag.vue'
import TransactionTypeFilter from '@/shared/ui/TransactionTypeFilter.vue'
import { filterTransactionsByType, type TransactionTypeFilter as TypeFilter } from '@/shared/utils/transactionType'
import TagDisplay from '@/components/tags/TagDisplay.vue'
import type { Customer } from '@/features/master-data/customers/types/customer'
import type { Transaction } from '@/features/transactions/types/transaction'
import type { Receivable } from '@/features/finance/types/receivable'
import type { Payable } from '@/features/finance/types/payable'
import { LinkType } from '@/features/transactions/types/link'
import { TABLE_COLUMN_WIDTH } from '@/shared/constants/table'
import { useResizableTableColumns } from '@/shared/composables/useResizableTableColumns'
import { formatDateTime, formatTransactionAmount, getTransactionAmountColor, formatCurrency } from '@/shared/utils/formatters'
import TransactionDetail from '@/features/transactions/pages/TransactionDetailPage.vue'

const route = useRoute()
const router = useRouter()
const { getColumnWidth, getColumnMinWidth, handleHeaderDragend } = useResizableTableColumns('customer-detail-transactions')

const loading = ref(false)
const customer = ref<Customer | null>(null)
const activeTab = ref('transactions')
const transactions = ref<Transaction[]>([])
const transactionsLoading = ref(false)
const transactionsLoaded = ref(false)
const typeFilter = ref<TypeFilter>('all')
const filteredTransactions = computed(() => filterTransactionsByType(transactions.value, typeFilter.value))
const linkDialogVisible = ref(false)
const receivables = ref<Receivable[]>([])
const receivablesLoading = ref(false)
const receivablesLoaded = ref(false)
const payables = ref<Payable[]>([])
const payablesLoading = ref(false)
const payablesLoaded = ref(false)

const detailVisible = ref(false)
const currentTransactionId = ref(0)

const handleViewTransaction = (row: Transaction) => {
  currentTransactionId.value = row.id
  detailVisible.value = true
}

const transactionSummary = computed(() => {
  const income = transactions.value
    .filter(t => t.transactionType === 'Income')
    .reduce((sum, t) => sum + t.amount, 0)
  const expense = transactions.value
    .filter(t => t.transactionType === 'Expense')
    .reduce((sum, t) => sum + t.amount, 0)
  const transfer = transactions.value
    .filter(t => t.transactionType === 'Transfer')
    .reduce((sum, t) => sum + t.amount, 0)
  return { income, expense, transfer, net: income - expense }
})

const handleLinkSuccess = () => {
  transactionsLoaded.value = false
  receivablesLoaded.value = false
  payablesLoaded.value = false
  loadTransactions()
}

const loadCustomer = async () => {
  const id = Number(route.params.id)
  if (!id) return

  loading.value = true
  try {
    const { data } = await getCustomerById(id)
    customer.value = data.data
    await loadTransactions()
  } catch {
    ElMessage.error('加载客户详情失败')
  } finally {
    loading.value = false
  }
}

const loadTransactions = async () => {
  const id = Number(route.params.id)
  if (!id || transactionsLoaded.value) return

  transactionsLoading.value = true
  try {
    const { data } = await getTransactionsByCustomer(id)
    transactions.value = data.data
    transactionsLoaded.value = true
  } catch {
    ElMessage.error('加载交易记录失败')
  } finally {
    transactionsLoading.value = false
  }
}

const loadReceivables = async () => {
  const id = Number(route.params.id)
  if (!id || receivablesLoaded.value) return
  receivablesLoading.value = true
  try {
    const { data } = await getReceivablesByCustomer(id)
    receivables.value = data.data
    receivablesLoaded.value = true
  } catch {
    ElMessage.error('加载应收记录失败')
  } finally {
    receivablesLoading.value = false
  }
}

const loadPayables = async () => {
  const id = Number(route.params.id)
  if (!id || payablesLoaded.value) return
  payablesLoading.value = true
  try {
    const { data } = await getPayablesByCustomer(id)
    payables.value = data.data
    payablesLoaded.value = true
  } catch {
    ElMessage.error('加载应付记录失败')
  } finally {
    payablesLoading.value = false
  }
}

const handleTabChange = (tab: string | number) => {
  if (tab === 'transactions' && !transactionsLoaded.value) loadTransactions()
  if (tab === 'receivables' && !receivablesLoaded.value) loadReceivables()
  if (tab === 'payables' && !payablesLoaded.value) loadPayables()
}

const formatDate = (date: string) => formatDateTime(date, 'date')

onMounted(async () => {
  await loadCustomer()
})
</script>

<style scoped>
.page-container {
  padding: 0;
}

.page-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 20px;
}

.page-header-left {
  display: flex;
  align-items: center;
  gap: 12px;
}

.page-title {
  font-size: 20px;
  font-weight: 600;
  color: var(--text-primary);
  margin: 0;
}

.info-card {
  background: var(--bg-card);
  border-radius: 12px;
  padding: 20px;
  margin-bottom: 16px;
  box-shadow: 0 1px 3px rgba(0, 0, 0, 0.04);
}

.info-card :deep(.el-descriptions__label) {
  color: var(--text-secondary);
  font-size: 13px;
  font-weight: 600;
}

.info-card :deep(.el-descriptions__content) {
  color: var(--text-regular);
  font-size: 13px;
}

.tab-section {
  background: var(--bg-card);
  border-radius: 12px;
  padding: 20px;
  box-shadow: 0 1px 3px rgba(0, 0, 0, 0.04);
}

.tab-section :deep(.el-tabs__header) {
  margin-bottom: 16px;
}

.tab-section :deep(.el-tabs__item) {
  font-size: 14px;
  color: var(--text-secondary);
}

.tab-section :deep(.el-tabs__item.is-active) {
  color: var(--color-primary);
  font-weight: 600;
}

.tab-section :deep(.el-table) {
  font-size: 13px;
}

.tab-section :deep(.el-table th.el-table__cell) {
  font-weight: 600;
  font-size: 12px;
  text-transform: uppercase;
  letter-spacing: 0.5px;
}

.tab-section :deep(.el-table td.el-table__cell) {
  padding: 12px 0;
}

.transactions-summary {
  margin-bottom: 16px;
}

.type-filter-row {
  margin-bottom: 12px;
}

.amount-success {
  color: var(--color-success);
  font-weight: 600;
}

.amount-danger {
  color: var(--color-danger);
  font-weight: 600;
}

.mb-4 {
  margin-bottom: 16px;
}

.clickable-rows :deep(tr) {
  cursor: pointer;
}
</style>
