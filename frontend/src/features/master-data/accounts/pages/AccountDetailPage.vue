<template>
  <div class="page-container">
    <div class="page-header">
      <div class="page-header-left">
        <el-button text @click="router.back()">
          <el-icon><ArrowLeft /></el-icon> 返回
        </el-button>
        <h2 class="page-title">账户详情</h2>
      </div>
    </div>

    <div v-loading="loading">
      <div class="info-card">
        <el-descriptions :column="3" border>
          <el-descriptions-item label="账户名称">{{ account?.name }}</el-descriptions-item>
          <el-descriptions-item label="账户类型">{{ getAccountTypeLabel(account?.accountType) }}</el-descriptions-item>
          <el-descriptions-item label="状态">
            <el-tag :type="account?.isActive ? 'success' : 'danger'">
              {{ account?.isActive ? '启用' : '禁用' }}
            </el-tag>
          </el-descriptions-item>
          <el-descriptions-item label="当前余额">
            <span class="amount">{{ formatCurrencyWithCode(account?.currentBalance ?? account?.balance ?? 0, account?.currency || 'CNY') }}</span>
          </el-descriptions-item>
          <el-descriptions-item label="初始余额">
            {{ formatCurrencyWithCode(account?.openingBalance ?? account?.balance ?? 0, account?.currency || 'CNY') }}
          </el-descriptions-item>
          <el-descriptions-item label="币种">{{ account?.currency }}</el-descriptions-item>
          <el-descriptions-item label="开户行">{{ account?.bankName || '-' }}</el-descriptions-item>
          <el-descriptions-item label="账号">{{ account?.accountNumber || '-' }}</el-descriptions-item>
          <el-descriptions-item label="描述" :span="3">{{ account?.description || '-' }}</el-descriptions-item>
          <el-descriptions-item label="创建时间">
            {{ account?.createdAt ? formatDate(account.createdAt) : '-' }}
          </el-descriptions-item>
        </el-descriptions>
      </div>

      <div class="detail-sections">
        <div v-if="isFixedDepositAccount" class="fixed-deposit-guide section-block">
          <div>
            <div class="fixed-deposit-guide__title">定期账户主档</div>
            <p class="fixed-deposit-guide__desc">
              这里用于查看账户主档和该账户名下的定期记录。真实的定期存款请在定期台账中登记，普通经营收支不再直接使用定期账户。
            </p>
            <p v-if="!account?.isActive" class="fixed-deposit-guide__hint">
              当前账户已停用，可继续查看历史记录，但不建议再作为新增定期记录的默认账户。
            </p>
          </div>
          <div class="fixed-deposit-guide__actions">
            <el-button v-if="canCreateFixedDepositRecord" type="primary" @click="openFixedDepositLedger('create')">
              登记定期记录
            </el-button>
            <el-button @click="openFixedDepositLedger()">查看定期台账</el-button>
          </div>
        </div>

        <div class="section-block section-summary">
          <SummaryOverview
            :title="summaryTitle"
            :subtitle="summarySubtitle"
            :loading="statisticsLoading"
            :empty="!accountStatistics"
          >
            <TransactionSummaryCards :statistics="accountStatistics!" />
          </SummaryOverview>
        </div>

        <div class="analysis-section section-block section-analysis">
          <div class="analysis-header">
            <div>
              <h3 class="analysis-title">{{ analysisTitle }}</h3>
              <p class="analysis-subtitle">{{ analysisSubtitle }}</p>
            </div>
          </div>

          <BalanceTrendChart
            class="chart-surface chart-surface--trend"
            :trends="balanceTrends"
            :loading="trendLoading"
            @range-change="onTrendRangeChange"
          />
        </div>

        <div class="tab-section section-block section-records">
          <el-tabs v-model="activeTab" @tab-change="handleTabChange">
            <el-tab-pane v-if="isFixedDepositAccount" label="定期记录" name="fixedDeposits">
              <div class="fixed-deposit-summary">
                <div class="summary-chip">记录数 <strong>{{ fixedDeposits.length }}</strong></div>
                <div class="summary-chip">存续中 <strong>{{ activeFixedDepositCount }}</strong></div>
                <div class="summary-chip">本金合计 <strong>{{ formatCurrencyWithCode(fixedDepositPrincipalTotal, account?.currency || 'CNY') }}</strong></div>
              </div>

              <el-table
                v-if="fixedDeposits.length > 0"
                :data="fixedDeposits"
                v-loading="fixedDepositsLoading"
                class="resizable-table"
                border
                allow-drag-last-column
                @header-dragend="handleHeaderDragend"
              >
                <el-table-column prop="depositDate" label="起息日" :width="getColumnWidth('fixedDepositDate', TABLE_COLUMN_WIDTH.date)">
                  <template #default="{ row }">{{ formatDate(row.depositDate) }}</template>
                </el-table-column>
                <el-table-column prop="maturityDate" label="到期日" :width="getColumnWidth('fixedDepositMaturityDate', TABLE_COLUMN_WIDTH.date)">
                  <template #default="{ row }">{{ formatDate(row.maturityDate) }}</template>
                </el-table-column>
                <el-table-column prop="principal" label="本金" :width="getColumnWidth('fixedDepositPrincipal', TABLE_COLUMN_WIDTH.amount)" align="right">
                  <template #default="{ row }">{{ formatCurrencyWithCode(row.principal, account?.currency || 'CNY') }}</template>
                </el-table-column>
                <el-table-column prop="termMonths" label="期限" :width="getColumnWidth('fixedDepositTermMonths', TABLE_COLUMN_WIDTH.shortText)" align="center">
                  <template #default="{ row }">{{ row.termMonths }} 个月</template>
                </el-table-column>
                <el-table-column prop="interestRate" label="利率" :width="getColumnWidth('fixedDepositInterestRate', TABLE_COLUMN_WIDTH.rate)" align="right">
                  <template #default="{ row }">{{ formatRate(row.interestRate) }}</template>
                </el-table-column>
                <el-table-column prop="expectedInterest" label="预计收益" :width="getColumnWidth('fixedDepositExpectedInterest', TABLE_COLUMN_WIDTH.amount)" align="right">
                  <template #default="{ row }">{{ formatCurrencyWithCode(row.expectedInterest, account?.currency || 'CNY') }}</template>
                </el-table-column>
                <el-table-column prop="status" label="状态" :width="getColumnWidth('fixedDepositStatus', TABLE_COLUMN_WIDTH.status)" align="center">
                  <template #default="{ row }">
                    <el-tag :type="getFixedDepositStatusTagType(row)" size="small">
                      {{ getFixedDepositStatusLabel(row) }}
                    </el-tag>
                  </template>
                </el-table-column>
                <el-table-column
                  prop="notes"
                  label="备注"
                  :min-width="getColumnMinWidth('fixedDepositNotes', TABLE_COLUMN_WIDTH.description)"
                  show-overflow-tooltip
                >
                  <template #default="{ row }">{{ row.notes || '-' }}</template>
                </el-table-column>
              </el-table>

              <div v-else-if="!fixedDepositsLoading" class="fixed-deposit-empty-state">
                <el-empty description="当前定期账户还没有登记定期记录" />
                <div class="fixed-deposit-empty-state__actions">
                  <el-button v-if="canCreateFixedDepositRecord" type="primary" @click="openFixedDepositLedger('create')">
                    登记第一笔定期记录
                  </el-button>
                  <el-button @click="openFixedDepositLedger()">前往定期台账</el-button>
                </div>
                <p class="fixed-deposit-empty-state__tip">
                  新建账户只会建立“定期账户主档”，真实的本金、期限和利率需要在定期记录里单独登记。
                </p>
              </div>
            </el-tab-pane>
            <el-tab-pane label="交易记录" name="transactions">
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
                <el-table-column prop="categoryName" label="分类" :width="getColumnWidth('categoryName', TABLE_COLUMN_WIDTH.category)">
                  <template #default="{ row }">
                    <el-link v-if="row.categoryId" type="primary" @click.stop="router.push({ name: 'Transactions', query: { categoryId: String(row.categoryId) } })">
                      {{ row.categoryName }}
                    </el-link>
                    <span v-else>{{ row.categoryName || '-' }}</span>
                  </template>
                </el-table-column>
                <el-table-column prop="projectName" label="项目" :width="getColumnWidth('projectName', TABLE_COLUMN_WIDTH.project)">
                  <template #default="{ row }">
                    <el-link v-if="row.projectId" type="primary" @click.stop="router.push({ name: 'ProjectDetail', params: { id: row.projectId } })">
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
          </el-tabs>
        </div>
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
import { ArrowLeft } from '@element-plus/icons-vue'
import { getAccountById, getAccountBalanceTrend } from '@/features/master-data/accounts/api/account'
import { getFixedDepositsByAccount } from '@/features/master-data/fixed-deposits/api/fixedDeposit'
import { getTransactionsByAccount, getAccountTransactionStatistics } from '@/features/transactions/api/transaction'
import BalanceTrendChart from '@/features/transactions/components/BalanceTrendChart.vue'
import SummaryOverview from '@/shared/ui/SummaryOverview.vue'
import TransactionSummaryCards from '@/features/transactions/components/TransactionSummaryCards.vue'
import TagDisplay from '@/components/tags/TagDisplay.vue'
import TransactionDetail from '@/features/transactions/pages/TransactionDetailPage.vue'
import TransactionTypeTag from '@/shared/ui/TransactionTypeTag.vue'
import TransactionTypeFilter from '@/shared/ui/TransactionTypeFilter.vue'
import { filterTransactionsByType, type TransactionTypeFilter as TypeFilter } from '@/shared/utils/transactionType'
import { TABLE_COLUMN_WIDTH } from '@/shared/constants/table'
import { useResizableTableColumns } from '@/shared/composables/useResizableTableColumns'
import { useDetailPageStatistics } from '@/shared/composables/useDetailPageStatistics'
import { useUserStore } from '@/features/auth/stores/user'
import type { Account, BalanceTrendItem } from '@/features/master-data/accounts/types/account'
import type { FixedDeposit } from '@/features/master-data/fixed-deposits/types/fixedDeposit'
import type { Transaction, TransactionStatistics } from '@/features/transactions/types/transaction'
import { formatDateTime, formatMoney, formatTransactionAmount, getTransactionAmountColor } from '@/shared/utils/formatters'
import dayjs from 'dayjs'

const route = useRoute()
const router = useRouter()
const userStore = useUserStore()
const { getColumnWidth, getColumnMinWidth, handleHeaderDragend } = useResizableTableColumns('account-detail-transactions')

const loading = ref(false)
const account = ref<Account | null>(null)
const activeTab = ref('transactions')
const transactions = ref<Transaction[]>([])
const transactionsLoading = ref(false)
const transactionsLoaded = ref(false)
const typeFilter = ref<TypeFilter>('all')
const filteredTransactions = computed(() => filterTransactionsByType(transactions.value, typeFilter.value))
const fixedDeposits = ref<FixedDeposit[]>([])
const fixedDepositsLoading = ref(false)
const fixedDepositsLoaded = ref(false)

const detailVisible = ref(false)
const currentTransactionId = ref(0)

const handleViewTransaction = (row: Transaction) => {
  currentTransactionId.value = row.id
  detailVisible.value = true
}

const balanceTrends = ref<BalanceTrendItem[]>([])
const trendLoading = ref(false)

const accountId = computed(() => Number(route.params.id) || null)

const { statistics: accountStatistics, statisticsLoading, refreshStatistics } =
  useDetailPageStatistics<TransactionStatistics>({
    entityId: accountId,
    fetchStatistics: getAccountTransactionStatistics,
    initialStatistics: {
      totalIncome: 0,
      totalExpense: 0,
      netProfit: 0,
      totalTransfer: 0,
      incomeCount: 0,
      expenseCount: 0,
      transferCount: 0,
      totalCount: 0
    },
    onRefresh: () => {
      transactionsLoaded.value = false
      loadTransactions()
    }
  })

const handleLinkSuccess = () => {
  refreshStatistics()
}
const isFixedDepositAccount = computed(() => account.value?.accountType === 'FixedDeposit')
const activeFixedDepositCount = computed(() => fixedDeposits.value.filter(item => getFixedDepositStatus(item) === 'Active').length)
const fixedDepositPrincipalTotal = computed(() => fixedDeposits.value.reduce((sum, item) => sum + Number(item.principal || 0), 0))
const canCreateFixedDepositRecord = computed(() => {
  return Boolean(
    isFixedDepositAccount.value
    && account.value?.id
    && account.value.isActive
    && userStore.canEdit
    && router.hasRoute('FixedDeposits')
  )
})
const summaryTitle = computed(() => isFixedDepositAccount.value ? '关联交易概览' : '收支概览')
const summarySubtitle = computed(() => {
  if (!accountStatistics.value) {
    return isFixedDepositAccount.value
      ? '定期账户以定期记录为主，这里仅辅助查看关联转账和历史流水。'
      : '快速查看收入、支出和净收益表现'
  }

  const transferText = accountStatistics.value.transferCount > 0
    ? `，其中转账 ${accountStatistics.value.transferCount} 笔`
    : ''

  if (isFixedDepositAccount.value) {
    return `共 ${accountStatistics.value.totalCount} 笔关联记录${transferText}，用于辅助查看定期相关的资金转入、支取和内部划转。`
  }

  return `共 ${accountStatistics.value.totalCount} 笔交易${transferText}`
})
const analysisTitle = computed(() => isFixedDepositAccount.value ? '关联流水趋势' : '资金分析')
const analysisSubtitle = computed(() => {
  return isFixedDepositAccount.value
    ? '辅助查看定期账户相关的资金流转与历史余额波动'
    : '趋势图和收支分布的可视化分析'
})

const loadAccount = async () => {
  loading.value = true
  try {
    const id = Number(route.params.id)
    const { data } = await getAccountById(id)
    account.value = data.data
    transactions.value = []
    transactionsLoaded.value = false
    fixedDeposits.value = []
    fixedDepositsLoaded.value = false
    activeTab.value = data.data.accountType === 'FixedDeposit' ? 'fixedDeposits' : 'transactions'

    const loaders = [loadBalanceTrend(6)]
    if (activeTab.value === 'fixedDeposits') {
      loaders.push(loadFixedDeposits())
    } else {
      loaders.push(loadTransactions())
    }

    await Promise.all(loaders)
  } catch (error) {
    console.error('加载账户详情失败:', error)
  } finally {
    loading.value = false
  }
}

const loadTransactions = async () => {
  if (transactionsLoaded.value) return

  transactionsLoading.value = true
  try {
    const id = Number(route.params.id)
    const { data } = await getTransactionsByAccount(id)
    transactions.value = data.data
    transactionsLoaded.value = true
  } catch (error) {
    console.error('加载交易记录失败:', error)
  } finally {
    transactionsLoading.value = false
  }
}

const loadFixedDeposits = async () => {
  if (!isFixedDepositAccount.value || fixedDepositsLoaded.value) return

  fixedDepositsLoading.value = true
  try {
    const id = Number(route.params.id)
    const { data } = await getFixedDepositsByAccount(id)
    fixedDeposits.value = data.data
    fixedDepositsLoaded.value = true
  } catch (error) {
    console.error('加载定期记录失败:', error)
    fixedDeposits.value = []
  } finally {
    fixedDepositsLoading.value = false
  }
}

const loadBalanceTrend = async (months: number) => {
  trendLoading.value = true
  try {
    const id = Number(route.params.id)
    const { data } = await getAccountBalanceTrend(id, months)
    balanceTrends.value = data.data.trends
  } catch (error) {
    console.error('加载余额趋势失败:', error)
    balanceTrends.value = []
  } finally {
    trendLoading.value = false
  }
}

const onTrendRangeChange = (months: number) => {
  loadBalanceTrend(months)
}

const handleTabChange = (tab: string) => {
  if (tab === 'transactions' && !transactionsLoaded.value) {
    loadTransactions()
  }
  if (tab === 'fixedDeposits' && !fixedDepositsLoaded.value) {
    loadFixedDeposits()
  }
}

const openFixedDepositLedger = (action?: 'create') => {
  const accountId = account.value?.id
  if (!accountId || !router.hasRoute('FixedDeposits')) return

  const query: Record<string, string> = {
    accountId: String(accountId)
  }

  if (action === 'create') {
    query.action = 'create'
  }

  void router.push({ name: 'FixedDeposits', query })
}

const getAccountTypeLabel = (type?: string) => {
  const map: Record<string, string> = {
    Bank: '银行账户',
    Alipay: '支付宝账户',
    FixedDeposit: '定期账户'
  }
  return type ? (map[type] || type) : '-'
}

const getFixedDepositStatus = (item: FixedDeposit) => {
  if (item.status === 'Withdrawn') return 'Withdrawn'
  if (dayjs(item.maturityDate).isBefore(dayjs(), 'day')) return 'Matured'
  return 'Active'
}

const getFixedDepositStatusLabel = (item: FixedDeposit) => {
  const status = getFixedDepositStatus(item)
  if (status === 'Withdrawn') return '已支取'
  if (status === 'Matured') return '已到期'
  return '存续中'
}

const getFixedDepositStatusTagType = (item: FixedDeposit) => {
  const status = getFixedDepositStatus(item)
  if (status === 'Withdrawn') return 'info'
  if (status === 'Matured') return 'warning'
  return 'success'
}

const formatDate = (date: string) => formatDateTime(date, 'date')
const formatCurrencyWithCode = (amount: number | null | undefined, currency: string) => `${currency} ${formatMoney(Number(amount ?? 0))}`
const formatRate = (value?: number) => value == null ? '-' : `${Number(value).toFixed(2)}%`

onMounted(() => {
  loadAccount()
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
  margin: 0;
  font-size: 20px;
  font-weight: 600;
  color: var(--text-primary);
}

.info-card {
  background: var(--bg-card);
  border-radius: 12px;
  padding: 24px;
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

.amount {
  font-size: 16px;
  font-weight: 700;
  color: var(--color-primary);
}

.detail-sections {
  display: flex;
  flex-direction: column;
  gap: 16px;
}

.fixed-deposit-guide {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 20px;
  padding: 20px 24px;
  border-radius: 12px;
  border: 1px solid rgba(217, 119, 6, 0.18);
  background: linear-gradient(135deg, rgba(251, 191, 36, 0.12), rgba(245, 158, 11, 0.04));
}

.fixed-deposit-guide__title {
  font-size: 16px;
  font-weight: 700;
  color: var(--text-primary);
}

.fixed-deposit-guide__desc,
.fixed-deposit-guide__hint {
  margin: 8px 0 0;
  font-size: 13px;
  line-height: 1.6;
  color: var(--text-secondary);
}

.fixed-deposit-guide__actions {
  display: flex;
  align-items: center;
  gap: 12px;
  flex-shrink: 0;
}

.analysis-section {
  background: var(--bg-card);
  border-radius: 12px;
  padding: 20px 24px;
  box-shadow: 0 1px 3px rgba(0, 0, 0, 0.04);
}

.analysis-header {
  display: flex;
  justify-content: space-between;
  align-items: flex-start;
  gap: 16px;
  margin-bottom: 16px;
}

.analysis-title {
  margin: 0;
  font-size: 16px;
  font-weight: 600;
  color: var(--text-primary);
}

.analysis-subtitle {
  margin: 6px 0 0;
  font-size: 12px;
  line-height: 1.5;
  color: var(--text-secondary);
}

.chart-surface {
  height: 100%;
  margin: 0;
}

.chart-surface--trend {
  min-width: 0;
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

.type-filter-row {
  margin-bottom: 12px;
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
  --el-table-border-color: var(--border-light);
  --el-table-header-bg-color: var(--bg-page);
  --el-table-header-text-color: var(--text-secondary);
  --el-table-text-color: var(--text-regular);
  --el-table-row-hover-bg-color: var(--bg-hover);
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

.fixed-deposit-summary {
  display: flex;
  flex-wrap: wrap;
  gap: 10px;
  margin-bottom: 16px;
}

.summary-chip {
  display: inline-flex;
  align-items: center;
  gap: 6px;
  padding: 8px 12px;
  border: 1px solid var(--border-light);
  border-radius: 999px;
  background: var(--bg-page);
  font-size: 12px;
  color: var(--text-secondary);
}

.summary-chip strong {
  color: var(--text-primary);
  font-weight: 600;
}

.fixed-deposit-empty-state {
  padding: 12px 0 4px;
}

.fixed-deposit-empty-state__actions {
  display: flex;
  justify-content: center;
  gap: 12px;
  flex-wrap: wrap;
}

.fixed-deposit-empty-state__tip {
  margin: 12px auto 0;
  max-width: 560px;
  font-size: 13px;
  line-height: 1.6;
  color: var(--text-secondary);
  text-align: center;
}

.text-income {
  color: var(--color-success);
  font-weight: 600;
}

.text-expense {
  color: var(--color-danger);
  font-weight: 600;
}

.clickable-rows :deep(tr) {
  cursor: pointer;
}

@media (max-width: 768px) {
  .fixed-deposit-guide {
    flex-direction: column;
    align-items: flex-start;
  }

  .fixed-deposit-guide__actions {
    width: 100%;
    flex-wrap: wrap;
  }

  .analysis-section,
  .fixed-deposit-guide,
  .info-card,
  .tab-section {
    padding: 16px;
  }
}
</style>
