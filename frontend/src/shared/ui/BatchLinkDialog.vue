<template>
  <el-dialog
    v-model="visible"
    title="批量智能关联"
    width="1000px"
    :close-on-click-modal="false"
    @close="handleClose"
  >
    <!-- 加载中 -->
    <div v-if="loading" style="text-align: center; padding: 60px 0">
      <el-icon class="is-loading" :size="32"><Loading /></el-icon>
      <p style="color: var(--text-placeholder); margin-top: 16px">正在扫描未关联交易并匹配实体...</p>
    </div>

    <!-- 无匹配 -->
    <div v-else-if="!loading && candidates.length === 0" style="text-align: center; padding: 60px 0">
      <el-empty description="未找到可匹配的交易记录">
        <template #description>
          <p style="color: var(--text-regular)">
            扫描了 <b>{{ totalUnlinked }}</b> 条未关联交易，未能自动匹配到任何实体
          </p>
          <p style="color: var(--text-placeholder); font-size: 13px; margin-top: 4px">
            请检查客户/供应商/人员/项目/账户的名称是否与银行流水对方信息一致
          </p>
        </template>
      </el-empty>
    </div>

    <!-- 匹配结果 -->
    <div v-else>
      <el-alert
        type="info"
        :closable="false"
        show-icon
        style="margin-bottom: 16px"
      >
        <template #title>
          在 <b>{{ totalUnlinked }}</b> 条未关联交易中，找到
          <b>{{ candidates.length }}</b> 条可匹配记录，请确认关联方案
        </template>
        <template #default>
          每行可选择要关联的实体；有多个同名实体时下拉框会显示辅助信息供区分；不需要关联的行请选择"跳过"
        </template>
      </el-alert>

      <el-table :data="candidates" border style="width: 100%" max-height="450">
        <el-table-column label="日期" width="100">
          <template #default="{ row }">{{ formatDate(row.transactionDate) }}</template>
        </el-table-column>
        <el-table-column label="类型" width="65" align="center">
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
        <el-table-column label="对方" prop="counterparty" width="130" show-overflow-tooltip />
        <el-table-column label="账户" prop="accountName" width="110" show-overflow-tooltip />
        <el-table-column label="描述" prop="description" min-width="120" show-overflow-tooltip />
        <el-table-column label="关联到" min-width="230">
          <template #default="{ row }">
            <el-select
              v-model="selections[row.transactionId]"
              placeholder="跳过（不关联）"
              clearable
              style="width: 100%"
            >
              <el-option
                v-for="match in row.matches"
                :key="`${match.entityType}-${match.entityId}`"
                :value="`${match.entityType}:${match.entityId}`"
                :label="formatMatchLabel(match)"
              >
                <div style="display: flex; flex-direction: column; padding: 2px 0">
                  <span>
                    <el-tag :type="getEntityTagType(match.entityType)" size="small" style="margin-right: 6px">
                      {{ getEntityTypeName(match.entityType) }}
                    </el-tag>
                    {{ match.entityName }}
                  </span>
                  <span v-if="match.extraInfo" style="font-size: 12px; color: var(--text-placeholder); margin-top: 2px">
                    {{ match.extraInfo }}
                  </span>
                  <span style="font-size: 11px; color: var(--color-warning); margin-top: 1px">
                    {{ match.matchReason }}
                  </span>
                </div>
              </el-option>
            </el-select>
          </template>
        </el-table-column>
      </el-table>

      <div style="margin-top: 12px; color: var(--text-regular); font-size: 13px">
        已选择关联：<b>{{ selectedCount }}</b> 条，跳过：<b>{{ candidates.length - selectedCount }}</b> 条
      </div>
    </div>

    <template #footer>
      <el-button @click="handleClose">取消</el-button>
      <el-button
        v-if="candidates.length > 0"
        type="primary"
        :loading="confirming"
        :disabled="selectedCount === 0"
        @click="handleConfirm"
      >
        确认关联 ({{ selectedCount }})
      </el-button>
    </template>
  </el-dialog>
</template>

<script setup lang="ts">
import { ref, computed, watch } from 'vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import { Loading } from '@element-plus/icons-vue'
import { previewBatchLink, confirmBatchLink } from '@/features/transactions/api/link'
import TransactionTypeTag from '@/shared/ui/TransactionTypeTag.vue'
import { BatchLinkEntityType } from '@/features/transactions/types/link'
import type { BatchLinkCandidateDto, EntityMatchDto } from '@/features/transactions/types/link'

interface Props {
  modelValue: boolean
}
interface Emits {
  (e: 'update:modelValue', value: boolean): void
  (e: 'success'): void
}

const props = defineProps<Props>()
const emit = defineEmits<Emits>()

const loading = ref(false)
const confirming = ref(false)
const totalUnlinked = ref(0)
const candidates = ref<BatchLinkCandidateDto[]>([])
// key: transactionId, value: "entityType:entityId" 或 undefined（跳过）
const selections = ref<Record<number, string | undefined>>({})

const visible = computed({
  get: () => props.modelValue,
  set: (v) => emit('update:modelValue', v)
})

const selectedCount = computed(() =>
  Object.values(selections.value).filter(v => !!v).length
)

const formatDate = (d: string) => d?.substring(0, 10) ?? ''

const getEntityTypeName = (type: BatchLinkEntityType) => {
  const map: Record<BatchLinkEntityType, string> = {
    [BatchLinkEntityType.Customer]: '客户',
    [BatchLinkEntityType.Supplier]: '供应商',
    [BatchLinkEntityType.Person]: '人员',
    [BatchLinkEntityType.Project]: '项目',
    [BatchLinkEntityType.Account]: '账户'
  }
  return map[type] ?? ''
}

const getEntityTagType = (type: BatchLinkEntityType): 'success' | 'warning' | 'info' | 'primary' | 'danger' => {
  const map: Record<BatchLinkEntityType, 'success' | 'warning' | 'info' | 'primary' | 'danger'> = {
    [BatchLinkEntityType.Customer]: 'success',
    [BatchLinkEntityType.Supplier]: 'warning',
    [BatchLinkEntityType.Person]: 'info',
    [BatchLinkEntityType.Project]: 'primary',
    [BatchLinkEntityType.Account]: 'danger'
  }
  return map[type] ?? 'info'
}

const formatMatchLabel = (match: EntityMatchDto) =>
  `[${getEntityTypeName(match.entityType)}] ${match.entityName}${match.extraInfo ? ' · ' + match.extraInfo : ''}`

const loadPreview = async () => {
  loading.value = true
  candidates.value = []
  selections.value = {}
  totalUnlinked.value = 0

  try {
    const { data } = await previewBatchLink()
    totalUnlinked.value = data.data.totalUnlinked
    candidates.value = data.data.candidates

    // 自动预选：每条交易若只有1个候选则自动选中
    const autoSel: Record<number, string | undefined> = {}
    for (const c of candidates.value) {
      if (c.matches.length === 1) {
        const m = c.matches[0]
        if (m) {
          autoSel[c.transactionId] = `${m.entityType}:${m.entityId}`
        }
      }
    }
    selections.value = autoSel
  } catch {
    ElMessage.error('加载匹配结果失败')
  } finally {
    loading.value = false
  }
}

const handleConfirm = async () => {
  const items = Object.entries(selections.value)
    .filter(([, v]) => !!v)
    .map(([txId, val]) => {
      const [typeStr, idStr] = val!.split(':')
      return {
        transactionId: Number(txId),
        entityType: Number(typeStr),
        entityId: Number(idStr)
      }
    })

  if (items.length === 0) return

  try {
    await ElMessageBox.confirm(
      `确认将 ${items.length} 条交易记录关联到对应实体？`,
      '确认批量关联',
      { confirmButtonText: '确认', cancelButtonText: '取消', type: 'warning' }
    )
  } catch {
    return
  }

  confirming.value = true
  try {
    const { data } = await confirmBatchLink({ items })
    ElMessage.success(data.data?.message || `成功关联 ${data.data?.linkedCount} 条记录`)
    emit('success')
    handleClose()
  } catch {
    ElMessage.error('关联操作失败')
  } finally {
    confirming.value = false
  }
}

const handleClose = () => {
  candidates.value = []
  selections.value = {}
  visible.value = false
}

watch(visible, (val) => {
  if (val) loadPreview()
})
</script>
