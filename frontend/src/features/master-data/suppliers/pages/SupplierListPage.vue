<template>
  <div class="page-container">
    <div class="page-header">
      <div class="page-header-left">
        <h2 class="page-title">供应商管理</h2>
        <p class="page-desc">管理公司供应商信息</p>
      </div>
      <div class="page-header-right">
        <el-button v-if="userStore.canEdit" type="warning" @click="batchLinkVisible = true">
          <el-icon><Link /></el-icon> 批量智能关联
        </el-button>
        <el-button v-if="userStore.canEdit" @click="showImportDialog = true">批量导入</el-button>
        <el-button v-if="userStore.canEdit" type="primary" @click="handleCreate">新增供应商</el-button>
      </div>
    </div>

    <!-- 统计卡片 -->
    <el-row :gutter="24" class="stat-cards">
      <el-col :xs="24" :sm="12" :md="6">
        <StatCard
          :icon="OfficeBuilding"
          :value="String(statistics.totalCount)"
          label="总供应商数"
          :count="`${statistics.activeCount} 个活跃`"
          theme="info"
        />
      </el-col>
      <el-col :xs="24" :sm="12" :md="6">
        <StatCard
          :icon="Check"
          :value="String(statistics.activeCount)"
          label="活跃供应商"
          theme="income"
        />
      </el-col>
      <el-col :xs="24" :sm="12" :md="6">
        <StatCard
          :icon="Close"
          :value="String(statistics.inactiveCount)"
          label="停用供应商"
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
        <el-form-item label="供应商名称">
          <SearchableFilterInput
            v-model="searchForm.name"
            :fetch-options="getActiveSuppliers"
            placeholder="请输入或选择供应商名称"
            clearable
          />
        </el-form-item>
        <el-form-item label="联系人">
          <el-input v-model="searchForm.contactPerson" placeholder="请输入联系人" clearable />
        </el-form-item>
        <el-form-item label="联系电话">
          <el-input v-model="searchForm.contactPhone" placeholder="请输入联系电话" clearable />
        </el-form-item>
        <el-form-item label="标签">
          <TagSelector
            :model-value="searchForm.tagIds"
            scope="supplier"
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
        scope="supplier"
        @remove="removeTagFilter"
        @clear="clearTagFilters"
      />
    </div>

    <div class="table-section">
      <el-table
        :data="tableData"
        v-loading="loading"
        class="resizable-table"
        border
        allow-drag-last-column
        @header-dragend="handleHeaderDragend"
        @sort-change="handleSortChange"
      >
        <el-table-column prop="name" sortable="custom" :min-width="getColumnMinWidth('name', TABLE_COLUMN_WIDTH.company)">
          <template #header><span>供应商名称<span style="color: var(--color-danger)">*</span></span></template>
          <template #default="{ row }">
            <el-button link type="primary" @click="router.push(`/suppliers/${row.id}`)">{{ row.name }}</el-button>
          </template>
        </el-table-column>
        <el-table-column prop="shortName" label="简称" :width="getColumnWidth('shortName', TABLE_COLUMN_WIDTH.shortText)">
          <template #default="{ row }">{{ row.shortName || '-' }}</template>
        </el-table-column>
        <el-table-column prop="contactPerson" label="联系人" sortable="custom" :width="getColumnWidth('contactPerson', TABLE_COLUMN_WIDTH.contact)">
          <template #default="{ row }">{{ row.contactPerson || '-' }}</template>
        </el-table-column>
        <el-table-column prop="contactPhone" label="联系电话" :width="getColumnWidth('contactPhone', TABLE_COLUMN_WIDTH.phone)">
          <template #default="{ row }">{{ row.contactPhone || '-' }}</template>
        </el-table-column>
        <el-table-column prop="contactEmail" label="联系邮箱" :width="getColumnWidth('contactEmail', TABLE_COLUMN_WIDTH.email)">
          <template #default="{ row }">{{ row.contactEmail || '-' }}</template>
        </el-table-column>
        <el-table-column prop="bankName" label="开户银行" :width="getColumnWidth('bankName', TABLE_COLUMN_WIDTH.bank)">
          <template #default="{ row }">{{ row.bankName || '-' }}</template>
        </el-table-column>
        <el-table-column prop="bankAccount" label="银行账号" :width="getColumnWidth('bankAccount', TABLE_COLUMN_WIDTH.bankAccount)">
          <template #default="{ row }">{{ row.bankAccount || '-' }}</template>
        </el-table-column>
        <el-table-column prop="description" label="备注" :min-width="getColumnMinWidth('description', TABLE_COLUMN_WIDTH.description)" show-overflow-tooltip>
          <template #default="{ row }">{{ row.description || '-' }}</template>
        </el-table-column>
        <el-table-column prop="isActive" label="状态" :width="getColumnWidth('isActive', TABLE_COLUMN_WIDTH.status)">
          <template #default="{ row }">
            <el-tag :type="row.isActive ? 'success' : 'danger'">
              {{ row.isActive ? '启用' : '禁用' }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column label="标签" :width="getColumnWidth('tags', 160)">
          <template #default="{ row }">
            <TagDisplay :tags="row.tags" size="small" :max-display="2" />
          </template>
        </el-table-column>
        <el-table-column column-key="actions" label="操作" :width="getColumnWidth('actions', TABLE_COLUMN_WIDTH.actionFour)" fixed="right">
          <template #default="{ row }">
            <el-button v-if="userStore.canEdit" link type="primary" size="small" @click="handleManageTags(row)">标签</el-button>
            <el-button v-if="userStore.canEdit" link type="primary" size="small" @click="handleEdit(row)">编辑</el-button>
            <el-button
              v-if="userStore.canEdit"
              link
              :type="row.isActive ? 'warning' : 'success'"
              size="small"
              @click="handleToggleStatus(row)"
            >
              {{ row.isActive ? '停用' : '启用' }}
            </el-button>
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
      module-type="supplier"
      @success="handleImportSuccess"
    />

    <SupplierForm
      v-model:visible="formVisible"
      :supplier="currentSupplier"
      @success="handleFormSuccess"
    />

    <BatchLinkDialog
      v-model="batchLinkVisible"
      @success="loadData"
    />

    <TagEditorDialog
      v-model:visible="tagDialogVisible"
      owner-type="supplier"
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
import { OfficeBuilding, Check, Close, Plus, Link } from '@element-plus/icons-vue'
import type { Supplier, SupplierStatistics } from '@/features/master-data/suppliers/types/supplier'
import { deleteSupplier, getActiveSuppliers, getSuppliers, getSupplierStatistics, updateSupplier } from '@/features/master-data/suppliers/api/supplier'
import { useUserStore } from '@/features/auth/stores/user'
import ImportDialog from '@/features/import/components/ImportDialog.vue'
import StatCard from '@/shared/ui/StatCard.vue'
import SearchableFilterInput from '@/shared/ui/SearchableFilterInput.vue'
import ActiveTagFilters from '@/components/tags/ActiveTagFilters.vue'
import TagSelector from '@/components/tags/TagSelector.vue'
import TagDisplay from '@/components/tags/TagDisplay.vue'
import { TABLE_COLUMN_WIDTH } from '@/shared/constants/table'
import { useResizableTableColumns } from '@/shared/composables/useResizableTableColumns'
import { useRouteFilters } from '@/shared/composables/useRouteFilters'
import { useListPageStatistics } from '@/shared/composables/useListPageStatistics'
import SupplierForm from '@/features/master-data/suppliers/pages/SupplierFormPage.vue'
import BatchLinkDialog from '@/shared/ui/BatchLinkDialog.vue'
import TagEditorDialog from '@/components/tags/TagEditorDialog.vue'

const route = useRoute()
const router = useRouter()
const userStore = useUserStore()
const { getColumnWidth, getColumnMinWidth, handleHeaderDragend } = useResizableTableColumns('supplier-list')
const loading = ref(false)
const tableData = ref<Supplier[]>([])
const showImportDialog = ref(false)
const formVisible = ref(false)
const batchLinkVisible = ref(false)
const currentSupplier = ref<Supplier | null>(null)
const tagDialogVisible = ref(false)
const tagEditTarget = ref<{ id: number; name: string } | null>(null)

const searchForm = reactive({
  name: '',
  contactPerson: '',
  contactPhone: '',
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
    contactPerson: searchForm.contactPerson || undefined,
    contactPhone: searchForm.contactPhone || undefined
  }

  if (searchForm.tagIds.length > 0) {
    params.tagFilters = [{ scope: 'supplier', tagIds: searchForm.tagIds, matchMode: 'or' }]
  }

  return params
}

// 应用 useRouteFilters
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

// 应用 useListPageStatistics
const { statistics, loadStatistics } = useListPageStatistics<SupplierStatistics>({
  fetchStatistics: getSupplierStatistics,
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
  currentSupplier.value = null
  formVisible.value = true
}

const handleEdit = (row: Supplier) => {
  currentSupplier.value = row
  formVisible.value = true
}

const handleManageTags = (row: Supplier) => {
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
    const response = await getSuppliers(params)
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

const handleToggleStatus = async (row: Supplier) => {
  try {
    const action = row.isActive ? '停用' : '启用'
    await ElMessageBox.confirm(`确定要${action}该供应商吗？`, '提示', {
      confirmButtonText: '确定',
      cancelButtonText: '取消',
      type: 'warning'
    })

    await updateSupplier(row.id, {
      name: row.name,
      shortName: row.shortName,
      contactPerson: row.contactPerson,
      contactPhone: row.contactPhone,
      contactEmail: row.contactEmail,
      address: row.address,
      taxNumber: row.taxNumber,
      bankAccount: row.bankAccount,
      bankName: row.bankName,
      description: row.description,
      isActive: !row.isActive
    })
    ElMessage.success(`${action}成功`)
    loadData()
    loadStatistics()
  } catch (error) {
    if (error !== 'cancel') {
      console.error('操作失败:', error)
    }
  }
}

const handleDelete = async (row: Supplier) => {
  try {
    await ElMessageBox.confirm('确定要删除该供应商吗？', '提示', {
      confirmButtonText: '确定',
      cancelButtonText: '取消',
      type: 'warning'
    })

    await deleteSupplier(row.id)
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
  searchForm.contactPerson = ''
  searchForm.contactPhone = ''
  searchForm.tagIds = []
  pagination.page = 1

  if (Object.keys(route.query).length > 0) {
    await router.replace({ name: 'SupplierList' })
  }

  loadData()
  loadStatistics()
}

const handleImportSuccess = () => {
  ElMessage.success('导入完成')
  loadData()
  loadStatistics()
}

const handleFormSuccess = () => {
  formVisible.value = false
  currentSupplier.value = null
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
