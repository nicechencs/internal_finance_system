<template>
  <el-dialog
    v-model="visible"
    :title="`一键关联 - ${entityName}`"
    width="900px"
    :close-on-click-modal="false"
    @close="handleClose"
  >
    <div v-if="loading" style="text-align: center; padding: 40px 0">
      <el-icon class="is-loading" :size="24"><Loading /></el-icon>
      <p style="color: var(--text-placeholder); margin-top: 12px">正在匹配交易记录...</p>
    </div>

    <div v-else-if="candidates.length === 0" style="text-align: center; padding: 40px 0">
      <el-empty description="未找到可匹配的交易记录" />
      <p style="color: var(--text-placeholder); font-size: 13px; margin-top: 8px">
        系统未找到与「{{ entityName }}」名称匹配且未关联的交易记录
      </p>
    </div>

    <div v-else>
      <el-alert
        :title="`找到 ${candidates.length} 条匹配的交易记录`"
        type="info"
        :closable="false"
        show-icon
        style="margin-bottom: 16px"
      >
        <template #default>
          请勾选需要关联的记录，确认后将自动建立关联关系
        </template>
      </el-alert>

      <el-table
        ref="tableRef"
        :data="candidates"
        style="width: 100%"
        max-height="400"
        @selection-change="handleSelectionChange"
      >
        <el-table-column type="selection" width="45" />
        <el-table-column label="日期" width="110">
          <template #default="{ row }">
            {{ formatDate(row.transactionDate) }}
          </template>
        </el-table-column>
        <el-table-column label="类型" width="70">
          <template #default="{ row }">
            <TransactionTypeTag :transaction-type="row.transactionType" />
          </template>
        </el-table-column>
        <el-table-column label="金额" width="120" align="right">
          <template #default="{ row }">
            <span :style="{ color: row.transactionType === 'Income' ? 'var(--color-success)' : 'var(--color-danger)', fontWeight: 'bold' }">
              {{ row.transactionType === 'Income' ? '+' : '-' }}¥{{ row.amount.toLocaleString('zh-CN', { minimumFractionDigits: 2 }) }}
            </span>
          </template>
        </el-table-column>
        <el-table-column label="对方" prop="counterparty" min-width="130" show-overflow-tooltip />
        <el-table-column label="账户" prop="accountName" width="120" show-overflow-tooltip />
        <el-table-column label="描述" prop="description" min-width="140" show-overflow-tooltip />
        <el-table-column label="匹配原因" prop="matchReason" width="180" show-overflow-tooltip>
          <template #default="{ row }">
            <span style="color: var(--color-warning); font-size: 12px">{{ row.matchReason }}</span>
          </template>
        </el-table-column>
      </el-table>
    </div>

    <template #footer>
      <el-button @click="handleClose">取消</el-button>
      <el-button
        type="primary"
        :loading="confirming"
        :disabled="selectedIds.length === 0"
        @click="handleConfirm"
      >
        确认关联 ({{ selectedIds.length }})
      </el-button>
    </template>
  </el-dialog>
</template>

<script setup lang="ts">
import { ref, computed, watch } from 'vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import { Loading } from '@element-plus/icons-vue'
import { previewLink, confirmLink } from '@/features/transactions/api/link'
import TransactionTypeTag from '@/shared/ui/TransactionTypeTag.vue'
import type { LinkType, LinkCandidateDto } from '@/features/transactions/types/link'

interface Props {
  modelValue: boolean
  linkType: LinkType
  entityId: number
  entityName: string
}

interface Emits {
  (e: 'update:modelValue', value: boolean): void
  (e: 'success'): void
}

const props = defineProps<Props>()
const emit = defineEmits<Emits>()

const loading = ref(false)
const confirming = ref(false)
const candidates = ref<LinkCandidateDto[]>([])
const selectedIds = ref<number[]>([])
const tableRef = ref()

const visible = computed({
  get: () => props.modelValue,
  set: (value) => emit('update:modelValue', value)
})

const formatDate = (dateStr: string) => {
  return dateStr ? dateStr.substring(0, 10) : ''
}

const handleSelectionChange = (rows: LinkCandidateDto[]) => {
  selectedIds.value = rows.map(r => r.transactionId)
}

const loadPreview = async () => {
  if (!props.entityId) return
  loading.value = true
  candidates.value = []
  selectedIds.value = []

  try {
    const { data } = await previewLink({
      linkType: props.linkType,
      entityId: props.entityId
    })
    candidates.value = data.data?.candidates ?? []
    // 默认全选
    if (candidates.value.length > 0) {
      setTimeout(() => {
        candidates.value.forEach(row => {
          tableRef.value?.toggleRowSelection(row, true)
        })
      }, 100)
    }
  } catch (error: any) {
    console.error('预览关联失败:', error)
    ElMessage.error('加载匹配结果失败')
  } finally {
    loading.value = false
  }
}

const handleConfirm = async () => {
  if (selectedIds.value.length === 0) return

  try {
    await ElMessageBox.confirm(
      `确认将 ${selectedIds.value.length} 条交易记录关联到「${props.entityName}」？`,
      '确认关联',
      { confirmButtonText: '确认', cancelButtonText: '取消', type: 'warning' }
    )
  } catch {
    return
  }

  confirming.value = true
  try {
    const { data } = await confirmLink({
      linkType: props.linkType,
      entityId: props.entityId,
      transactionIds: selectedIds.value
    })
    ElMessage.success(data.data?.message || `成功关联 ${data.data?.linkedCount} 条记录`)
    emit('success')
    handleClose()
  } catch (error: any) {
    console.error('确认关联失败:', error)
    ElMessage.error('关联操作失败')
  } finally {
    confirming.value = false
  }
}

const handleClose = () => {
  candidates.value = []
  selectedIds.value = []
  visible.value = false
}

watch(visible, (val) => {
  if (val) {
    loadPreview()
  }
})
</script>
