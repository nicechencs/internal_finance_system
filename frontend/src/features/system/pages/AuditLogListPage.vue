<template>
  <div class="page-container">
    <div class="page-header">
      <div class="page-header-left">
        <h2 class="page-title">审计日志</h2>
        <p class="page-desc">查看系统操作记录</p>
      </div>
    </div>

    <div class="search-section search-section-top">
      <el-form :inline="true" :model="filters" class="search-form" @submit.prevent="handleFilter">
        <el-form-item label="操作类型">
          <el-select v-model="filters.action" placeholder="全部" clearable style="width: 160px">
            <el-option
              v-for="option in actionOptions"
              :key="option.value"
              :label="option.label"
              :value="option.value"
            />
          </el-select>
        </el-form-item>
        <el-form-item label="实体类型">
          <el-select v-model="filters.entityType" placeholder="全部" clearable style="width: 160px">
            <el-option
              v-for="option in entityTypeOptions"
              :key="option.value"
              :label="option.label"
              :value="option.value"
            />
          </el-select>
        </el-form-item>
        <el-form-item label="操作人">
          <el-input
            v-model="filters.username"
            placeholder="输入用户名"
            clearable
            style="width: 160px"
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
        <el-form-item class="search-buttons">
          <el-button type="primary" native-type="submit">查询</el-button>
          <el-button @click="handleReset">重置</el-button>
        </el-form-item>
      </el-form>
    </div>

    <div class="content-section">
      <div class="table-wrapper">
        <el-table
          :data="tableRows"
          v-loading="loading"
          class="resizable-table"
          border
          allow-drag-last-column
          @header-dragend="handleHeaderDragend"
          @row-click="handleRowClick"
          row-class-name="clickable-row"
        >
          <el-table-column type="index" label="序号" width="60" align="center">
            <template #default="{ $index }">
              {{ (pagination.page - 1) * pagination.pageSize + $index + 1 }}
            </template>
          </el-table-column>

          <el-table-column prop="createdAt" label="时间" :width="getColumnWidth('createdAt', TABLE_COLUMN_WIDTH.dateTime)">
            <template #default="{ row }">
              {{ formatDateTime(row.createdAt) }}
            </template>
          </el-table-column>

          <el-table-column prop="action" label="操作" :width="getColumnWidth('action', TABLE_COLUMN_WIDTH.type)">
            <template #default="{ row }">
              <el-tag :type="getActionTagType(row.action)" size="small">
                {{ getActionLabel(row.action) }}
              </el-tag>
            </template>
          </el-table-column>

          <el-table-column prop="entityType" label="实体类型" :width="getColumnWidth('entityType', TABLE_COLUMN_WIDTH.shortText)">
            <template #default="{ row }">
              {{ getEntityTypeLabel(row.entityType) }}
            </template>
          </el-table-column>

          <el-table-column prop="entityId" label="实体ID" :width="getColumnWidth('entityId', 90)">
            <template #default="{ row }">
              {{ row.entityId != null ? row.entityId : '-' }}
            </template>
          </el-table-column>

          <el-table-column prop="operatorName" label="操作人" :width="getColumnWidth('operatorName', TABLE_COLUMN_WIDTH.contact)">
            <template #default="{ row }">
              <el-link
                v-if="row.userId"
                type="primary"
                :underline="false"
                @click.stop="goToUser(row.userId)"
              >
                {{ row.operatorName }}
              </el-link>
              <span v-else>{{ row.operatorName }}</span>
            </template>
          </el-table-column>

          <el-table-column label="变更摘要" :min-width="getColumnMinWidth('summary', 260)" show-overflow-tooltip>
            <template #default="{ row }">
              <span class="change-summary">{{ row._summary }}</span>
            </template>
          </el-table-column>

          <el-table-column label="操作" width="80" align="center" fixed="right">
            <template #default="{ row }">
              <el-button type="primary" link size="small" @click.stop="openDetail(row.id)">
                查看
              </el-button>
            </template>
          </el-table-column>
        </el-table>

        <el-pagination
          v-model:current-page="pagination.page"
          v-model:page-size="pagination.pageSize"
          :total="pagination.total"
          :page-sizes="[10, 20, 50, 100]"
          layout="total, sizes, prev, pager, next, jumper"
          class="pagination"
          @size-change="handleSizeChange"
          @current-change="handlePageChange"
        />
      </div>
    </div>

    <!-- 详情弹窗 -->
    <AuditLogDetailDialog
      v-model="detailDialogVisible"
      :audit-log-id="selectedAuditLogId"
    />
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted, reactive, ref } from 'vue'
import { useRouter } from 'vue-router'
import { ElMessage } from 'element-plus'
import { getAuditLogs } from '@/features/system/api/auditLog'
import { dateRangeShortcuts } from '@/shared/utils/dateShortcuts'
import { formatDateTime } from '@/shared/utils/formatters'
import type { AuditLog, AuditLogQueryParams } from '@/features/system/types/auditLog'
import { TABLE_COLUMN_WIDTH } from '@/shared/constants/table'
import { useResizableTableColumns } from '@/shared/composables/useResizableTableColumns'
import AuditLogDetailDialog from '@/features/system/components/AuditLogDetailDialog.vue'
import {
  ACTION_LABEL_MAP,
  ENTITY_TYPE_LABEL_MAP,
  getActionLabel, getActionTagType, getEntityTypeLabel,
  formatSummaryValue, safeJsonParse,
  PROPERTY_LABEL_MAP, HIDDEN_FIELDS,
} from '@/features/system/utils/auditLogHelpers'

const router = useRouter()
const { getColumnWidth, getColumnMinWidth, handleHeaderDragend } = useResizableTableColumns('audit-log-list')

const loading = ref(false)
const auditLogs = ref<AuditLog[]>([])

// 详情弹窗
const detailDialogVisible = ref(false)
const selectedAuditLogId = ref<number | null>(null)
const actionOptions = Object.entries(ACTION_LABEL_MAP).map(([value, label]) => ({ value, label }))
const entityTypeOptions = Object.entries(ENTITY_TYPE_LABEL_MAP).map(([value, label]) => ({ value, label }))

const filters = reactive({
  action: null as string | null,
  entityType: null as string | null,
  username: '',
  dateRange: null as [Date, Date] | null
})

const formatLocalDate = (date: Date) => formatDateTime(date, 'date')

const pagination = reactive({
  page: 1,
  pageSize: 20,
  total: 0
})

// ── 变更摘要：预计算，避免渲染时重复 JSON.parse ──
const buildSummary = (row: AuditLog): string => {
  const entityLabel = getEntityTypeLabel(row.entityType)
  const actionLabel = getActionLabel(row.action)

  if (row.action === 'Update' && row.oldValue && row.newValue) {
    const oldObj = safeJsonParse(row.oldValue)
    const newObj = safeJsonParse(row.newValue)
    if (oldObj && newObj) {
      const changes: string[] = []
      for (const key of Object.keys(newObj)) {
        if (HIDDEN_FIELDS.has(key)) continue
        if (JSON.stringify(oldObj[key]) !== JSON.stringify(newObj[key])) {
          const label = PROPERTY_LABEL_MAP[key] || key
          changes.push(`${label}: ${formatSummaryValue(key, oldObj[key])} → ${formatSummaryValue(key, newObj[key])}`)
        }
      }
      if (changes.length > 0) {
        const preview = changes.slice(0, 3).join('；')
        const suffix = changes.length > 3 ? ` 等${changes.length}项` : ''
        return `${actionLabel}${entityLabel} #${row.entityId ?? ''}：${preview}${suffix}`
      }
    }
  }

  if (row.action === 'Create' && row.newValue) {
    const obj = safeJsonParse(row.newValue)
    if (obj) {
      const nameField = obj.name || obj.title || obj.description || obj.fullName || obj.userName || obj.username
      if (nameField) {
        return `${actionLabel}${entityLabel} #${row.entityId ?? ''}：${String(nameField).substring(0, 30)}`
      }
    }
  }

  if (row.action === 'Delete' && row.oldValue) {
    const obj = safeJsonParse(row.oldValue)
    if (obj) {
      const nameField = obj.name || obj.title || obj.description || obj.fullName || obj.userName || obj.username
      if (nameField) {
        return `${actionLabel}${entityLabel} #${row.entityId ?? ''}：${String(nameField).substring(0, 30)}`
      }
    }
  }

  return `${actionLabel}${entityLabel}${row.entityId != null ? ' #' + row.entityId : ''}`
}

// 预计算摘要，模板直接读取 _summary 字段
const tableRows = computed(() =>
  auditLogs.value.map(row => ({ ...row, _summary: buildSummary(row) }))
)

// ── 详情弹窗 ──
const openDetail = (id: number) => {
  selectedAuditLogId.value = id
  detailDialogVisible.value = true
}

const handleRowClick = (row: AuditLog & { _summary: string }) => {
  openDetail(row.id)
}

const goToUser = (userId: number) => {
  router.push({ name: 'UserManagement', query: { highlight: String(userId) } })
}

const loadAuditLogs = async () => {
  loading.value = true
  try {
    const params: AuditLogQueryParams = {
      page: pagination.page,
      pageSize: pagination.pageSize
    }

    if (filters.action) params.action = filters.action
    if (filters.entityType) params.entityType = filters.entityType
    if (filters.username.trim()) params.username = filters.username.trim()
    if (filters.dateRange?.length === 2) {
      params.startDate = formatLocalDate(filters.dateRange[0])
      params.endDate = formatLocalDate(filters.dateRange[1])
    }

    const { data } = await getAuditLogs(params)
    auditLogs.value = data.data.items
    pagination.total = data.data.total
  } catch (error) {
    ElMessage.error('加载审计日志失败')
  } finally {
    loading.value = false
  }
}

const handleFilter = () => {
  pagination.page = 1
  loadAuditLogs()
}

const handleReset = () => {
  filters.action = null
  filters.entityType = null
  filters.username = ''
  filters.dateRange = null
  handleFilter()
}

const handleSizeChange = () => {
  loadAuditLogs()
}

const handlePageChange = () => {
  loadAuditLogs()
}

onMounted(() => {
  loadAuditLogs()
})
</script>

<style scoped>
.page-container {
  padding: 0;
}

.page-header {
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
  box-shadow: 0 1px 3px rgba(0, 0, 0, 0.04);
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

.table-wrapper :deep(.clickable-row) {
  cursor: pointer;
}

.table-wrapper :deep(.clickable-row:hover > td.el-table__cell) {
  background-color: var(--el-fill-color-light) !important;
}

.change-summary {
  color: var(--text-secondary);
  font-size: 12px;
}

.pagination {
  padding: 16px 20px;
  justify-content: flex-end;
  border-top: 1px solid var(--bg-hover);
}

@media (max-width: 768px) {
  .page-header {
    flex-direction: column;
    align-items: flex-start;
    gap: 12px;
  }
}
</style>
