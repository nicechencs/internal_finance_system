<template>
  <div class="page-container">
    <!-- 页面头部 -->
    <div class="page-header">
      <div class="page-header-left">
        <h2 class="page-title">分类管理</h2>
        <p class="page-desc">设置交易的收入和支出分类</p>
      </div>
      <div class="page-header-right">
        <el-button v-if="userStore.isAdmin" type="primary" @click="handleAdd">新增分类</el-button>
      </div>
    </div>

    <!-- 统计卡片 -->
    <el-row :gutter="24" class="stat-cards">
      <el-col :xs="24" :sm="12" :md="6">
        <StatCard
          :icon="FolderOpened"
          :value="String(statistics.totalCount)"
          label="总分类数"
          :count="`${statistics.activeCount} 个启用`"
          theme="info"
        />
      </el-col>
      <el-col :xs="24" :sm="12" :md="6">
        <StatCard
          :icon="TrendCharts"
          :value="String(statistics.incomeCategoryCount)"
          label="收入分类"
          theme="income"
        />
      </el-col>
      <el-col :xs="24" :sm="12" :md="6">
        <StatCard
          :icon="Minus"
          :value="String(statistics.expenseCategoryCount)"
          label="支出分类"
          theme="expense"
        />
      </el-col>
      <el-col :xs="24" :sm="12" :md="6">
        <StatCard
          :icon="Check"
          :value="String(statistics.activeCount)"
          label="启用分类"
          theme="profit"
        />
      </el-col>
    </el-row>

    <!-- 搜索区域 -->
    <div class="search-section">
      <el-form :inline="true" :model="searchForm" @submit.prevent="handleQuery">
        <el-form-item label="类型">
          <el-select v-model="searchForm.categoryType" placeholder="全部" clearable @change="handleQuery" style="width: 150px">
            <el-option label="全部" value="" />
            <el-option label="收入" value="Income" />
            <el-option label="支出" value="Expense" />
          </el-select>
        </el-form-item>
        <el-form-item>
          <el-button type="primary" @click="handleQuery">查询</el-button>
          <el-button @click="handleResetAll">重置</el-button>
        </el-form-item>
      </el-form>
    </div>

    <!-- 数据表格区域 -->
    <div class="table-section">
      <el-table :data="tableData" style="width: 100%" v-loading="loading" class="resizable-table" border allow-drag-last-column @header-dragend="handleHeaderDragend" @sort-change="handleSortChange">
        <el-table-column prop="name" label="分类名称" sortable="custom" :min-width="getColumnMinWidth('name', TABLE_COLUMN_WIDTH.name)">
          <template #default="{ row }">
            <el-button link type="primary" @click="router.push({ name: 'Transactions', query: { categoryId: String(row.id) } })">
              {{ row.name }}
            </el-button>
          </template>
        </el-table-column>
        <el-table-column prop="categoryType" label="类型" sortable="custom" :width="getColumnWidth('categoryType', TABLE_COLUMN_WIDTH.status)">
          <template #default="{ row }">
            <el-tag :type="row.categoryType === 'Income' ? 'success' : 'danger'">
              {{ row.categoryType === 'Income' ? '收入' : '支出' }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column prop="parentName" label="父分类" :min-width="getColumnMinWidth('parentName', TABLE_COLUMN_WIDTH.name)">
          <template #default="{ row }">
            <el-button
              v-if="row.parentId && row.parentName"
              link
              type="primary"
              @click="router.push({ name: 'Transactions', query: { categoryId: String(row.parentId) } })"
            >
              {{ row.parentName }}
            </el-button>
            <span v-else>{{ row.parentName || '-' }}</span>
          </template>
        </el-table-column>
        <el-table-column prop="description" label="描述" :min-width="getColumnMinWidth('description', TABLE_COLUMN_WIDTH.description)" show-overflow-tooltip />
        <el-table-column prop="isActive" label="状态" :width="getColumnWidth('isActive', TABLE_COLUMN_WIDTH.status)">
          <template #default="{ row }">
            <el-tag :type="row.isActive ? 'success' : 'info'">
              {{ row.isActive ? '启用' : '禁用' }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column column-key="actions" label="操作" :width="getColumnWidth('actions', TABLE_COLUMN_WIDTH.actionTwo)" fixed="right">
          <template #default="{ row }">
            <el-button v-if="userStore.isAdmin" link type="primary" size="small" @click="handleEdit(row)">编辑</el-button>
            <el-button v-if="userStore.isAdmin" link type="danger" size="small" @click="handleDelete(row)">删除</el-button>
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

    <CategoryForm
      v-model:visible="dialogVisible"
      :category="currentCategory"
      @success="handleFormSuccess"
    />
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { FolderOpened, TrendCharts, Minus, Check } from '@element-plus/icons-vue'
import { getCategories, deleteCategory, getCategoryStatistics } from '@/features/master-data/categories/api/category'
import { useUserStore } from '@/features/auth/stores/user'
import { useListPage } from '@/shared/composables/useListPage'
import { useCategoryStore } from '@/features/master-data/categories/stores/category'
import type { Category, CategoryStatistics } from '@/features/master-data/categories/types/category'
import CategoryForm from '@/features/master-data/categories/components/CategoryForm.vue'
import StatCard from '@/shared/ui/StatCard.vue'
import { TABLE_COLUMN_WIDTH } from '@/shared/constants/table'
import { useResizableTableColumns } from '@/shared/composables/useResizableTableColumns'

const userStore = useUserStore()
const categoryStore = useCategoryStore()
const router = useRouter()
const { getColumnWidth, getColumnMinWidth, handleHeaderDragend } = useResizableTableColumns('category-list')
const dialogVisible = ref(false)
const currentCategory = ref<Category | null>(null)

const sortState = reactive({
  sortBy: '',
  sortOrder: '' as '' | 'asc' | 'desc'
})

const statistics = ref<CategoryStatistics>({
  totalCount: 0,
  incomeCategoryCount: 0,
  expenseCategoryCount: 0,
  activeCount: 0
})

const {
  loading,
  tableData,
  searchForm,
  pagination,
  loadData,
  handleReset,
  handleSearch,
  handleSizeChange,
  handlePageChange,
  handleDelete
} = useListPage<Category, { categoryType: string }>({
  fetchData: getCategories,
  deleteData: deleteCategory,
  deleteMessage: '确定要删除该分类吗？',
  initialSearchForm: { categoryType: '' },
  transformParams: (params: any) => {
    if (sortState.sortBy) {
      params.sortBy = sortState.sortBy
      params.sortOrder = sortState.sortOrder
    }
    return params
  },
  onDeleteSuccess: () => {
    categoryStore.invalidateCache()
    loadStatistics()
  }
})

const handleSortChange = ({ prop, order }: { prop: string; order: string | null }) => {
  sortState.sortBy = order ? prop : ''
  sortState.sortOrder = order === 'ascending' ? 'asc' : order === 'descending' ? 'desc' : ''
  pagination.page = 1
  loadData()
}

const handleQuery = () => {
  handleSearch()
  loadStatistics()
}

const handleResetAll = () => {
  handleReset()
  loadStatistics()
}

const handleAdd = () => {
  currentCategory.value = null
  dialogVisible.value = true
}

const handleEdit = (row: Category) => {
  currentCategory.value = row
  dialogVisible.value = true
}

const handleFormSuccess = () => {
  loadData()
  loadStatistics()
  categoryStore.invalidateCache()
}

const loadStatistics = async () => {
  try {
    const params: Record<string, any> = {}
    if (searchForm.categoryType) params.categoryType = searchForm.categoryType
    const { data } = await getCategoryStatistics(params)
    statistics.value = data.data
  } catch (error) {
    console.error('加载统计数据失败:', error)
  }
}

onMounted(() => {
  loadStatistics()
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
