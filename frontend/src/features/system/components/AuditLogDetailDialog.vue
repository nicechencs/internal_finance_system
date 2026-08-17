<template>
  <el-dialog
    v-model="visible"
    title="审计日志详情"
    width="720px"
    :close-on-click-modal="true"
    @close="handleClose"
  >
    <div v-loading="loading" class="audit-detail">
      <!-- 基本信息 -->
      <div class="detail-section">
        <div class="detail-grid">
          <div class="detail-item">
            <span class="detail-label">日志编号</span>
            <span class="detail-value">#{{ detail?.id }}</span>
          </div>
          <div class="detail-item">
            <span class="detail-label">操作时间</span>
            <span class="detail-value">{{ detail?.createdAt ? formatDateTime(detail.createdAt) : '-' }}</span>
          </div>
          <div class="detail-item">
            <span class="detail-label">操作类型</span>
            <span class="detail-value">
              <el-tag :type="getActionTagType(detail?.action)" size="small">
                {{ getActionLabel(detail?.action) }}
              </el-tag>
            </span>
          </div>
          <div class="detail-item">
            <span class="detail-label">实体类型</span>
            <span class="detail-value">{{ getEntityTypeLabel(detail?.entityType) }}</span>
          </div>
          <div class="detail-item">
            <span class="detail-label">实体ID</span>
            <span class="detail-value">{{ detail?.entityId ?? '-' }}</span>
          </div>
          <div class="detail-item">
            <span class="detail-label">操作人</span>
            <span class="detail-value">{{ detail?.operatorName }}</span>
          </div>
          <div class="detail-item">
            <span class="detail-label">IP 地址</span>
            <span class="detail-value">{{ detail?.ipAddress || '-' }}</span>
          </div>
        </div>
      </div>

      <!-- 变更内容 -->
      <div class="detail-section">
        <h4 class="section-title">变更内容</h4>

        <!-- Update: 新旧值对比 -->
        <template v-if="detail?.action === 'Update' && diffFields.length > 0">
          <el-table :data="diffFields" border size="small" class="diff-table">
            <el-table-column label="字段" prop="label" width="140" />
            <el-table-column label="旧值" min-width="200">
              <template #default="{ row }">
                <span class="diff-old">{{ row.oldDisplay }}</span>
              </template>
            </el-table-column>
            <el-table-column label="新值" min-width="200">
              <template #default="{ row }">
                <span class="diff-new">{{ row.newDisplay }}</span>
              </template>
            </el-table-column>
          </el-table>
        </template>

        <!-- Create: 显示新值 -->
        <template v-else-if="detail?.action === 'Create' && newValueFields.length > 0">
          <el-table :data="newValueFields" border size="small">
            <el-table-column label="字段" prop="label" width="140" />
            <el-table-column label="值" prop="display" min-width="300" />
          </el-table>
        </template>

        <!-- Delete: 显示旧值 -->
        <template v-else-if="detail?.action === 'Delete' && oldValueFields.length > 0">
          <el-table :data="oldValueFields" border size="small">
            <el-table-column label="字段" prop="label" width="140" />
            <el-table-column label="删除前的值" prop="display" min-width="300" />
          </el-table>
        </template>

        <!-- 其他操作：显示原始数据（用 computed 避免模板重复调用） -->
        <template v-else-if="detail?.oldValue || detail?.newValue">
          <div v-if="rawOldFields.length > 0" class="raw-section">
            <div class="raw-label">旧值</div>
            <el-table :data="rawOldFields" border size="small">
              <el-table-column label="字段" prop="label" width="140" />
              <el-table-column label="值" prop="display" min-width="300" />
            </el-table>
          </div>
          <pre v-else-if="detail?.oldValue" class="raw-json">{{ formatJson(detail.oldValue) }}</pre>

          <div v-if="rawNewFields.length > 0" class="raw-section">
            <div class="raw-label">新值</div>
            <el-table :data="rawNewFields" border size="small">
              <el-table-column label="字段" prop="label" width="140" />
              <el-table-column label="值" prop="display" min-width="300" />
            </el-table>
          </div>
          <pre v-else-if="detail?.newValue" class="raw-json">{{ formatJson(detail.newValue) }}</pre>
        </template>

        <template v-else>
          <el-empty description="无变更数据" :image-size="60" />
        </template>
      </div>
    </div>
  </el-dialog>
</template>

<script setup lang="ts">
import { computed, ref, watch } from 'vue'
import { getAuditLogById } from '@/features/system/api/auditLog'
import type { AuditLog } from '@/features/system/types/auditLog'
import { formatDateTime } from '@/shared/utils/formatters'
import {
  getActionLabel, getActionTagType, getEntityTypeLabel,
  parseFieldList, buildDiffFields,
} from '@/features/system/utils/auditLogHelpers'
import { ElMessage } from 'element-plus'

const props = defineProps<{
  modelValue: boolean
  auditLogId: number | null
}>()

const emit = defineEmits<{
  'update:modelValue': [value: boolean]
}>()

const visible = computed({
  get: () => props.modelValue,
  set: (val) => emit('update:modelValue', val)
})

const loading = ref(false)
const detail = ref<AuditLog | null>(null)

// 合并为单个 watch，避免双 watch 触发重复请求
watch(
  [() => props.modelValue, () => props.auditLogId],
  async ([isVisible, id]) => {
    if (isVisible && id) {
      await loadDetail(id)
    }
  }
)

const loadDetail = async (id: number) => {
  loading.value = true
  try {
    const { data } = await getAuditLogById(id)
    detail.value = data.data
  } catch {
    ElMessage.error('加载审计日志详情失败')
  } finally {
    loading.value = false
  }
}

const handleClose = () => {
  detail.value = null
}

// ── computed：避免模板中重复 JSON.parse ──
const entityType = computed(() => detail.value?.entityType)
const oldValueFields = computed(() => parseFieldList(detail.value?.oldValue, entityType.value))
const newValueFields = computed(() => parseFieldList(detail.value?.newValue, entityType.value))
const diffFields = computed(() => buildDiffFields(detail.value?.oldValue, detail.value?.newValue, entityType.value))

// 其他操作的 fallback 展示（也用 computed 缓存）
const rawOldFields = computed(() => parseFieldList(detail.value?.oldValue, entityType.value))
const rawNewFields = computed(() => parseFieldList(detail.value?.newValue, entityType.value))

const formatJson = (value: string): string => {
  try {
    return JSON.stringify(JSON.parse(value), null, 2)
  } catch {
    return value
  }
}
</script>

<style scoped>
.audit-detail {
  max-height: 65vh;
  overflow-y: auto;
}

.detail-section {
  margin-bottom: 20px;
}

.detail-section:last-child {
  margin-bottom: 0;
}

.section-title {
  font-size: 14px;
  font-weight: 600;
  color: var(--text-primary);
  margin: 0 0 12px 0;
  padding-bottom: 8px;
  border-bottom: 1px solid var(--el-border-color-lighter);
}

.detail-grid {
  display: grid;
  grid-template-columns: repeat(2, 1fr);
  gap: 12px 24px;
}

.detail-item {
  display: flex;
  align-items: center;
  gap: 8px;
}

.detail-label {
  font-size: 13px;
  color: var(--text-secondary);
  white-space: nowrap;
  min-width: 70px;
}

.detail-value {
  font-size: 13px;
  color: var(--text-primary);
  word-break: break-all;
}

.diff-table :deep(.diff-old) {
  color: var(--el-color-danger);
  text-decoration: line-through;
}

.diff-table :deep(.diff-new) {
  color: var(--el-color-success);
  font-weight: 500;
}

.raw-section {
  margin-bottom: 12px;
}

.raw-section:last-child {
  margin-bottom: 0;
}

.raw-label {
  font-size: 13px;
  color: var(--text-secondary);
  margin-bottom: 6px;
  font-weight: 500;
}

.raw-json {
  background: var(--bg-page);
  border: 1px solid var(--el-border-color-lighter);
  border-radius: 6px;
  padding: 12px;
  font-size: 12px;
  line-height: 1.6;
  overflow-x: auto;
  white-space: pre-wrap;
  word-break: break-all;
  color: var(--text-primary);
  margin: 0;
}
</style>
