<template>
  <div class="page-container">
    <!-- 页面头部 -->
    <div class="page-header">
      <div class="page-header-left">
        <h2 class="page-title">分类规则</h2>
        <p class="page-desc">配置自动分类匹配规则</p>
      </div>
      <div class="page-header-right">
        <el-button v-if="userStore.isAdmin" type="warning" @click="ruleRerunDialogVisible = true">规则重跑</el-button>
        <el-button v-if="userStore.isAdmin" type="primary" @click="handleAdd">新增规则</el-button>
      </div>
    </div>

    <!-- 搜索区域 -->
    <div class="search-section">
      <el-form :inline="true" :model="searchForm" @submit.prevent="handleSearch">
        <el-form-item label="关键词">
          <el-input v-model="searchForm.keyword" placeholder="规则名称/匹配值" clearable style="width: 220px" />
        </el-form-item>
        <el-form-item label="状态">
          <el-select v-model="searchForm.isActive" placeholder="全部状态" clearable style="width: 120px">
            <el-option label="启用" :value="true" />
            <el-option label="禁用" :value="false" />
          </el-select>
        </el-form-item>
        <el-form-item>
          <el-button type="primary" @click="handleSearch">查询</el-button>
          <el-button @click="handleReset">重置</el-button>
        </el-form-item>
      </el-form>
    </div>

    <!-- 数据表格区域 -->
    <div class="table-section">
      <el-table :data="tableData" style="width: 100%" v-loading="loading" class="resizable-table" border allow-drag-last-column @header-dragend="handleHeaderDragend" @sort-change="handleSortChange">
        <el-table-column prop="name" label="规则名称" sortable="custom" :min-width="getColumnMinWidth('name', TABLE_COLUMN_WIDTH.name)" />
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
        <el-table-column prop="matchField" label="匹配字段" :width="getColumnWidth('matchField', TABLE_COLUMN_WIDTH.account)">
          <template #default="{ row }">
            {{ getMatchFieldLabel(row.matchField) }}
          </template>
        </el-table-column>
        <el-table-column prop="matchOperator" label="匹配操作符" :width="getColumnWidth('matchOperator', TABLE_COLUMN_WIDTH.account)">
          <template #default="{ row }">
            {{ getMatchOperatorLabel(row.matchOperator) }}
          </template>
        </el-table-column>
        <el-table-column prop="matchValue" label="匹配值" :min-width="getColumnMinWidth('matchValue', TABLE_COLUMN_WIDTH.description)" show-overflow-tooltip />
        <el-table-column prop="priority" label="优先级" :width="getColumnWidth('priority', TABLE_COLUMN_WIDTH.status)" sortable="custom" />
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

    <RuleForm
      v-model:visible="dialogVisible"
      :rule="currentRule"
      @success="loadData"
    />
    <RuleRerunDialog
      v-model="ruleRerunDialogVisible"
      @success="loadData"
    />
  </div>
</template>

<script setup lang="ts">
import { ref, reactive } from 'vue'
import { useRouter } from 'vue-router'
import { getRules, deleteRule } from '@/features/reconciliation/api/rule'
import { useUserStore } from '@/features/auth/stores/user'
import { useListPage } from '@/shared/composables/useListPage'
import type { Rule } from '@/features/reconciliation/types/rule'
import RuleForm from '@/features/reconciliation/components/RuleForm.vue'
import RuleRerunDialog from '@/features/reconciliation/components/RuleRerunDialog.vue'
import { TABLE_COLUMN_WIDTH } from '@/shared/constants/table'
import { useResizableTableColumns } from '@/shared/composables/useResizableTableColumns'

const userStore = useUserStore()
const router = useRouter()
const { getColumnWidth, getColumnMinWidth, handleHeaderDragend } = useResizableTableColumns('rule-list')
const dialogVisible = ref(false)
const ruleRerunDialogVisible = ref(false)
const currentRule = ref<Rule | null>(null)

const sortState = reactive({
  sortBy: '',
  sortOrder: '' as '' | 'asc' | 'desc'
})

const {
  loading,
  tableData,
  searchForm,
  pagination,
  loadData,
  handleSearch,
  handleReset,
  handleSizeChange,
  handlePageChange,
  handleDelete
} = useListPage<Rule, { keyword: string; isActive: string | boolean }>({
  fetchData: getRules,
  deleteData: deleteRule,
  deleteMessage: '确定要删除该规则吗？',
  initialSearchForm: { keyword: '', isActive: '' },
  transformParams: (params: any) => {
    if (sortState.sortBy) {
      params.sortBy = sortState.sortBy
      params.sortOrder = sortState.sortOrder
    }
    // 清理空搜索参数
    if (!params.keyword) {
      delete params.keyword
    }
    if (params.isActive === '' || params.isActive === undefined) {
      delete params.isActive
    }
    return params
  }
})

const handleSortChange = ({ prop, order }: { prop: string; order: string | null }) => {
  sortState.sortBy = order ? prop : ''
  sortState.sortOrder = order === 'ascending' ? 'asc' : order === 'descending' ? 'desc' : ''
  pagination.page = 1
  loadData()
}

const getMatchFieldLabel = (field: string) => {
  const labels: Record<string, string> = {
    CounterpartyName: '对方名称',
    Description: '交易描述',
    Memo: '摘要',
    Amount: '金额'
  }
  return labels[field] || field
}

const getMatchOperatorLabel = (operator: string) => {
  const labels: Record<string, string> = {
    Contains: '包含',
    Equals: '等于',
    Regex: '正则'
  }
  return labels[operator] || operator
}

const handleAdd = () => {
  currentRule.value = null
  dialogVisible.value = true
}

const handleEdit = (row: Rule) => {
  currentRule.value = row
  dialogVisible.value = true
}
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
