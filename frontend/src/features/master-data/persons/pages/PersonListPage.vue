<template>
  <div class="page-container">
    <div class="page-header">
      <div class="page-header-left">
        <h2 class="page-title">人员管理</h2>
        <p class="page-desc">管理公司人员信息</p>
      </div>
      <div class="page-header-right">
        <el-button v-if="userStore.canEdit" type="warning" @click="batchLinkVisible = true">
          <el-icon><Link /></el-icon> 批量智能关联
        </el-button>
        <el-button v-if="userStore.canEdit" @click="showImportDialog = true">批量导入</el-button>
        <el-button v-if="userStore.canEdit" type="primary" @click="handleCreate">新增人员</el-button>
      </div>
    </div>

    <!-- 统计卡片 -->
    <el-row :gutter="24" class="stat-cards">
      <el-col :xs="24" :sm="12" :md="6">
        <StatCard
          :icon="UserFilled"
          :value="String(statistics.totalCount)"
          label="总人数"
          :count="`${statistics.activeCount} 人在职`"
          theme="info"
        />
      </el-col>
      <el-col :xs="24" :sm="12" :md="6">
        <StatCard
          :icon="Check"
          :value="String(statistics.activeCount)"
          label="在职人数"
          theme="income"
        />
      </el-col>
      <el-col :xs="24" :sm="12" :md="6">
        <StatCard
          :icon="Close"
          :value="String(statistics.inactiveCount)"
          label="离职人数"
          theme="expense"
        />
      </el-col>
      <el-col :xs="24" :sm="12" :md="6">
        <StatCard
          :icon="Plus"
          :value="String(statistics.thisMonthNewCount)"
          label="本月新增"
          theme="profit"
        />
      </el-col>
    </el-row>

    <div class="search-section">
      <el-form :inline="true" :model="searchForm" @submit.prevent="handleSearch">
        <el-form-item label="姓名">
          <SearchableFilterInput
            v-model="searchForm.name"
            :fetch-options="getActivePersons"
            placeholder="请输入或选择姓名"
            clearable
          />
        </el-form-item>
        <el-form-item label="人员类型">
          <el-select v-model="searchForm.personType" placeholder="请选择人员类型" clearable style="width: 150px">
            <el-option label="员工" value="Employee" />
            <el-option label="承包商" value="Contractor" />
            <el-option label="合伙人" value="Partner" />
          </el-select>
        </el-form-item>
        <el-form-item label="电话">
          <el-input v-model="searchForm.phone" placeholder="请输入电话" clearable />
        </el-form-item>
        <el-form-item label="标签">
          <TagSelector
            :model-value="searchForm.tagIds"
            scope="person"
            @change="handleTagSearchChange"
            placeholder="按标签筛选"
          />
        </el-form-item>
        <el-form-item>
          <el-button type="primary" @click="handleSearch">查询</el-button>
          <el-button @click="handleReset">重置</el-button>
        </el-form-item>
      </el-form>

      <ActiveTagFilters
        :tag-ids="searchForm.tagIds"
        scope="person"
        @remove="removeTagFilter"
        @clear="clearTagFilters"
      />
    </div>

    <div class="table-section">
      <el-table
        :data="tableData"
        v-loading="loading"
        style="width: 100%"
        class="resizable-table"
        border
        allow-drag-last-column
        @header-dragend="handleHeaderDragend"
        @sort-change="handleSortChange"
      >
        <el-table-column prop="name" sortable="custom" :min-width="getColumnMinWidth('name', TABLE_COLUMN_WIDTH.name)">
          <template #header><span>姓名 <span style="color: var(--color-danger)">*</span></span></template>
          <template #default="{ row }">
            <el-button link type="primary" @click="router.push(`/persons/${row.id}`)">{{ row.name }}</el-button>
          </template>
        </el-table-column>
        <el-table-column prop="personType" sortable="custom" :width="getColumnWidth('personType', TABLE_COLUMN_WIDTH.shortText)">
          <template #header><span>人员类型 <span style="color: var(--color-danger)">*</span></span></template>
          <template #default="{ row }">{{ formatPersonType(row.personType) }}</template>
        </el-table-column>
        <el-table-column prop="phone" label="电话" :width="getColumnWidth('phone', TABLE_COLUMN_WIDTH.phone)">
          <template #default="{ row }">{{ row.phone || '-' }}</template>
        </el-table-column>
        <el-table-column prop="email" label="邮箱" :min-width="getColumnMinWidth('email', TABLE_COLUMN_WIDTH.email)">
          <template #default="{ row }">{{ row.email || '-' }}</template>
        </el-table-column>
        <el-table-column prop="bankName" label="开户行" :width="getColumnWidth('bankName', TABLE_COLUMN_WIDTH.bank)">
          <template #default="{ row }">{{ row.bankName || '-' }}</template>
        </el-table-column>
        <el-table-column prop="bankAccount" label="银行账号" :width="getColumnWidth('bankAccount', TABLE_COLUMN_WIDTH.bankAccount)">
          <template #default="{ row }">{{ row.bankAccount || '-' }}</template>
        </el-table-column>
        <el-table-column prop="joinDate" label="入职日期" :width="getColumnWidth('joinDate', TABLE_COLUMN_WIDTH.date)">
          <template #default="{ row }">{{ formatDateTime(row.joinDate, 'date') }}</template>
        </el-table-column>
        <el-table-column prop="isActive" label="状态" :width="getColumnWidth('isActive', TABLE_COLUMN_WIDTH.status)">
          <template #default="{ row }">
            <el-tag :type="row.isActive ? 'success' : 'danger'">
              {{ row.isActive ? '在职' : '离职' }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column label="标签" :width="getColumnWidth('tags', 160)">
          <template #default="{ row }">
            <TagDisplay :tags="row.tags" size="small" :max-display="2" />
          </template>
        </el-table-column>
        <el-table-column column-key="actions" label="操作" :width="getColumnWidth('actions', TABLE_COLUMN_WIDTH.actionThree)" fixed="right">
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

    <ImportDialog
      v-model="showImportDialog"
      module-type="person"
      @success="handleImportSuccess"
    />

    <PersonForm
      v-model:visible="formVisible"
      :person="currentPerson"
      @success="handleFormSuccess"
    />

    <BatchLinkDialog
      v-model="batchLinkVisible"
      @success="loadData"
    />

    <TagEditorDialog
      v-model:visible="tagDialogVisible"
      owner-type="person"
      :owner-id="tagEditTarget?.id ?? null"
      :owner-name="tagEditTarget?.name"
      @saved="loadData"
    />
  </div>
</template>

<script setup lang="ts">
import { onMounted, reactive, ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { ElMessage, ElMessageBox } from 'element-plus'
import { UserFilled, Check, Close, Plus, Link } from '@element-plus/icons-vue'
import type { Person, PersonStatistics } from '@/features/master-data/persons/types/person'
import { deletePerson, getActivePersons, getPersons, getPersonStatistics } from '@/features/master-data/persons/api/person'
import { useUserStore } from '@/features/auth/stores/user'
import { formatDateTime } from '@/shared/utils/formatters'
import ImportDialog from '@/features/import/components/ImportDialog.vue'
import StatCard from '@/shared/ui/StatCard.vue'
import SearchableFilterInput from '@/shared/ui/SearchableFilterInput.vue'
import ActiveTagFilters from '@/components/tags/ActiveTagFilters.vue'
import TagSelector from '@/components/tags/TagSelector.vue'
import TagDisplay from '@/components/tags/TagDisplay.vue'
import { TABLE_COLUMN_WIDTH } from '@/shared/constants/table'
import { useResizableTableColumns } from '@/shared/composables/useResizableTableColumns'
import PersonForm from '@/features/master-data/persons/pages/PersonFormPage.vue'
import BatchLinkDialog from '@/shared/ui/BatchLinkDialog.vue'
import TagEditorDialog from '@/components/tags/TagEditorDialog.vue'
import { useRouteFilters } from '@/shared/composables/useRouteFilters'
import { useListPageStatistics } from '@/shared/composables/useListPageStatistics'

const route = useRoute()
const router = useRouter()
const userStore = useUserStore()
const { getColumnWidth, getColumnMinWidth, handleHeaderDragend } = useResizableTableColumns('person-list')
const loading = ref(false)
const tableData = ref<Person[]>([])
const showImportDialog = ref(false)
const formVisible = ref(false)
const batchLinkVisible = ref(false)
const currentPerson = ref<Person | null>(null)
const tagDialogVisible = ref(false)
const tagEditTarget = ref<{ id: number; name: string } | null>(null)

const searchForm = reactive({
  name: '',
  personType: '',
  phone: '',
  tagIds: [] as number[]
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

const buildFilterParams = (): Record<string, any> => {
  const params: Record<string, any> = {
    name: searchForm.name || undefined,
    personType: searchForm.personType || undefined,
    phone: searchForm.phone || undefined
  }

  if (searchForm.tagIds.length > 0) {
    params.tagFilters = [{ scope: 'person', tagIds: searchForm.tagIds, matchMode: 'or' }]
  }

  return params
}

// 应用路由筛选
const { applyRouteFilters } = useRouteFilters({
  filters: searchForm,
  fieldMappings: [
    { queryParam: 'tagId', filterField: 'tagIds', type: 'number[]' }
  ],
  onFiltersApplied: () => {
    pagination.page = 1
    loadData()
    loadStatistics()
  }
})

// 统计数据
const { statistics, loadStatistics } = useListPageStatistics<PersonStatistics>({
  fetchStatistics: getPersonStatistics,
  initialStatistics: {
    totalCount: 0,
    activeCount: 0,
    inactiveCount: 0,
    thisMonthNewCount: 0
  },
  buildParams: buildFilterParams,
  autoLoad: false
})

const handleSortChange = ({ prop, order }: { prop: string; order: string | null }) => {
  sortState.sortBy = order ? prop : ''
  sortState.sortOrder = order === 'ascending' ? 'asc' : order === 'descending' ? 'desc' : ''
  pagination.page = 1
  loadData()
}

const handleCreate = () => {
  currentPerson.value = null
  formVisible.value = true
}

const handleEdit = (row: Person) => {
  currentPerson.value = row
  formVisible.value = true
}

const handleManageTags = (row: Person) => {
  tagEditTarget.value = { id: row.id, name: row.name }
  tagDialogVisible.value = true
}

const loadData = async () => {
  loading.value = true
  try {
    const params: any = {
      page: pagination.page,
      pageSize: pagination.pageSize,
      ...buildFilterParams()
    }
    if (sortState.sortBy) {
      params.sortBy = sortState.sortBy
      params.sortOrder = sortState.sortOrder
    }
    const response = await getPersons(params)
    tableData.value = response.data.data.items
    pagination.total = response.data.data.total
  } catch (error) {
    console.error('加载数据失败:', error)
    ElMessage.error('加载数据失败')
  } finally {
    loading.value = false
  }
}

const handleSizeChange = () => {
  pagination.page = 1
  loadData()
}

const handlePageChange = () => {
  loadData()
}

const handleSearch = () => {
  pagination.page = 1
  loadData()
  loadStatistics()
}

const handleTagSearchChange = (tagIds: number[]) => {
  searchForm.tagIds = tagIds
  handleSearch()
}

const removeTagFilter = (tagId: number) => {
  searchForm.tagIds = searchForm.tagIds.filter(id => id !== tagId)
  handleSearch()
}

const clearTagFilters = () => {
  searchForm.tagIds = []
  handleSearch()
}

const handleDelete = async (row: Person) => {
  try {
    await ElMessageBox.confirm('确定要删除该人员吗？', '提示', {
      confirmButtonText: '确定',
      cancelButtonText: '取消',
      type: 'warning'
    })

    await deletePerson(row.id)
    ElMessage.success('删除成功')
    loadData()
    loadStatistics()
  } catch (error) {
    if (error !== 'cancel') {
      console.error('删除失败:', error)
    }
  }
}

const handleReset = async () => {
  searchForm.name = ''
  searchForm.personType = ''
  searchForm.phone = ''
  searchForm.tagIds = []
  pagination.page = 1

  if (Object.keys(route.query).length > 0) {
    await router.replace({ name: 'PersonList' })
  }

  loadData()
  loadStatistics()
}

const formatPersonType = (type: string) => {
  const typeMap: Record<string, string> = {
    Employee: '员工',
    Contractor: '承包商',
    Partner: '合伙人',
    Shareholder: '股东'
  }
  return typeMap[type] || type
}

const handleImportSuccess = () => {
  ElMessage.success('导入完成')
  loadData()
  loadStatistics()
}

const handleFormSuccess = () => {
  formVisible.value = false
  currentPerson.value = null
  loadData()
  loadStatistics()
}

onMounted(() => {
  applyRouteFilters()
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

.page-header :deep(.el-button--primary) {
  border-radius: 8px;
  padding: 10px 20px;
}

.search-section :deep(.el-form-item__label) {
  color: var(--text-secondary);
  font-size: 13px;
}

.search-section :deep(.el-input__wrapper),
.search-section :deep(.el-select .el-input__wrapper) {
  border-radius: 8px;
}
</style>
