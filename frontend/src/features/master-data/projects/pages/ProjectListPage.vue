<template>
  <div class="page-container">
    <!-- 页面头部 -->
    <div class="page-header">
      <div>
        <h2 class="page-title">项目管理</h2>
        <p class="page-desc">管理和跟踪公司项目</p>
      </div>
      <div class="page-header-actions">
        <el-button v-if="userStore.canEdit" type="warning" @click="batchLinkVisible = true">
          <el-icon><Link /></el-icon> 批量智能关联
        </el-button>
        <el-button v-if="userStore.canEdit" @click="showImportDialog = true">批量导入</el-button>
        <el-button v-if="userStore.canEdit" type="primary" @click="handleCreate">新增项目</el-button>
      </div>
    </div>

    <!-- 统计卡片 -->
    <el-row :gutter="24" class="stat-cards">
      <el-col :xs="24" :sm="12" :md="6">
        <StatCard
          :icon="Briefcase"
          :value="String(statistics.totalCount)"
          label="总项目数"
          theme="info"
        />
      </el-col>
      <el-col :xs="24" :sm="12" :md="6">
        <StatCard
          :icon="TrendCharts"
          :value="formatCurrency(statistics.totalContractAmount)"
          label="总合同金额"
          theme="income"
        />
      </el-col>
      <el-col :xs="24" :sm="12" :md="6">
        <StatCard
          :icon="Coin"
          :value="formatCurrency(statistics.totalProfit)"
          label="总利润"
          theme="profit"
        />
      </el-col>
      <el-col :xs="24" :sm="12" :md="6">
        <StatCard
          :icon="Money"
          :value="formatCurrency(statistics.totalReceivable)"
          label="应收款"
          theme="balance"
        />
      </el-col>
    </el-row>

    <!-- 搜索区域 -->
    <div class="search-section">
      <el-form :inline="true" :model="filters" @submit.prevent="handleFilter">
        <el-form-item label="项目名称">
          <SearchableFilterInput
            v-model="filters.name"
            :fetch-options="getActiveProjects"
            placeholder="请输入或选择项目名称"
            clearable
            @update:model-value="handleFilter"
          />
        </el-form-item>
        <el-form-item label="客户">
          <el-select v-model="filters.customerId" placeholder="全部" clearable @change="handleFilter" style="width: 200px">
            <el-option v-for="customer in customers" :key="customer.id" :label="customer.name" :value="customer.id" />
          </el-select>
        </el-form-item>
        <el-form-item label="状态">
          <el-select v-model="filters.status" placeholder="全部" clearable @change="handleFilter" style="width: 150px">
            <el-option label="进行中" value="Active" />
            <el-option label="已完成" value="Completed" />
            <el-option label="已取消" value="Cancelled" />
          </el-select>
        </el-form-item>
        <el-form-item label="标签">
          <TagSelector
            :model-value="filters.tagIds"
            scope="project"
            @change="handleTagFilterChange"
            placeholder="按标签筛选"
          />
        </el-form-item>
        <el-form-item>
          <el-button type="primary" @click="handleFilter">查询</el-button>
          <el-button @click="handleReset">重置</el-button>
        </el-form-item>
      </el-form>

      <ActiveTagFilters
        :tag-ids="filters.tagIds"
        scope="project"
        @remove="removeTagFilter"
        @clear="clearTagFilters"
      />
    </div>

    <!-- 数据表格区域 -->
    <div class="table-section">
      <el-table :data="projects" v-loading="loading" class="resizable-table" border allow-drag-last-column @header-dragend="handleHeaderDragend" @sort-change="handleSortChange">
        <el-table-column prop="projectCode" label="项目编号" :width="getColumnWidth('projectCode', TABLE_COLUMN_WIDTH.code)">
          <template #default="{ row }">
            <el-button link type="primary" @click="handleViewDetail(row)">{{ row.projectCode }}</el-button>
          </template>
        </el-table-column>
        <el-table-column prop="name" label="项目名称" sortable="custom" :min-width="getColumnMinWidth('name', TABLE_COLUMN_WIDTH.project)" show-overflow-tooltip>
          <template #default="{ row }">
            <el-button link type="primary" @click="handleViewDetail(row)">{{ row.name }}</el-button>
          </template>
        </el-table-column>
        <el-table-column prop="customerName" label="客户" :width="getColumnWidth('customerName', TABLE_COLUMN_WIDTH.company)" show-overflow-tooltip>
          <template #default="{ row }">
            <el-button
              v-if="row.customerId"
              link
              type="primary"
              @click="router.push({ name: 'CustomerDetail', params: { id: row.customerId } })"
            >
              {{ row.customerName }}
            </el-button>
            <span v-else>{{ row.customerName || '-' }}</span>
          </template>
        </el-table-column>
        <el-table-column prop="contractAmount" label="合同金额" sortable="custom" :width="getColumnWidth('contractAmount', TABLE_COLUMN_WIDTH.amount)" align="right">
          <template #default="{ row }">
            {{ formatMoney(row.contractAmount) }}
          </template>
        </el-table-column>
        <el-table-column prop="receivedAmount" label="已收金额" :width="getColumnWidth('receivedAmount', TABLE_COLUMN_WIDTH.amount)" align="right">
          <template #default="{ row }">
            {{ formatMoney(row.receivedAmount) }}
          </template>
        </el-table-column>
        <el-table-column label="收款进度" :width="getColumnWidth('collectionProgress', 140)" align="center">
          <template #default="{ row }">
            <el-progress
              :percentage="row.contractAmount > 0 ? Math.min(Math.round(row.receivedAmount / row.contractAmount * 100), 100) : 0"
              :stroke-width="14"
              :text-inside="true"
              :color="row.receivedAmount >= row.contractAmount && row.contractAmount > 0 ? '#67c23a' : '#e6a23c'"
            />
          </template>
        </el-table-column>
        <el-table-column label="收款状态" :width="getColumnWidth('collectionStatus', TABLE_COLUMN_WIDTH.status)" align="center">
          <template #default="{ row }">
            <el-tag v-if="row.receivedAmount >= row.contractAmount && row.contractAmount > 0" type="success">已结清</el-tag>
            <el-tag v-else-if="row.receivedAmount > 0" type="warning">部分收款</el-tag>
            <el-tag v-else type="info">未收款</el-tag>
          </template>
        </el-table-column>
        <el-table-column prop="status" label="状态" sortable="custom" :width="getColumnWidth('status', TABLE_COLUMN_WIDTH.status)" align="center">
          <template #default="{ row }">
            <el-tag v-if="row.status === 'Active'" type="success">进行中</el-tag>
            <el-tag v-else-if="row.status === 'Completed'" type="info">已完成</el-tag>
            <el-tag v-else-if="row.status === 'Cancelled'" type="danger">已取消</el-tag>
          </template>
        </el-table-column>
        <el-table-column prop="startDate" label="开始日期" sortable="custom" :width="getColumnWidth('startDate', TABLE_COLUMN_WIDTH.date)">
          <template #default="{ row }">
            {{ row.startDate ? formatDateTime(row.startDate, 'date') : '-' }}
          </template>
        </el-table-column>
        <el-table-column prop="endDate" label="结束日期" :width="getColumnWidth('endDate', TABLE_COLUMN_WIDTH.date)">
          <template #default="{ row }">
            {{ row.endDate ? formatDateTime(row.endDate, 'date') : '-' }}
          </template>
        </el-table-column>
        <el-table-column prop="description" label="描述" :min-width="getColumnMinWidth('description', TABLE_COLUMN_WIDTH.description)" show-overflow-tooltip>
          <template #default="{ row }">
            <span>{{ row.description || '-' }}</span>
          </template>
        </el-table-column>
        <el-table-column label="标签" :width="getColumnWidth('tags', 160)">
          <template #default="{ row }">
            <TagDisplay :tags="row.tags" size="small" :max-display="2" />
          </template>
        </el-table-column>
        <el-table-column column-key="actions" label="操作" :width="getColumnWidth('actions', TABLE_COLUMN_WIDTH.actionTwo)" fixed="right">
          <template #default="{ row }">
            <el-button v-if="userStore.canEdit" link type="primary" size="small" @click="handleManageTags(row)">标签</el-button>
            <el-button v-if="userStore.canEdit" link type="primary" size="small" @click="handleEdit(row)">编辑</el-button>
            <el-button v-if="userStore.canDelete" link type="danger" size="small" @click="handleDelete(row)">删除</el-button>
          </template>
        </el-table-column>
      </el-table>

      <el-pagination
        v-model:current-page="pagination.page"
        v-model:page-size="pagination.pageSize"
        :total="pagination.total"
        :page-sizes="[10, 20, 50, 100]"
        layout="total, sizes, prev, pager, next, jumper"
        @size-change="handleSizeChange"
        @current-change="handlePageChange"
        class="pagination"
      />
    </div>

    <!-- 项目表单对话框 -->
    <ProjectForm
      v-model:visible="formVisible"
      :project="currentProject"
      @success="handleFormSuccess"
    />

    <!-- 导入对话框 -->
    <ImportDialog
      v-model="showImportDialog"
      module-type="project"
      @success="handleImportSuccess"
    />

    <BatchLinkDialog
      v-model="batchLinkVisible"
      @success="loadProjects"
    />

    <TagEditorDialog
      v-model:visible="tagDialogVisible"
      owner-type="project"
      :owner-id="tagEditTarget?.id ?? null"
      :owner-name="tagEditTarget?.name"
      @saved="loadProjects"
    />

  </div>
</template>

<script setup lang="ts">
import { ref, reactive, onMounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { ElMessage, ElMessageBox } from 'element-plus'
import { Briefcase, TrendCharts, Coin, Money, Link } from '@element-plus/icons-vue'
import { getProjects, deleteProject, getActiveProjects, getProjectStatistics } from '@/features/master-data/projects/api/project'
import { getActiveCustomers } from '@/features/master-data/customers/api/customer'
import { useUserStore } from '@/features/auth/stores/user'
import type { Project, ProjectStatistics } from '@/features/master-data/projects/types/project'
import type { Customer } from '@/features/master-data/customers/types/customer'
import ProjectForm from '@/features/master-data/projects/pages/ProjectFormPage.vue'
import ImportDialog from '@/features/import/components/ImportDialog.vue'
import BatchLinkDialog from '@/shared/ui/BatchLinkDialog.vue'
import StatCard from '@/shared/ui/StatCard.vue'
import SearchableFilterInput from '@/shared/ui/SearchableFilterInput.vue'
import ActiveTagFilters from '@/components/tags/ActiveTagFilters.vue'
import TagSelector from '@/components/tags/TagSelector.vue'
import TagDisplay from '@/components/tags/TagDisplay.vue'
import { TABLE_COLUMN_WIDTH } from '@/shared/constants/table'
import { useResizableTableColumns } from '@/shared/composables/useResizableTableColumns'
import { useRouteFilters } from '@/shared/composables/useRouteFilters'
import { useListPageStatistics } from '@/shared/composables/useListPageStatistics'
import { formatDateTime, formatCurrency, formatMoney } from '@/shared/utils/formatters'
import TagEditorDialog from '@/components/tags/TagEditorDialog.vue'

const route = useRoute()
const userStore = useUserStore()
const router = useRouter()
const { getColumnWidth, getColumnMinWidth, handleHeaderDragend } = useResizableTableColumns('project-list')
const loading = ref(false)
const projects = ref<Project[]>([])
const customers = ref<Customer[]>([])
const formVisible = ref(false)
const showImportDialog = ref(false)
const batchLinkVisible = ref(false)
const currentProject = ref<Project | null>(null)
const tagDialogVisible = ref(false)
const tagEditTarget = ref<{ id: number; name: string } | null>(null)

const filters = reactive({
  name: '',
  customerId: undefined as number | undefined,
  status: '',
  tagIds: [] as number[]
})

const buildFilterParams = (): Record<string, any> => {
  const params: Record<string, any> = {
    name: filters.name || undefined,
    customerId: filters.customerId || undefined,
    status: filters.status || undefined
  }

  if (filters.tagIds.length > 0) {
    params.tagFilters = [{ scope: 'project', tagIds: filters.tagIds, matchMode: 'or' }]
  }

  return params
}

// 应用 useRouteFilters
const { applyRouteFilters } = useRouteFilters({
  filters,
  fieldMappings: [
    { queryParam: 'tagId', filterField: 'tagIds', type: 'number[]' }
  ],
  onFiltersApplied: () => {
    pagination.page = 1
    loadProjects()
    loadStatistics()
  }
})

// 应用 useListPageStatistics
const { statistics, loadStatistics } = useListPageStatistics<ProjectStatistics>({
  fetchStatistics: getProjectStatistics,
  initialStatistics: {
    totalCount: 0,
    totalContractAmount: 0,
    totalProfit: 0,
    totalReceivable: 0
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
  loadProjects()
}

const loadProjects = async () => {
  loading.value = true
  try {
    const { data } = await getProjects({
      page: pagination.page,
      pageSize: pagination.pageSize,
      ...buildFilterParams(),
      ...(sortState.sortBy ? { sortBy: sortState.sortBy, sortOrder: sortState.sortOrder } : {})
    })
    projects.value = data.data.items
    pagination.total = data.data.total
  } catch (error) {
    ElMessage.error('加载项目列表失败')
  } finally {
    loading.value = false
  }
}

const loadCustomers = async () => {
  try {
    const { data } = await getActiveCustomers()
    customers.value = data.data
  } catch (error) {
    ElMessage.error('加载客户列表失败')
  }
}

const handleCreate = () => {
  currentProject.value = null
  formVisible.value = true
}

const handleViewDetail = (row: Project) => {
  router.push(`/projects/${row.id}`)
}

const handleEdit = (row: Project) => {
  currentProject.value = row
  formVisible.value = true
}

const handleManageTags = (row: Project) => {
  tagEditTarget.value = { id: row.id, name: row.name }
  tagDialogVisible.value = true
}

const handleDelete = async (row: Project) => {
  try {
    await ElMessageBox.confirm('确定要删除这个项目吗？', '提示', {
      confirmButtonText: '确定',
      cancelButtonText: '取消',
      type: 'warning'
    })

    await deleteProject(row.id)
    ElMessage.success('删除成功')
    loadProjects()
    loadStatistics()
  } catch (error) {
    if (error !== 'cancel') {
      // API 错误已由请求拦截器统一提示
    }
  }
}

const handleFilter = () => {
  pagination.page = 1
  loadProjects()
  loadStatistics()
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

const handleReset = () => {
  filters.name = ''
  filters.customerId = undefined
  filters.status = ''
  filters.tagIds = []
  handleFilter()
}

const handleSizeChange = () => {
  loadProjects()
}

const handlePageChange = () => {
  loadProjects()
}

const handleFormSuccess = (project?: Project) => {
  formVisible.value = false
  loadProjects()
  loadStatistics()

  // 后端已自动创建默认应收记录，不再需要前端初始化
  if (project) {
    ElMessage.success('项目创建成功，已自动创建应收记录')
  }
}

const handleImportSuccess = () => {
  ElMessage.success('导入完成')
  loadProjects()
  loadStatistics()
}

onMounted(() => {
  applyRouteFilters()
  loadCustomers()
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

.page-header-actions {
  display: flex;
  gap: 12px;
}

.page-title {
  font-size: 20px;
  font-weight: 600;
  color: var(--text-primary);
  margin: 0;
}

.page-desc {
  font-size: 13px;
  color: var(--text-placeholder);
  margin: 4px 0 0 0;
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
</style>
