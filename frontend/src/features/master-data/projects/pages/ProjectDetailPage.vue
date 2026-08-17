<template>
  <div class="page-container">
    <div class="page-header">
      <div class="page-header-left">
        <el-button text @click="router.back()">
          <el-icon><ArrowLeft /></el-icon> 返回
        </el-button>
        <h2 class="page-title">项目详情</h2>
      </div>
      <el-button type="warning" @click="linkDialogVisible = true" v-if="project">
        <el-icon><Link /></el-icon> 一键关联
      </el-button>
    </div>

    <LinkDialog
      v-model="linkDialogVisible"
      :link-type="LinkType.Project"
      :entity-id="Number(route.params.id)"
      :entity-name="project?.name || ''"
      @success="handleLinkSuccess"
    />


    <div v-loading="loading">
       
      <div class="info-card">
        <el-descriptions :column="3" border>
          <el-descriptions-item label="项目名称">{{ project?.name }}</el-descriptions-item>
          <el-descriptions-item label="项目编号">{{ project?.projectCode || '-' }}</el-descriptions-item>
          <el-descriptions-item label="客户">
            <el-link
              v-if="project?.customerId"
              type="primary"
              @click="router.push({ name: 'CustomerDetail', params: { id: project.customerId } })"
            >
              {{ project.customerName }}
            </el-link>
            <span v-else>{{ project?.customerName || '-' }}</span>
          </el-descriptions-item>
          <el-descriptions-item label="合同金额">
            <span class="amount">{{ formatCurrency(project?.contractAmount || 0) }}</span>
          </el-descriptions-item>
          <el-descriptions-item label="已收款">
            <span class="amount-success">{{ formatCurrency(project?.receivedAmount || 0) }}</span>
          </el-descriptions-item>
          <el-descriptions-item label="应收款">
            <span class="amount-warning">{{ formatCurrency(project?.receivableAmount || 0) }}</span>
          </el-descriptions-item>
          <el-descriptions-item label="状态">
            <el-tag :type="getStatusType(project?.status)">
              {{ getStatusText(project?.status) }}
            </el-tag>
          </el-descriptions-item>
          <el-descriptions-item label="开始日期">{{ project?.startDate ? formatDate(project.startDate) : '-' }}</el-descriptions-item>
          <el-descriptions-item label="结束日期">{{ project?.endDate ? formatDate(project.endDate) : '-' }}</el-descriptions-item>
          <el-descriptions-item label="描述" :span="3">{{ project?.description || '-' }}</el-descriptions-item>
          <el-descriptions-item label="标签" :span="3">
            <TagDisplay :tags="project?.tags || []" />
          </el-descriptions-item>
          <el-descriptions-item label="创建时间">{{ project?.createdAt ? formatDateTime(project.createdAt) : '-' }}</el-descriptions-item>
          <el-descriptions-item label="更新时间" :span="2">{{ project?.updatedAt ? formatDateTime(project.updatedAt) : '-' }}</el-descriptions-item>
        </el-descriptions>
      </div>

      <!-- 成本明细 -->
      <div v-if="project?.costBreakdown && project.costBreakdown.length > 0" class="info-card">
        <h4 class="section-title">成本明细</h4>
        <el-table :data="project.costBreakdown" border size="small" style="width: 100%">
          <el-table-column prop="categoryName" label="分类" />
          <el-table-column prop="amount" label="金额" align="right">
            <template #default="{ row }">
              {{ formatCurrency(row.amount) }}
            </template>
          </el-table-column>
          <el-table-column label="占总成本比例" align="right" width="140">
            <template #default="{ row }">
              {{ project!.totalCost > 0 ? ((row.amount / project!.totalCost) * 100).toFixed(1) + '%' : '-' }}
            </template>
          </el-table-column>
        </el-table>
      </div>

        <SummaryOverview
        title="项目概览"
        :subtitle="project ? `${getStatusText(project.status)} · 利润率 ${project.profitRate.toFixed(1)}%` : '查看合同金额、收款、成本和利润'"
        :loading="loading"
        :empty="!project"
      >
        <DetailSummaryCards :items="projectSummaryCards" />
      </SummaryOverview>
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
                    {{ formatTransactionAmount(getProjectAmount(row), row.transactionType) }}
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
              <el-table-column
                prop="description"
                label="描述"
                :min-width="getColumnMinWidth('description', TABLE_COLUMN_WIDTH.description)"
                show-overflow-tooltip
              >
                <template #default="{ row }">{{ row.description || '-' }}</template>
              </el-table-column>
              <el-table-column label="操作" width="80" fixed="right">
                <template #default="{ row }">
                  <el-button link type="primary" size="small" @click.stop="handleViewTransaction(row)">详情</el-button>
                </template>
              </el-table-column>
            </el-table>
          </el-tab-pane>

          <el-tab-pane label="分摊记录" name="allocations">
            <el-table
              :data="allocatedTransactions"
              v-loading="transactionsLoading"
              class="resizable-table"
              border
              allow-drag-last-column
              @header-dragend="handleHeaderDragend"
            >
              <el-table-column prop="transactionDate" label="交易日期" :width="getColumnWidth('allocationTransactionDate', TABLE_COLUMN_WIDTH.date)">
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
              <el-table-column prop="amount" label="交易金额" :width="getColumnWidth('allocationAmount', TABLE_COLUMN_WIDTH.amount)" align="right">
                <template #default="{ row }">{{ formatRMB(row.amount) }}</template>
              </el-table-column>
              <el-table-column column-key="allocationDetails" label="分摊明细" :min-width="getColumnMinWidth('allocationDetails', TABLE_COLUMN_WIDTH.detail)">
                <template #default="{ row }">
                  <div v-if="row.allocations && row.allocations.length > 0">
                    <div v-for="alloc in row.allocations" :key="alloc.id" class="allocation-item">
                      <el-link
                        v-if="alloc.personId && alloc.personName"
                        class="alloc-person"
                        type="primary"
                        @click="router.push({ name: 'PersonDetail', params: { id: alloc.personId } })"
                      >
                        {{ alloc.personName }}
                      </el-link>
                      <span v-else-if="alloc.personName" class="alloc-person">{{ alloc.personName }}</span>
                      <span class="alloc-amount">{{ formatRMB(alloc.amount) }}</span>
                      <span v-if="alloc.allocationRate" class="alloc-rate">({{ alloc.allocationRate }}%)</span>
                      <span v-if="alloc.description" class="alloc-desc">- {{ alloc.description }}</span>
                    </div>
                  </div>
                  <span v-else>-</span>
                </template>
              </el-table-column>
            </el-table>
          </el-tab-pane>

          <el-tab-pane label="收款计划" name="receivables">
            <div class="receivables-summary mb-4">
              <el-descriptions :column="3" border size="small">
                <el-descriptions-item label="应收总额">
                  <span class="amount-primary">{{ formatCurrency(receivablesSummary.totalAmount) }}</span>
                </el-descriptions-item>
                <el-descriptions-item label="已收金额">
                  <span class="amount-success">{{ formatCurrency(receivablesSummary.receivedAmount) }}</span>
                </el-descriptions-item>
                <el-descriptions-item label="未收金额">
                  <span class="amount-warning">{{ formatCurrency(receivablesSummary.remainingAmount) }}</span>
                </el-descriptions-item>
              </el-descriptions>
            </div>

            <el-table
              :data="receivables"
              v-loading="receivablesLoading"
              border
            >
              <el-table-column prop="description" label="期次" width="150">
                <template #default="{ row }">{{ row.description || '-' }}</template>
              </el-table-column>
              <el-table-column prop="totalAmount" label="应收金额" width="150" align="right">
                <template #default="{ row }">{{ formatCurrency(row.totalAmount) }}</template>
              </el-table-column>
              <el-table-column prop="receivedAmount" label="已收金额" width="150" align="right">
                <template #default="{ row }">
                  <span class="amount-success">{{ formatCurrency(row.receivedAmount) }}</span>
                </template>
              </el-table-column>
              <el-table-column prop="remainingAmount" label="未收金额" width="150" align="right">
                <template #default="{ row }">
                  <span class="amount-warning">{{ formatCurrency(row.remainingAmount) }}</span>
                </template>
              </el-table-column>
              <el-table-column prop="dueDate" label="到期日" width="120">
                <template #default="{ row }">{{ row.dueDate ? formatDate(row.dueDate) : '-' }}</template>
              </el-table-column>
              <el-table-column prop="status" label="状态" width="100">
                <template #default="{ row }">
                  <el-tag :type="getReceivableStatusType(row.status)" size="small">
                    {{ getReceivableStatusText(row.status) }}
                  </el-tag>
                </template>
              </el-table-column>
              <el-table-column prop="settledAt" label="结算时间" width="120">
                <template #default="{ row }">{{ row.settledAt ? formatDate(row.settledAt) : '-' }}</template>
              </el-table-column>
            </el-table>
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
import { getProjectById } from '@/features/master-data/projects/api/project'
import { getTransactionsByProject } from '@/features/transactions/api/transaction'
import { getReceivablesByProject } from '@/features/finance/api/receivable'
import { getPayablesByProject } from '@/features/finance/api/payable'
import PayableRecordsTable from '@/shared/ui/PayableRecordsTable.vue'
import TransactionTypeTag from '@/shared/ui/TransactionTypeTag.vue'
import TransactionTypeFilter from '@/shared/ui/TransactionTypeFilter.vue'
import { filterTransactionsByType, type TransactionTypeFilter as TypeFilter } from '@/shared/utils/transactionType'
import type { Receivable } from '@/features/finance/types/receivable'
import type { Payable } from '@/features/finance/types/payable'
import LinkDialog from '@/features/transactions/components/LinkDialog.vue'
import DetailSummaryCards, { type DetailSummaryCardItem } from '@/shared/ui/DetailSummaryCards.vue'
import SummaryOverview from '@/shared/ui/SummaryOverview.vue'
import type { Project } from '@/features/master-data/projects/types/project'
import type { Transaction } from '@/features/transactions/types/transaction'
import { LinkType } from '@/features/transactions/types/link'
import { TABLE_COLUMN_WIDTH } from '@/shared/constants/table'
import { useResizableTableColumns } from '@/shared/composables/useResizableTableColumns'
import { formatRMB, formatDateTime, formatTransactionAmount, getTransactionAmountColor } from '@/shared/utils/formatters'
import TagDisplay from '@/components/tags/TagDisplay.vue'
import TransactionDetail from '@/features/transactions/pages/TransactionDetailPage.vue'

const route = useRoute()
const router = useRouter()
const { getColumnWidth, getColumnMinWidth, handleHeaderDragend } = useResizableTableColumns('project-detail')

const loading = ref(false)
const project = ref<Project | null>(null)
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

const receivablesSummary = computed(() => {
  return {
    totalAmount: receivables.value.reduce((sum, r) => sum + r.totalAmount, 0),
    receivedAmount: receivables.value.reduce((sum, r) => sum + r.receivedAmount, 0),
    remainingAmount: receivables.value.reduce((sum, r) => sum + r.remainingAmount, 0)
  }
})

/** 计算交易在当前项目中的有效金额（分摊交易取分摊到本项目的金额） */
const getProjectAmount = (t: Transaction): number => {
  const projectId = Number(route.params.id)
  if (t.isAllocated && t.allocations?.length > 0) {
    // 分摊交易：只计当前项目分到的金额
    return t.allocations
      .filter(a => a.projectId === projectId)
      .reduce((sum, a) => sum + a.amount, 0)
  }
  return t.amount
}

/** 交易记录汇总：收入/支出/净额（分摊交易按本项目实际分到的金额计算） */
const transactionSummary = computed(() => {
  const income = transactions.value
    .filter(t => t.transactionType === 'Income')
    .reduce((sum, t) => sum + getProjectAmount(t), 0)
  const expense = transactions.value
    .filter(t => t.transactionType === 'Expense')
    .reduce((sum, t) => sum + getProjectAmount(t), 0)
  const transfer = transactions.value
    .filter(t => t.transactionType === 'Transfer')
    .reduce((sum, t) => sum + getProjectAmount(t), 0)
  return { income, expense, transfer, net: income - expense }
})

const handleLinkSuccess = () => {
  transactionsLoaded.value = false
  loadTransactions()
  payablesLoaded.value = false
}

const allocatedTransactions = computed(() => {
  return transactions.value.filter(t => t.isAllocated && t.allocations && t.allocations.length > 0)
})

const formatDate = (date: string) => formatDateTime(date, 'date')
const formatCurrency = (amount: number) => formatRMB(amount)

const getStatusType = (status?: string) => {
  const statusMap: Record<string, string> = {
    Active: 'success',
    Completed: 'info',
    Cancelled: 'danger'
  }
  return (status ? statusMap[status] : 'info') || 'info'
}

const getStatusText = (status?: string) => {
  const statusMap: Record<string, string> = {
    Active: '进行中',
    Completed: '已完成',
    Cancelled: '已取消'
  }
  return status ? (statusMap[status] || status) : '-'
}

const projectSummaryCards = computed<DetailSummaryCardItem[]>(() => [
  {
    key: 'contract',
    label: '合同金额',
    value: formatCurrency(project.value?.contractAmount || 0),
    meta: project.value?.projectCode ? `项目编号 ${project.value.projectCode}` : '当前暂无项目编号',
    tone: 'balance'
  },
  {
    key: 'received',
    label: '已确认收款',
    value: formatCurrency(project.value?.receivedAmount || 0),
    meta: `应收余额 ${formatCurrency(project.value?.receivableAmount || 0)}`,
    tone: 'income'
  },
  {
    key: 'cost',
    label: '总成本',
    value: formatCurrency(project.value?.totalCost || 0),
    meta: project.value?.status ? `项目状态 ${getStatusText(project.value.status)}` : '当前暂无项目状态',
    tone: 'expense'
  },
  {
    key: 'profit',
    label: '利润',
    value: formatCurrency(project.value?.profitAmount || 0),
    meta: project.value ? `${project.value.profitRate.toFixed(1)}% 利润率` : '当前暂无利润率',
    tone: 'profit',
    valueClass: (project.value?.profitAmount || 0) >= 0 ? 'positive' : 'negative'
  }
])

const loadProject = async () => {
  loading.value = true
  try {
    const id = Number(route.params.id)
    const { data } = await getProjectById(id)
    project.value = data.data
    await loadTransactions()
  } catch (error) {
    console.error('加载项目详情失败:', error)
  } finally {
    loading.value = false
  }
}

const loadTransactions = async () => {
  if (transactionsLoaded.value) return

  transactionsLoading.value = true
  try {
    const id = Number(route.params.id)
    const { data } = await getTransactionsByProject(id)
    transactions.value = data.data
    transactionsLoaded.value = true
  } catch (error) {
    console.error('加载交易记录失败:', error)
  } finally {
    transactionsLoading.value = false
  }
}

const loadPayables = async () => {
  if (payablesLoaded.value) return
  payablesLoading.value = true
  try {
    const id = Number(route.params.id)
    const { data } = await getPayablesByProject(id)
    payables.value = data.data
    payablesLoaded.value = true
  } catch (error) {
    ElMessage.error('加载应付记录失败')
  } finally {
    payablesLoading.value = false
  }
}

const handleTabChange = (tab: string) => {
  if ((tab === 'transactions' || tab === 'allocations') && !transactionsLoaded.value) {
    loadTransactions()
  }
  if (tab === 'receivables' && !receivablesLoaded.value) {
    loadReceivables()
  }
  if (tab === 'payables' && !payablesLoaded.value) {
    loadPayables()
  }
}

const loadReceivables = async () => {
  if (receivablesLoaded.value) return

  receivablesLoading.value = true
  try {
    const id = Number(route.params.id)
    const { data } = await getReceivablesByProject(id)
    receivables.value = data.data
    receivablesLoaded.value = true
  } catch (error) {
    console.error('加载收款计划失败:', error)
  } finally {
    receivablesLoading.value = false
  }
}


const getReceivableStatusType = (status: string) => {
  const statusMap: Record<string, string> = {
    pending: 'warning',
    partial: 'info',
    settled: 'success'
  }
  return statusMap[status] || 'info'
}

const getReceivableStatusText = (status: string) => {
  const statusMap: Record<string, string> = {
    pending: '待收款',
    partial: '部分收款',
    settled: '已结清'
  }
  return statusMap[status] || status
}

onMounted(() => {
  loadProject()
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
  padding: 24px;
  margin-bottom: 16px;
  box-shadow: 0 1px 3px rgba(0, 0, 0, 0.04);
}

.section-title {
  font-size: 14px;
  font-weight: 600;
  color: var(--text-primary);
  margin: 0 0 12px 0;
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

.amount {
  font-size: 16px;
  font-weight: 700;
  color: var(--color-primary);
}

.amount-success {
  color: var(--color-success);
  font-weight: 600;
}

.amount-warning {
  color: var(--color-warning);
  font-weight: 600;
}

.amount-danger {
  color: var(--color-danger);
  font-weight: 600;
}

.amount-primary {
  color: var(--color-primary);
  font-weight: 600;
}

.tab-section {
  background: var(--bg-card);
  border-radius: 12px;
  padding: 20px 24px;
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

.text-income {
  color: var(--color-success);
  font-weight: 600;
}

.text-expense {
  color: var(--color-danger);
  font-weight: 600;
}

.allocation-item {
  display: flex;
  align-items: center;
  gap: 8px;
  padding: 2px 0;
  font-size: 13px;
}

.alloc-person {
  color: var(--text-regular);
  font-weight: 500;
}

.alloc-amount {
  color: var(--color-primary);
  font-weight: 600;
}

.alloc-rate {
  color: var(--text-placeholder);
  font-size: 12px;
}

.alloc-desc {
  color: var(--text-placeholder);
  font-size: 12px;
}

.receivables-summary {
  margin-bottom: 16px;
}

.type-filter-row {
  margin-bottom: 12px;
}

.amount-primary {
  color: var(--color-primary);
  font-weight: 600;
  font-size: 14px;
}

.mb-4 {
  margin-bottom: 16px;
}

.clickable-rows :deep(tr) {
  cursor: pointer;
}
</style>
