<template>
  <div class="page-container">
    <!-- 页面头部 -->
    <div class="page-header">
      <div class="page-header-left">
        <h2 class="page-title">标签规则管理</h2>
        <p class="page-desc">配置自动打标签匹配规则，按优先级顺序匹配并为记录添加标签</p>
      </div>
      <div class="page-header-right">
        <el-button v-if="userStore.isAdmin" type="warning" @click="rerunDialogVisible = true">规则重跑</el-button>
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
      <el-table
        :data="tableData"
        style="width: 100%"
        v-loading="loading"
        class="resizable-table"
        border
        allow-drag-last-column
        @header-dragend="handleHeaderDragend"
        @sort-change="handleSortChange"
      >
        <el-table-column
          prop="ruleName"
          label="规则名称"
          sortable="custom"
          :min-width="getColumnMinWidth('ruleName', TABLE_COLUMN_WIDTH.name)"
        />
        <el-table-column
          prop="targetScope"
          label="目标范围"
          :width="getColumnWidth('targetScope', TABLE_COLUMN_WIDTH.account)"
        >
          <template #default="{ row }">
            {{ getScopeLabel(row.targetScope) }}
          </template>
        </el-table-column>
        <el-table-column
          prop="matchField"
          label="匹配字段"
          :width="getColumnWidth('matchField', TABLE_COLUMN_WIDTH.account)"
        >
          <template #default="{ row }">
            {{ getMatchFieldLabel(row.matchField) }}
          </template>
        </el-table-column>
        <el-table-column
          prop="matchOperator"
          label="匹配操作符"
          :width="getColumnWidth('matchOperator', TABLE_COLUMN_WIDTH.account)"
        >
          <template #default="{ row }">
            {{ getMatchOperatorLabel(row.matchOperator) }}
          </template>
        </el-table-column>
        <el-table-column
          prop="matchValue"
          label="匹配值"
          :min-width="getColumnMinWidth('matchValue', TABLE_COLUMN_WIDTH.description)"
          show-overflow-tooltip
        />
        <el-table-column
          label="标签"
          :min-width="getColumnMinWidth('tags', TABLE_COLUMN_WIDTH.description)"
        >
          <template #default="{ row }">
            <div style="display: flex; flex-wrap: wrap; gap: 4px">
              <el-tag
                v-for="tag in row.tags"
                :key="tag.tagId"
                size="small"
                :style="tag.tagColor ? { backgroundColor: tag.tagColor + '20', borderColor: tag.tagColor, color: tag.tagColor } : {}"
              >
                {{ tag.tagName }}
              </el-tag>
              <span v-if="!row.tags || row.tags.length === 0" style="color: var(--text-placeholder)">—</span>
            </div>
          </template>
        </el-table-column>
        <el-table-column
          prop="priority"
          label="优先级"
          :width="getColumnWidth('priority', TABLE_COLUMN_WIDTH.status)"
          sortable="custom"
        />
        <el-table-column
          prop="isActive"
          label="状态"
          :width="getColumnWidth('isActive', TABLE_COLUMN_WIDTH.status)"
        >
          <template #default="{ row }">
            <el-tag :type="row.isActive ? 'success' : 'info'">
              {{ row.isActive ? '启用' : '禁用' }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column
          column-key="actions"
          label="操作"
          :width="getColumnWidth('actions', TABLE_COLUMN_WIDTH.actionTwo)"
          fixed="right"
        >
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

    <TagRuleForm
      v-model="dialogVisible"
      :tag-rule="currentTagRule"
      @success="loadData"
    />
    <TagRuleRerunDialog
      v-model="rerunDialogVisible"
      @success="loadData"
    />
  </div>
</template>

<script setup lang="ts">
import { ref, reactive } from 'vue'
import { getTagRules, deleteTagRule } from '@/features/reconciliation/api/tagRule'
import { useUserStore } from '@/features/auth/stores/user'
import { useListPage } from '@/shared/composables/useListPage'
import type { TagRule } from '@/features/reconciliation/types/tagRule'
import TagRuleForm from '@/features/reconciliation/components/TagRuleForm.vue'
import TagRuleRerunDialog from '@/features/reconciliation/components/TagRuleRerunDialog.vue'
import { TABLE_COLUMN_WIDTH } from '@/shared/constants/table'
import { useResizableTableColumns } from '@/shared/composables/useResizableTableColumns'

const userStore = useUserStore()
const { getColumnWidth, getColumnMinWidth, handleHeaderDragend } = useResizableTableColumns('tag-rule-list')
const dialogVisible = ref(false)
const rerunDialogVisible = ref(false)
const currentTagRule = ref<TagRule | null>(null)

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
} = useListPage<TagRule, { keyword: string; isActive: string | boolean }>({
  fetchData: getTagRules,
  deleteData: deleteTagRule,
  deleteMessage: '确定要删除该标签规则吗？',
  initialSearchForm: { keyword: '', isActive: '' },
  transformParams: (params: any) => {
    if (sortState.sortBy) {
      params.sortBy = sortState.sortBy
      params.sortOrder = sortState.sortOrder
    }
    if (!params.keyword) {
      delete params.keyword
    }
    if (params.isActive === '' || params.isActive === undefined) {
      delete params.isActive
    }
    return params
  }
})

const scopeLabels: Record<string, string> = {
  Transaction: '交易记录'
}

const matchFieldLabels: Record<string, string> = {
  CounterpartyName: '对方名称',
  Description: '描述/摘要',
  Memo: '备注',
  Amount: '金额'
}

const matchOperatorLabels: Record<string, string> = {
  Contains: '包含',
  Equals: '精确匹配',
  Regex: '正则表达式',
  StartsWith: '开头匹配',
  EndsWith: '结尾匹配',
  Range: '区间'
}

const getScopeLabel = (scope: string) => scopeLabels[scope] || scope
const getMatchFieldLabel = (field: string) => matchFieldLabels[field] || field
const getMatchOperatorLabel = (operator: string) => matchOperatorLabels[operator] || operator

const handleSortChange = ({ prop, order }: { prop: string; order: string | null }) => {
  sortState.sortBy = order ? prop : ''
  sortState.sortOrder = order === 'ascending' ? 'asc' : order === 'descending' ? 'desc' : ''
  pagination.page = 1
  loadData()
}

const handleAdd = () => {
  currentTagRule.value = null
  dialogVisible.value = true
}

const handleEdit = (row: TagRule) => {
  currentTagRule.value = row
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

.search-section :deep(.el-form-item__label) {
  color: var(--text-secondary);
  font-size: 13px;
}

.search-section :deep(.el-input__wrapper),
.search-section :deep(.el-select .el-input__wrapper) {
  border-radius: 8px;
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
</style>
