<template>
  <div class="page-container">
    <PageHeader
      title="交易管理"
      description="查看和管理所有收支与转账记录"
      :actions="headerActions"
      @action="handleHeaderAction"
    >
      <template #primary>
        <el-button v-if="userStore.canEdit" type="primary" @click="handleCreate">新增交易</el-button>
      </template>
    </PageHeader>

    <el-row :gutter="isMobile ? 12 : 24" class="stat-cards">
      <el-col :xs="12" :sm="12" :md="6">
        <StatCard
          :icon="TrendCharts"
          :value="formatCurrency(statistics.totalIncome)"
          label="总收入"
          :count="`${statistics.incomeCount} 笔`"
          theme="income"
        />
      </el-col>
      <el-col :xs="12" :sm="12" :md="6">
        <StatCard
          :icon="Minus"
          :value="formatCurrency(statistics.totalExpense)"
          label="总支出"
          :count="`${statistics.expenseCount} 笔`"
          theme="expense"
        />
      </el-col>
      <el-col :xs="12" :sm="12" :md="6">
        <StatCard
          :icon="Coin"
          :value="formatCurrency(statistics.netProfit)"
          label="净收益"
          :count="`${statistics.totalCount} 笔总计`"
          theme="profit"
        />
      </el-col>
      <el-col :xs="12" :sm="12" :md="6">
        <StatCard
          :icon="Sort"
          :value="formatCurrency(statistics.totalTransfer)"
          label="转账总额"
          :count="`${statistics.transferCount} 笔`"
          theme="transfer"
        />
      </el-col>
    </el-row>

    <ActiveTagFilters
      :tag-ids="filters.tagIds"
      scope="transaction"
      @remove="removeTagFilter"
      @clear="clearTagFilters"
    />

    <FilterDrawer
      v-model="filterDrawerVisible"
      :active-count="activeFilterCount"
      @apply="handleFilter"
      @reset="handleReset"
    >
    <div class="search-section search-section-top">
      <el-form :inline="true" :model="filters" class="search-form" @submit.prevent="handleFilter">
        <el-form-item label="账户">
          <SearchableSelect
            v-model="filters.accountId"
            :options="accountStore.items"
            entity-name="账户"
            width="200px"
          />
        </el-form-item>
        <el-form-item label="分类">
          <SearchableSelect
            v-model="filters.categoryId"
            :options="categoryStore.items"
            entity-name="分类"
            width="180px"
          />
        </el-form-item>
        <el-form-item label="项目">
          <SearchableSelect
            v-model="filters.projectId"
            :options="projectStore.items"
            entity-name="项目"
            width="200px"
          />
        </el-form-item>
        <el-form-item label="日期范围">
          <el-date-picker
            v-model="filters.dateRange"
            type="daterange"
            range-separator="至"
            start-placeholder="开始日期"
            end-placeholder="结束日期"
            :shortcuts="dateRangeShortcuts"
          />
        </el-form-item>
        <el-form-item label="标签">
          <TagSelector
            :model-value="filters.tagIds"
            scope="transaction"
            @change="handleTagFilterChange"
            placeholder="按标签筛选"
          />
        </el-form-item>
        <el-form-item class="search-buttons">
          <el-button type="primary" native-type="submit">查询</el-button>
          <el-button @click="handleReset">重置</el-button>
        </el-form-item>
      </el-form>
    </div>
    </FilterDrawer>

    <div class="content-section">
      <el-tabs v-model="activeTab" class="transaction-tabs" @tab-change="handleTabChange">
        <el-tab-pane label="全部" name="all" />
        <el-tab-pane label="收入" name="Income" />
        <el-tab-pane label="支出" name="Expense" />
        <el-tab-pane label="转账" name="Transfer" />
      </el-tabs>

      <ResponsiveList :items="transactions" :loading="loading">
        <template #table>
        <div class="table-wrapper">
        <el-table
          :data="transactions"
          v-loading="loading"
          class="resizable-table"
          border
          allow-drag-last-column
          @header-dragend="handleHeaderDragend"
          @sort-change="handleSortChange"
        >
          <el-table-column prop="transactionDate" label="交易日期" sortable="custom" :width="getColumnWidth('transactionDate', TABLE_COLUMN_WIDTH.date)">
            <template #default="{ row }">
              {{ formatDateTime(row.transactionDate, 'date') }}
            </template>
          </el-table-column>

          <el-table-column prop="transactionTime" label="交易时间" :width="getColumnWidth('transactionTime', 100)">
            <template #default="{ row }">
              {{ formatTime(row.transactionTime) }}
            </template>
          </el-table-column>

          <el-table-column prop="transactionNumber" label="流水号" :width="getColumnWidth('transactionNumber', 160)" show-overflow-tooltip>
            <template #default="{ row }">
              {{ row.transactionNumber || '-' }}
            </template>
          </el-table-column>

          <el-table-column prop="amount" label="金额" sortable="custom" :width="getColumnWidth('amount', TABLE_COLUMN_WIDTH.amount)" align="right">
            <template #default="{ row }">
              <span :style="{ color: getTransactionAmountColor(row.transactionType) }">
                {{ formatTransactionAmount(row.amount, row.transactionType) }}
              </span>
            </template>
          </el-table-column>

          <el-table-column prop="description" label="描述" :min-width="getColumnMinWidth('description', TABLE_COLUMN_WIDTH.description)" show-overflow-tooltip>
            <template #default="{ row }">
              <span v-if="row.transactionType === 'Transfer' && row.relatedAccountName">
                {{ row.description || '-' }}
                <el-tag size="small" type="info" style="margin-left: 8px">
                  {{ getTransferLabel(row) }}
                </el-tag>
              </span>
              <span v-else>{{ row.description || '-' }}</span>
            </template>
          </el-table-column>

          <el-table-column prop="memo" label="摘要" :min-width="getColumnMinWidth('memo', TABLE_COLUMN_WIDTH.description)" show-overflow-tooltip>
            <template #default="{ row }">
              {{ row.memo || '-' }}
            </template>
          </el-table-column>

          <el-table-column prop="categoryName" label="分类" :width="getColumnWidth('categoryName', TABLE_COLUMN_WIDTH.category)">
            <template #default="{ row }">
              <el-link
                v-if="row.categoryId"
                type="primary"
                @click="router.push({ name: 'Transactions', query: { categoryId: String(row.categoryId) } })"
              >
                {{ row.categoryName }}
              </el-link>
              <span v-else>{{ row.categoryName || '-' }}</span>
            </template>
          </el-table-column>

          <el-table-column prop="projectName" label="项目" :width="getColumnWidth('projectName', TABLE_COLUMN_WIDTH.project)">
            <template #default="{ row }">
              <el-link v-if="row.projectId" type="primary" @click="router.push({ name: 'ProjectDetail', params: { id: row.projectId } })">
                {{ row.projectName }}
              </el-link>
              <span v-else>{{ row.projectName || '-' }}</span>
            </template>
          </el-table-column>

          <el-table-column prop="accountName" label="账户" :width="getColumnWidth('accountName', TABLE_COLUMN_WIDTH.account)">
            <template #default="{ row }">
              <el-link v-if="row.accountId" type="primary" @click="router.push({ name: 'AccountDetail', params: { id: row.accountId } })">
                {{ row.accountName }}
              </el-link>
              <span v-else>-</span>
            </template>
          </el-table-column>

          <el-table-column column-key="counterparty" label="对方" :width="getColumnWidth('counterparty', TABLE_COLUMN_WIDTH.company)">
            <template #default="{ row }">
              <el-link v-if="row.customerId" type="primary" @click="router.push({ name: 'CustomerDetail', params: { id: row.customerId } })">
                {{ row.customerName }}
              </el-link>
              <el-link v-else-if="row.supplierId" type="primary" @click="router.push({ name: 'SupplierDetail', params: { id: row.supplierId } })">
                {{ row.supplierName }}
              </el-link>
              <el-link v-else-if="row.personId" type="primary" @click="router.push({ name: 'PersonDetail', params: { id: row.personId } })">
                {{ row.personName }}
              </el-link>
              <span v-else>{{ row.counterparty || '-' }}</span>
            </template>
          </el-table-column>

          <el-table-column prop="counterpartyAccount" label="对方账户" :width="getColumnWidth('counterpartyAccount', 150)" show-overflow-tooltip>
            <template #default="{ row }">
              {{ row.counterpartyAccount || '-' }}
            </template>
          </el-table-column>

          <el-table-column prop="counterpartyBank" label="对方银行" :width="getColumnWidth('counterpartyBank', 130)" show-overflow-tooltip>
            <template #default="{ row }">
              {{ row.counterpartyBank || '-' }}
            </template>
          </el-table-column>

          <el-table-column column-key="allocation" label="分摊" :width="getColumnWidth('allocation', TABLE_COLUMN_WIDTH.type)" align="center">
            <template #default="{ row }">
              <el-tag v-if="row.isAllocated" type="warning" size="small">已分摊</el-tag>
              <span v-else>-</span>
            </template>
          </el-table-column>

          <el-table-column label="标签" :width="getColumnWidth('tags', 180)">
            <template #default="{ row }">
              <TagDisplay :tags="row.tags" size="small" :max-display="2" />
            </template>
          </el-table-column>

          <el-table-column column-key="actions" label="操作" :width="getColumnWidth('actions', TABLE_COLUMN_WIDTH.actionThree)" fixed="right">
            <template #default="{ row }">
              <el-button link type="primary" size="small" @click="handleView(row)">详情</el-button>
              <el-button v-if="userStore.canEdit" link type="primary" size="small" @click="handleManageTags(row)">标签</el-button>
              <el-button v-if="userStore.canEdit && row.transactionType !== 'Transfer'" link type="primary" size="small" @click="handleEdit(row)">编辑</el-button>
              <el-button v-if="canConvertToTransfer(row)" link type="primary" size="small" @click="handleConvertToTransfer(row)">标记为内部转账</el-button>
              <el-button v-if="userStore.canDelete" link type="danger" size="small" @click="handleDelete(row)">删除</el-button>
            </template>
          </el-table-column>
        </el-table>
        </div>
        </template>
        <template #card="{ item }">
          <MobileListCard
            :title="getTransactionTitle(item)"
            :amount="item.amount"
            :amount-type="getAmountTone(item.transactionType)"
            @click="handleView(item)"
          >
            <template #tag>
              <TransactionTypeTag :transaction-type="item.transactionType" />
            </template>
            <template #meta>
              {{ joinMeta(formatDateTime(item.transactionDate, 'date'), item.accountName, item.categoryName) }}
            </template>
            <template #footer>
              <TagDisplay :tags="item.tags" size="small" :max-display="3" />
              <span v-if="item.description && getTransactionTitle(item) !== item.description" class="card-desc">
                {{ item.description }}
              </span>
            </template>
          </MobileListCard>
        </template>
        <template #pagination>
          <el-pagination
            v-model:current-page="pagination.page"
            v-model:page-size="pagination.pageSize"
            :total="pagination.total"
            :page-sizes="[10, 20, 50, 100]"
            :layout="isMobile ? 'total, prev, pager, next' : 'total, sizes, prev, pager, next, jumper'"
            class="pagination"
            @size-change="handleSizeChange"
            @current-change="handlePageChange"
          />
        </template>
      </ResponsiveList>
    </div>

    <TransactionForm
      v-model:visible="formVisible"
      :transaction="currentTransaction"
      @success="handleFormSuccess"
    />

    <TransactionDetail
      v-model:visible="detailVisible"
      :transaction-id="currentTransactionId"
    />

    <TransferDialog
      v-model="transferVisible"
      :accounts="accountList"
      @success="handleTransferSuccess"
    />

    <ConvertTransactionToTransferDialog
      v-model="convertTransferVisible"
      :transaction="currentConvertTransaction"
      :accounts="accountList"
      @success="handleConvertTransferSuccess"
    />

    <BatchLinkDialog
      v-model="batchLinkVisible"
      @success="handleBatchLinkSuccess"
    />

    <TagEditorDialog
      v-model:visible="tagDialogVisible"
      owner-type="transaction"
      :owner-id="tagEditTarget?.id ?? null"
      :owner-name="tagEditTarget?.name"
      @saved="loadTransactions"
    />
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted, reactive, ref, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { ElMessage, ElMessageBox } from 'element-plus'
import { TrendCharts, Minus, Coin, Sort } from '@element-plus/icons-vue'
import { getTransactions, deleteTransaction, getTransactionStatistics } from '@/features/transactions/api/transaction'
import { dateRangeShortcuts } from '@/shared/utils/dateShortcuts'
import { formatDateTime, formatCurrency, formatTransactionAmount, getAmountTone, getTransactionAmountColor } from '@/shared/utils/formatters'
import { getCounterpartyLabel, joinMeta } from '@/shared/utils/recordDisplay'
import { useBreakpoint } from '@/shared/composables/useBreakpoint'
import { useUserStore } from '@/features/auth/stores/user'
import { useAccountStore } from '@/features/master-data/accounts/stores/account'
import { useCategoryStore } from '@/features/master-data/categories/stores/category'
import { useProjectStore } from '@/features/master-data/projects/stores/project'
import type { PageRequest } from '@/shared/types/common'
import type { Transaction, TransactionStatistics } from '@/features/transactions/types/transaction'
import type { Account } from '@/features/master-data/accounts/types/account'
import SearchableSelect from '@/shared/ui/SearchableSelect.vue'
import TagSelector from '@/components/tags/TagSelector.vue'
import TagDisplay from '@/components/tags/TagDisplay.vue'
import ActiveTagFilters from '@/components/tags/ActiveTagFilters.vue'
import StatCard from '@/shared/ui/StatCard.vue'
import PageHeader from '@/shared/ui/PageHeader.vue'
import FilterDrawer from '@/shared/ui/FilterDrawer.vue'
import ResponsiveList from '@/shared/ui/ResponsiveList.vue'
import MobileListCard from '@/shared/ui/MobileListCard.vue'
import TransactionTypeTag from '@/shared/ui/TransactionTypeTag.vue'
import TransferDialog from '@/features/transactions/components/TransferDialog.vue'
import ConvertTransactionToTransferDialog from '@/features/transactions/components/ConvertTransactionToTransferDialog.vue'
import { TABLE_COLUMN_WIDTH } from '@/shared/constants/table'
import { useResizableTableColumns } from '@/shared/composables/useResizableTableColumns'
import TransactionForm from '@/features/transactions/components/TransactionForm.vue'
import TransactionDetail from '@/features/transactions/pages/TransactionDetailPage.vue'
import BatchLinkDialog from '@/shared/ui/BatchLinkDialog.vue'
import TagEditorDialog from '@/components/tags/TagEditorDialog.vue'

const route = useRoute()
const router = useRouter()
const userStore = useUserStore()
const { isMobile } = useBreakpoint()
const filterDrawerVisible = ref(false)
const accountStore = useAccountStore()
const categoryStore = useCategoryStore()
const projectStore = useProjectStore()
const { getColumnWidth, getColumnMinWidth, handleHeaderDragend } = useResizableTableColumns('transaction-list')

const loading = ref(false)
const transactions = ref<Transaction[]>([])
const formVisible = ref(false)
const detailVisible = ref(false)
const transferVisible = ref(false)
const batchLinkVisible = ref(false)
const convertTransferVisible = ref(false)
const currentTransaction = ref<Transaction | null>(null)
const currentConvertTransaction = ref<Transaction | null>(null)
const currentTransactionId = ref(0)
const accountList = ref<Account[]>([])
const activeTab = ref('all')
const tagDialogVisible = ref(false)
const tagEditTarget = ref<{ id: number; name: string } | null>(null)

const sortState = reactive({
  sortBy: '',
  sortOrder: '' as '' | 'asc' | 'desc'
})

const handleSortChange = ({ prop, order }: { prop: string; order: string | null }) => {
  sortState.sortBy = order ? prop : ''
  sortState.sortOrder = order === 'ascending' ? 'asc' : order === 'descending' ? 'desc' : ''
  pagination.page = 1
  loadTransactions()
}

const statistics = ref<TransactionStatistics>({
  totalIncome: 0,
  totalExpense: 0,
  netProfit: 0,
  totalTransfer: 0,
  incomeCount: 0,
  expenseCount: 0,
  transferCount: 0,
  totalCount: 0
})

const filters = reactive({
  accountId: null as number | null,
  categoryId: null as number | null,
  projectId: null as number | null,
  dateRange: null as [Date, Date] | null,
  tagIds: [] as number[]
})

const headerActions = computed(() => {
  if (!userStore.canEdit) return []
  return [
    { label: '账户转账', command: 'transfer' },
    { label: '批量智能关联', command: 'batchLink', type: 'warning' as const }
  ]
})

const activeFilterCount = computed(() => {
  let count = 0
  if (filters.accountId) count++
  if (filters.categoryId) count++
  if (filters.projectId) count++
  if (filters.dateRange) count++
  if (filters.tagIds.length) count++
  return count
})

const handleHeaderAction = (command: string) => {
  if (command === 'transfer') transferVisible.value = true
  if (command === 'batchLink') batchLinkVisible.value = true
}

const getTransactionTitle = (row: Transaction) =>
  getCounterpartyLabel(row) || row.description || row.memo || '未命名交易'

const pagination = reactive({
  page: 1,
  pageSize: 20,
  total: 0
})

const parseNumberQuery = (value: unknown) => {
  const raw = Array.isArray(value) ? value[0] : value
  const parsed = Number(raw)
  return Number.isFinite(parsed) && parsed > 0 ? parsed : null
}

const applyRouteFilters = () => {
  filters.accountId = parseNumberQuery(route.query.accountId)
  filters.categoryId = parseNumberQuery(route.query.categoryId)
  filters.projectId = parseNumberQuery(route.query.projectId)
  const tagId = parseNumberQuery(route.query.tagId)
  filters.tagIds = tagId ? [tagId] : []
}

const buildFilterParams = (): Partial<PageRequest> => {
  const params: Partial<PageRequest> = {}

  if (filters.accountId) params.accountId = filters.accountId
  if (filters.categoryId) params.categoryId = filters.categoryId
  if (filters.projectId) params.projectId = filters.projectId
  if (filters.dateRange?.length === 2) {
    params.startDate = formatDateTime(filters.dateRange[0], 'date')
    params.endDate = formatDateTime(filters.dateRange[1], 'date')
  }
  if (activeTab.value !== 'all') {
    params.transactionType = activeTab.value
  }
  if (filters.tagIds.length > 0) {
    params.tagFilters = [{ scope: 'transaction', tagIds: filters.tagIds, matchMode: 'or' }]
  }

  return params
}

const loadStatistics = async () => {
  try {
    const { data } = await getTransactionStatistics(buildFilterParams())
    statistics.value = data.data
  } catch (error) {
    console.error('加载交易统计失败', error)
  }
}

const loadTransactions = async () => {
  loading.value = true
  try {
    const params: PageRequest = {
      page: pagination.page,
      pageSize: pagination.pageSize,
      ...buildFilterParams()
    }
    if (sortState.sortBy) {
      params.sortBy = sortState.sortBy
      params.sortOrder = sortState.sortOrder || undefined
    }

    const { data } = await getTransactions(params)
    transactions.value = data.data.items
    pagination.total = data.data.total
  } catch (error) {
    ElMessage.error('加载交易列表失败')
  } finally {
    loading.value = false
  }
}

const loadAccounts = async (force = false) => {
  await accountStore.loadOptions(force)
  accountList.value = accountStore.items
}

const loadCategories = async () => {
  await categoryStore.loadOptions()
}

const loadProjects = async () => {
  await projectStore.loadOptions()
}

const handleTabChange = () => {
  pagination.page = 1
  loadTransactions()
  loadStatistics()
}

const handleCreate = () => {
  currentTransaction.value = null
  formVisible.value = true
}

const handleEdit = (row: Transaction) => {
  currentTransaction.value = row
  formVisible.value = true
}

const handleView = (row: Transaction) => {
  currentTransactionId.value = row.id
  detailVisible.value = true
}

const handleManageTags = (row: Transaction) => {
  tagEditTarget.value = { id: row.id, name: row.description || '交易#' + row.id }
  tagDialogVisible.value = true
}

const handleConvertToTransfer = (row: Transaction) => {
  currentConvertTransaction.value = row
  convertTransferVisible.value = true
}

const handleDelete = async (row: Transaction) => {
  try {
    await ElMessageBox.confirm('确定要删除这条交易记录吗？', '提示', {
      confirmButtonText: '确定',
      cancelButtonText: '取消',
      type: 'warning'
    })

    await deleteTransaction(row.id)
    ElMessage.success('删除成功')
    loadTransactions()
    loadStatistics()
  } catch (error) {
    if (error !== 'cancel') {
      // API 错误已由请求拦截器统一提示
    }
  }
}

const handleFilter = () => {
  pagination.page = 1
  loadTransactions()
  loadStatistics()
}

const handleTagFilterChange = (tagIds: number[]) => {
  filters.tagIds = tagIds
  handleFilter()
}

const handleReset = async () => {
  filters.accountId = null
  filters.categoryId = null
  filters.projectId = null
  filters.dateRange = null
  filters.tagIds = []
  activeTab.value = 'all'
  await router.replace({ name: 'Transactions' })
  handleFilter()
}

const handleSizeChange = () => {
  pagination.page = 1
  loadTransactions()
}

const handlePageChange = () => {
  loadTransactions()
}

const handleFormSuccess = () => {
  formVisible.value = false
  loadTransactions()
  loadStatistics()
}

const handleTransferSuccess = () => {
  loadTransactions()
  loadStatistics()
  loadAccounts(true)
}

const handleBatchLinkSuccess = () => {
  loadTransactions()
  loadStatistics()
}

const handleConvertTransferSuccess = () => {
  convertTransferVisible.value = false
  currentConvertTransaction.value = null
  loadTransactions()
  loadStatistics()
  loadAccounts(true)
}

const getTransferLabel = (row: Transaction) => {
  if (!row.relatedAccountName) return ''
  const direction = row.transferDirection
    ?? (row.description?.includes('转账至') ? 'Out' : row.description?.includes('转账自') ? 'In' : undefined)

  return direction === 'Out'
    ? `→ ${row.relatedAccountName}`
    : `← ${row.relatedAccountName}`
}

const canConvertToTransfer = (row: Transaction) => {
  return userStore.canEdit && row.transactionType !== 'Transfer'
}

const removeTagFilter = (tagId: number) => {
  filters.tagIds = filters.tagIds.filter(id => id !== tagId)
  handleFilter()
}

const clearTagFilters = () => {
  filters.tagIds = []
  handleFilter()
}

const formatTime = (value?: string) => {
  if (!value) return '-'
  // TimeSpan 格式如 "HH:mm:ss" 或 "HH:mm:ss.fffffff"
  const parts = value.split(':')
  if (parts.length >= 2) return `${parts[0]}:${parts[1]}${parts[2] ? ':' + parts[2].split('.')[0] : ''}`
  return value
}

onMounted(() => {
  applyRouteFilters()
  loadStatistics()
  loadTransactions()
  loadAccounts()
  loadCategories()
  loadProjects()
})

watch(() => route.query, () => {
  applyRouteFilters()
  loadTransactions()
  loadStatistics()
})
</script>

<style scoped>
.page-container {
  padding: 0;
}

.card-desc {
  display: block;
  margin-top: 4px;
}

.stat-cards {
  margin-bottom: 24px;
}

.search-section-top {
  background: var(--bg-page);
  border-radius: 8px;
  padding: 16px;
  margin-bottom: 20px;
}

.search-form {
  margin-bottom: 0 !important;
  display: flex;
  flex-wrap: wrap;
  gap: 0;
}

.search-buttons {
  margin-left: auto !important;
}

.search-form :deep(.el-form-item) {
  margin-bottom: 0 !important;
}

.search-section-top :deep(.el-form-item__label) {
  color: var(--text-secondary);
  font-size: 13px;
}

.search-section-top :deep(.el-input__wrapper),
.search-section-top :deep(.el-select .el-input__wrapper) {
  border-radius: 8px;
}

.content-section {
  background: var(--bg-card);
  border-radius: 12px;
  box-shadow: var(--shadow-card);
  overflow: hidden;
}

.transaction-tabs {
  padding: 0 20px;
  border-bottom: 1px solid var(--bg-hover);
}

.transaction-tabs :deep(.el-tabs__nav-wrap::after) {
  display: none;
}

.transaction-tabs :deep(.el-tabs__item) {
  font-size: 14px;
  font-weight: 500;
  color: var(--text-secondary);
  padding: 0 20px;
  height: 48px;
  line-height: 48px;
}

.transaction-tabs :deep(.el-tabs__item.is-active) {
  color: var(--color-primary);
}

.transaction-tabs :deep(.el-tabs__active-bar) {
  background-color: var(--color-primary);
  height: 3px;
}

.table-wrapper {
  overflow: hidden;
}

.table-wrapper :deep(.el-table) {
  font-size: 13px;
}

.table-wrapper :deep(.el-table th.el-table__cell) {
  font-weight: 600;
  font-size: 12px;
  text-transform: uppercase;
  letter-spacing: 0.5px;
}

.table-wrapper :deep(.el-table td.el-table__cell) {
  padding: 12px 0;
}

.pagination {
  padding: 16px 20px;
  justify-content: flex-end;
  border-top: 1px solid var(--bg-hover);
}

</style>
