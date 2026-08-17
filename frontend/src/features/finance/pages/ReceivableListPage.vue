<template>
  <div class="page-container">
    <!-- 页面头部 -->
    <PageHeader
      v-if="!embedded"
      title="应收管理"
      description="跟踪和管理应收账款"
    >
      <template #primary>
        <el-button v-if="userStore.canEdit" type="primary" @click="handleCreate">新增应收</el-button>
      </template>
    </PageHeader>

    <!-- 统计卡片 -->
    <el-row :gutter="isMobile ? 12 : 24" class="stat-cards" v-if="!embedded && !externalFilters">
      <el-col :xs="12" :sm="12" :md="6">
        <StatCard
          :icon="Tickets"
          :value="String(statistics.totalCount)"
          label="应收总数"
          :count="`${statistics.pendingCount} 待收`"
          theme="info"
        />
      </el-col>
      <el-col :xs="12" :sm="12" :md="6">
        <StatCard
          :icon="Money"
          :value="formatMoney(statistics.totalAmount)"
          label="应收总额"
          theme="income"
        />
      </el-col>
      <el-col :xs="12" :sm="12" :md="6">
        <StatCard
          :icon="Warning"
          :value="formatMoney(statistics.remainingAmount)"
          label="未收款"
          theme="expense"
        />
      </el-col>
      <el-col :xs="12" :sm="12" :md="6">
        <StatCard
          :icon="Clock"
          :value="formatMoney(statistics.overdueAmount)"
          label="逾期金额"
          theme="balance"
        />
      </el-col>
    </el-row>

    <!-- 搜索区域 -->
    <FilterDrawer
      v-if="!externalFilters"
      v-model="filterDrawerVisible"
      :active-count="activeFilterCount"
      @apply="handleFilter"
      @reset="handleReset"
    >
    <div class="search-section">
      <el-form :inline="true" :model="filters" @submit.prevent="handleFilter">
        <el-form-item label="项目">
          <SearchableSelect
            v-model="filters.projectId"
            :options="projects"
            entity-name="项目"
            width="220px"
            @change="handleFilter"
          />
        </el-form-item>
        <el-form-item label="客户">
          <SearchableSelect
            v-model="filters.customerId"
            :options="customers"
            entity-name="客户"
            width="220px"
            @change="handleFilter"
          />
        </el-form-item>
        <el-form-item label="状态">
          <el-select v-model="filters.status" placeholder="全部" clearable @change="handleFilter" style="width: 150px">
            <el-option label="待收款" value="pending" />
            <el-option label="部分收款" value="partial" />
            <el-option label="已结清" value="settled" />
          </el-select>
        </el-form-item>
        <el-form-item label="标签">
          <TagSelector
            :model-value="filters.tagIds"
            scope="receivable"
            @change="handleTagFilterChange"
            placeholder="按标签筛选"
          />
        </el-form-item>
        <el-form-item label="到期日期">
          <el-date-picker
            v-model="filters.dueDateRange"
            type="daterange"
            range-separator="至"
            start-placeholder="开始日期"
            end-placeholder="结束日期"
            :shortcuts="dateRangeShortcuts"
            @change="handleFilter"
          />
        </el-form-item>
        <el-form-item class="search-buttons">
          <el-button type="primary" @click="handleFilter">查询</el-button>
          <el-button @click="handleReset">重置</el-button>
        </el-form-item>
      </el-form>

      <ActiveTagFilters
        :tag-ids="filters.tagIds"
        scope="receivable"
        @remove="removeTagFilter"
        @clear="clearTagFilters"
      />
    </div>
    </FilterDrawer>

    <!-- 数据表格区域 -->
    <div class="table-section">
      <ResponsiveList :items="receivables" :loading="loading">
        <template #table>
      <el-table :data="receivables" v-loading="loading" class="resizable-table" border allow-drag-last-column @header-dragend="handleHeaderDragend" @sort-change="handleSortChange">
        <el-table-column prop="projectName" label="项目" :width="getColumnWidth('projectName', TABLE_COLUMN_WIDTH.project)">
          <template #default="{ row }">
            <el-link v-if="row.projectId" type="primary" @click="router.push({ name: 'ProjectDetail', params: { id: row.projectId } })">
              {{ row.projectName }}
            </el-link>
            <span v-else>-</span>
          </template>
        </el-table-column>
        <el-table-column prop="counterpartyName" label="对方" :width="getColumnWidth('counterpartyName', TABLE_COLUMN_WIDTH.company)">
          <template #default="{ row }">
            <template v-if="row.customerId">
              <el-link type="primary" @click="router.push({ name: 'CustomerDetail', params: { id: row.customerId } })">
                {{ row.customerName }}
              </el-link>
            </template>
            <template v-else-if="row.supplierId">
              <el-link type="primary" @click="router.push({ name: 'SupplierDetail', params: { id: row.supplierId } })">
                {{ row.supplierName }}
              </el-link>
            </template>
            <template v-else-if="row.personId">
              <el-link type="primary" @click="router.push({ name: 'PersonDetail', params: { id: row.personId } })">
                {{ row.personName }}
              </el-link>
            </template>
            <span v-else>-</span>
          </template>
        </el-table-column>
        <el-table-column prop="totalAmount" label="应收金额" sortable="custom" :width="getColumnWidth('totalAmount', TABLE_COLUMN_WIDTH.amount)" align="right">
          <template #default="{ row }">
            {{ formatMoney(row.totalAmount) }}
          </template>
        </el-table-column>
        <el-table-column prop="receivedAmount" label="已收金额" :width="getColumnWidth('receivedAmount', TABLE_COLUMN_WIDTH.amount)" align="right">
          <template #default="{ row }">
            {{ formatMoney(row.receivedAmount) }}
          </template>
        </el-table-column>
        <el-table-column prop="remainingAmount" label="未收金额" sortable="custom" :width="getColumnWidth('remainingAmount', TABLE_COLUMN_WIDTH.amount)" align="right">
          <template #default="{ row }">
            <span :style="{ color: row.remainingAmount > 0 ? 'var(--color-danger)' : 'var(--color-success)' }">
              {{ formatMoney(row.remainingAmount) }}
            </span>
          </template>
        </el-table-column>
        <el-table-column prop="status" label="状态" sortable="custom" :width="getColumnWidth('status', TABLE_COLUMN_WIDTH.status)">
          <template #default="{ row }">
            <el-tag :type="getStatusType(row.status)">
              {{ getStatusText(row.status) }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column prop="dueDate" label="到期日期" sortable="custom" :width="getColumnWidth('dueDate', TABLE_COLUMN_WIDTH.date)">
          <template #default="{ row }">
            <span :class="{ 'overdue': isOverdue(row) }">
              {{ row.dueDate ? formatDateTime(row.dueDate, 'date') : '-' }}
            </span>
          </template>
        </el-table-column>
        <el-table-column prop="createdAt" label="创建日期" sortable="custom" :width="getColumnWidth('createdAt', TABLE_COLUMN_WIDTH.date)">
          <template #default="{ row }">
            {{ row.createdAt ? formatDateTime(row.createdAt, 'date') : '-' }}
          </template>
        </el-table-column>
        <el-table-column prop="description" label="描述" :min-width="getColumnMinWidth('description', TABLE_COLUMN_WIDTH.description)" show-overflow-tooltip />
        <el-table-column label="标签" :width="getColumnWidth('tags', 160)">
          <template #default="{ row }">
            <TagDisplay :tags="row.tags" size="small" :max-display="2" />
          </template>
        </el-table-column>
        <el-table-column column-key="actions" label="操作" :width="getColumnWidth('actions', TABLE_COLUMN_WIDTH.actionFour)" fixed="right">
          <template #default="{ row }">
            <el-button link type="primary" size="small" @click="handleView(row)">详情</el-button>
            <el-button v-if="userStore.canEdit" link type="primary" size="small" @click="handleManageTags(row)">标签</el-button>
            <el-button
              link
              type="success"
              size="small"
              @click="handleReceive(row)"
              :disabled="row.status === 'settled'"
            >
              收款登记
            </el-button>
            <el-button v-if="userStore.canEdit" link type="primary" size="small" @click="handleEdit(row)">编辑</el-button>
            <el-button v-if="userStore.canEdit" link type="danger" size="small" @click="handleDelete(row)">删除</el-button>
          </template>
        </el-table-column>
      </el-table>
        </template>
        <template #card="{ item }">
          <MobileListCard
            :title="getCounterpartyLabel(item) || item.projectName || '未命名应收'"
            :amount="item.remainingAmount"
            :amount-type="item.remainingAmount > 0 ? 'income' : 'neutral'"
            :signed="false"
            @click="handleView(item)"
          >
            <template #tag>
              <el-tag :type="getStatusType(item.status)" size="small">{{ getStatusText(item.status) }}</el-tag>
            </template>
            <template #meta>
              {{ joinMeta(item.projectName, item.dueDate ? formatDateTime(item.dueDate, 'date') : '') }}
            </template>
            <template #footer>
              应收 {{ formatMoney(item.totalAmount) }} · 已收 {{ formatMoney(item.receivedAmount) }}
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
        @size-change="handleSizeChange"
        @current-change="handlePageChange"
        class="pagination"
      />
        </template>
      </ResponsiveList>
    </div>

    <!-- 应收详情对话框 -->
    <ReceivableDetail
      v-model:visible="detailVisible"
      :receivable-id="currentReceivableId"
      @success="handleDataChanged"
    />

    <!-- 应收表单对话框 -->
    <ReceivableForm
      v-model:visible="formVisible"
      :receivable="currentReceivable"
      @success="handleDataChanged"
    />

    <TagEditorDialog
      v-model:visible="tagDialogVisible"
      owner-type="receivable"
      :owner-id="tagEditTarget?.id ?? null"
      :owner-name="tagEditTarget?.name"
      @saved="handleDataChanged"
    />
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, onMounted, watch, computed } from 'vue'
import { useRouter, useRoute } from 'vue-router'
import { ElMessage, ElMessageBox } from 'element-plus'
import { Tickets, Money, Warning, Clock } from '@element-plus/icons-vue'
import { getReceivables, deleteReceivable, getReceivableStatistics } from '@/features/finance/api/receivable'
import { getProjects } from '@/features/master-data/projects/api/project'
import { getCustomers } from '@/features/master-data/customers/api/customer'
import { dateRangeShortcuts } from '@/shared/utils/dateShortcuts'
import { formatDateTime, formatMoney } from '@/shared/utils/formatters'
import { getCounterpartyLabel, joinMeta } from '@/shared/utils/recordDisplay'
import { useBreakpoint } from '@/shared/composables/useBreakpoint'
import { isDateBeforeToday } from '@/shared/utils/date'
import { useUserStore } from '@/features/auth/stores/user'
import { useRouteFilters } from '@/shared/composables/useRouteFilters'
import { useListPageStatistics } from '@/shared/composables/useListPageStatistics'
import type { Receivable, ReceivableStatistics } from '@/features/finance/types/receivable'
import type { Project } from '@/features/master-data/projects/types/project'
import type { Customer } from '@/features/master-data/customers/types/customer'
import SearchableSelect from '@/shared/ui/SearchableSelect.vue'
import StatCard from '@/shared/ui/StatCard.vue'
import PageHeader from '@/shared/ui/PageHeader.vue'
import FilterDrawer from '@/shared/ui/FilterDrawer.vue'
import ResponsiveList from '@/shared/ui/ResponsiveList.vue'
import MobileListCard from '@/shared/ui/MobileListCard.vue'
import { TABLE_COLUMN_WIDTH } from '@/shared/constants/table'
import { useResizableTableColumns } from '@/shared/composables/useResizableTableColumns'
import ReceivableDetail from '@/features/finance/pages/ReceivableDetailPage.vue'
import ReceivableForm from '@/features/finance/components/ReceivableForm.vue'
import TagEditorDialog from '@/components/tags/TagEditorDialog.vue'
import ActiveTagFilters from '@/components/tags/ActiveTagFilters.vue'
import TagSelector from '@/components/tags/TagSelector.vue'
import TagDisplay from '@/components/tags/TagDisplay.vue'

// Props
const props = defineProps<{
  embedded?: boolean
  externalFilters?: {
    projectId?: number | null
    customerId?: number | null
    status?: string
    dueDateRange?: [Date, Date] | null
    tagIds?: number[]
  }
  triggerReload?: number
}>()

const userStore = useUserStore()
const router = useRouter()
const route = useRoute()
const { getColumnWidth, getColumnMinWidth, handleHeaderDragend } = useResizableTableColumns('receivable-list')
const loading = ref(false)
const receivables = ref<Receivable[]>([])
const projects = ref<Project[]>([])
const customers = ref<Customer[]>([])
const detailVisible = ref(false)
const currentReceivableId = ref<number>(0)
const formVisible = ref(false)
const currentReceivable = ref<Receivable | null>(null)
const tagDialogVisible = ref(false)
const tagEditTarget = ref<{ id: number; name: string } | null>(null)

const filters = reactive({
  projectId: null as number | null,
  customerId: null as number | null,
  status: '',
  tagIds: [] as number[],
  dueDateRange: null as [Date, Date] | null
})

const { isMobile } = useBreakpoint()
const filterDrawerVisible = ref(false)
const activeFilterCount = computed(() => {
  let count = 0
  if (filters.projectId) count++
  if (filters.customerId) count++
  if (filters.status) count++
  if (filters.dueDateRange) count++
  if (filters.tagIds.length) count++
  return count
})

const buildFilterParams = (): Record<string, any> => {
  const params: Record<string, any> = {}
  if (filters.projectId) params.projectId = filters.projectId
  if (filters.customerId) params.customerId = filters.customerId
  if (filters.status) params.status = filters.status
  if (filters.dueDateRange && filters.dueDateRange.length === 2) {
    params.startDate = formatDateTime(filters.dueDateRange[0], 'date')
    params.endDate = formatDateTime(filters.dueDateRange[1], 'date')
  }
  if (filters.tagIds.length > 0) {
    params.tagFilters = [{ scope: 'receivable', tagIds: filters.tagIds, matchMode: 'or' }]
  }
  return params
}

const { statistics, loadStatistics } = useListPageStatistics<ReceivableStatistics>({
  fetchStatistics: getReceivableStatistics,
  initialStatistics: {
    totalCount: 0,
    pendingCount: 0,
    partialCount: 0,
    settledCount: 0,
    totalAmount: 0,
    receivedAmount: 0,
    remainingAmount: 0,
    overdueAmount: 0
  },
  buildParams: buildFilterParams,
  autoLoad: false
})

const pagination = reactive({
  page: 1,
  pageSize: 20,
  total: 0
})

const sortState = reactive({
  sortBy: '',
  sortOrder: '' as '' | 'asc' | 'desc'
})

const handleSortChange = ({ prop, order }: { prop: string; order: string | null }) => {
  sortState.sortBy = order ? prop : ''
  sortState.sortOrder = order === 'ascending' ? 'asc' : order === 'descending' ? 'desc' : ''
  pagination.page = 1
  loadReceivables()
}

const { applyRouteFilters } = useRouteFilters({
  filters,
  fieldMappings: [
    { queryParam: 'tagId', filterField: 'tagIds', type: 'number[]' }
  ],
  onFiltersApplied: () => {
    pagination.page = 1
    loadReceivables()
    loadStatistics()
  }
})

const handleDataChanged = () => {
  loadReceivables()
  loadStatistics()
}

const loadReceivables = async () => {
  loading.value = true
  try {
    // 如果有外部筛选条件，使用外部条件；否则使用内部条件
    const activeFilters = props.externalFilters || filters

    const params: any = {
      page: pagination.page,
      pageSize: pagination.pageSize,
      projectId: activeFilters.projectId,
      customerId: activeFilters.customerId,
      status: activeFilters.status
    }

    if (sortState.sortBy) {
      params.sortBy = sortState.sortBy
      params.sortOrder = sortState.sortOrder
    }

    if (activeFilters.dueDateRange && activeFilters.dueDateRange.length === 2) {
      params.startDate = formatDateTime(activeFilters.dueDateRange[0], 'date')
      params.endDate = formatDateTime(activeFilters.dueDateRange[1], 'date')
    }

    if (activeFilters.tagIds && activeFilters.tagIds.length > 0) {
      params.tagFilters = [{ scope: 'receivable', tagIds: activeFilters.tagIds, matchMode: 'or' }]
    }

    const { data } = await getReceivables(params)
    receivables.value = data.data.items
    pagination.total = data.data.total
  } catch (error) {
    ElMessage.error('加载应收列表失败')
  } finally {
    loading.value = false
  }
}

// 监听外部触发的重新加载
watch(() => props.triggerReload, () => {
  if (props.externalFilters !== undefined) {
    pagination.page = 1
    loadReceivables()
  }
})

const loadProjects = async () => {
  try {
    const { data } = await getProjects({ page: 1, pageSize: 1000 })
    projects.value = data.data.items
  } catch (error) {
    console.error('加载项目列表失败', error)
  }
}

const loadCustomers = async () => {
  try {
    const { data } = await getCustomers({ page: 1, pageSize: 1000 })
    customers.value = data.data.items
  } catch (error) {
    console.error('加载客户列表失败', error)
  }
}

const handleCreate = () => {
  currentReceivable.value = null
  formVisible.value = true
}

defineExpose({
  handleCreate
})

const handleEdit = (row: Receivable) => {
  currentReceivable.value = row
  formVisible.value = true
}

const handleManageTags = (row: Receivable) => {
  tagEditTarget.value = { id: row.id, name: row.customerName || row.supplierName || row.personName || '应收#' + row.id }
  tagDialogVisible.value = true
}

const handleView = (row: Receivable) => {
  currentReceivableId.value = row.id
  detailVisible.value = true
}

const handleReceive = (row: Receivable) => {
  currentReceivableId.value = row.id
  detailVisible.value = true
}

const handleDelete = async (row: Receivable) => {
  try {
    await ElMessageBox.confirm('确定要删除这条应收记录吗？', '提示', {
      confirmButtonText: '确定',
      cancelButtonText: '取消',
      type: 'warning'
    })

    await deleteReceivable(row.id)
    ElMessage.success('删除成功')
    loadReceivables()
    loadStatistics()
  } catch (error) {
    if (error !== 'cancel') {
      // API 错误已由请求拦截器统一提示
    }
  }
}

const handleFilter = () => {
  pagination.page = 1
  loadReceivables()
  if (!props.externalFilters) loadStatistics()
}

const handleTagFilterChange = (tagIds: number[]) => {
  filters.tagIds = tagIds
  handleFilter()
}

const removeTagFilter = (tagId: number) => {
  filters.tagIds = filters.tagIds.filter(id => id !== tagId)
  handleFilter()
}

const clearTagFilters = () => {
  filters.tagIds = []
  handleFilter()
}

const handleReset = async () => {
  filters.projectId = null
  filters.customerId = null
  filters.status = ''
  filters.tagIds = []
  filters.dueDateRange = null

  if (Object.keys(route.query).length > 0) {
    await router.replace({ name: 'ReceivableList' })
  }

  handleFilter()
}

const handleSizeChange = () => {
  pagination.page = 1
  loadReceivables()
}

const handlePageChange = () => {
  loadReceivables()
}

const getStatusType = (status: string) => {
  const statusMap: Record<string, any> = {
    pending: 'warning',
    partial: 'info',
    settled: 'success'
  }
  return statusMap[status] || ''
}

const getStatusText = (status: string) => {
  const statusMap: Record<string, string> = {
    pending: '待收款',
    partial: '部分收款',
    settled: '已结清'
  }
  return statusMap[status] || status
}

const isOverdue = (row: Receivable) => {
  if (!row.dueDate || row.status === 'settled') return false
  return isDateBeforeToday(row.dueDate) && row.remainingAmount > 0
}

onMounted(() => {
  if (!props.externalFilters) {
    applyRouteFilters()
  } else {
    loadReceivables()
  }
  loadProjects()
  loadCustomers()
})

</script>

<style scoped>
.page-container {
  padding: 0;
}


.stat-cards {
  margin-bottom: 24px;
}

.search-section {
  background: var(--bg-card);
  border-radius: 12px;
  padding: 20px 20px 4px 20px;
  margin-bottom: 16px;
  box-shadow: 0 1px 3px rgba(0, 0, 0, 0.04);
}

.table-section {
  background: var(--bg-card);
  border-radius: 12px;
  padding: 0;
  box-shadow: 0 1px 3px rgba(0, 0, 0, 0.04);
  overflow: hidden;
}

/* 表格自定义样式 */
.table-section :deep(.el-table) {
  font-size: 13px;
}

.table-section :deep(.el-table th.el-table__cell) {
  font-weight: 600;
  font-size: 12px;
  text-transform: uppercase;
  letter-spacing: 0.5px;
}

.table-section :deep(.el-table td.el-table__cell) {
  padding: 12px 0;
}

.pagination {
  padding: 16px 20px;
  justify-content: flex-end;
  border-top: 1px solid var(--bg-hover);
}

/* 新增按钮样式 */
.page-header :deep(.el-button--primary) {
  border-radius: 8px;
  padding: 10px 20px;
}

/* 搜索表单样式 */
.search-section :deep(.el-form-item__label) {
  color: var(--text-secondary);
  font-size: 13px;
}

.search-section :deep(.el-input__wrapper),
.search-section :deep(.el-select .el-input__wrapper) {
  border-radius: 8px;
}

.overdue {
  color: var(--color-danger);
  font-weight: bold;
}

.embedded-header {
  display: flex;
  justify-content: flex-end;
  margin-bottom: 16px;
}
</style>

