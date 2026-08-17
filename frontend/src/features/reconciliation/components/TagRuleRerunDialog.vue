<template>
  <el-dialog
    v-model="visible"
    title="标签规则重跑"
    :width="dialogWidth"
    :close-on-click-modal="false"
    @close="handleClose"
  >
    <!-- Step 1: 参数配置 -->
    <el-form v-if="step === 'params'" :model="form" label-width="100px" style="max-width: 400px">
      <el-form-item label="目标范围">
        <el-select v-model="form.targetScope" style="width: 100%">
          <el-option label="交易记录" value="Transaction" />
        </el-select>
        <div style="color: var(--text-placeholder); font-size: 12px; margin-top: 4px">
          对所有交易记录预览匹配结果，之后可勾选要实际写入的交易
        </div>
      </el-form-item>
    </el-form>

    <!-- Step 2: 预览 -->
    <div v-else-if="step === 'preview' && previewData">
      <el-alert
        :title="previewAlertTitle"
        type="info"
        show-icon
        :closable="false"
        style="margin-bottom: 12px"
      />

      <div v-if="previewData.candidates.length === 0" style="text-align: center; padding: 24px 0; color: var(--text-placeholder)">
        没有交易会被新增标签（可能都已被打过对应标签，或规则无命中）。
      </div>

      <el-table
        v-else
        :data="previewData.candidates"
        max-height="480"
        row-key="transactionId"
        @selection-change="onSelectionChange"
      >
        <el-table-column type="selection" width="48" reserve-selection />
        <el-table-column prop="transactionDate" label="日期" width="110">
          <template #default="{ row }">{{ formatDate(row.transactionDate) }}</template>
        </el-table-column>
        <el-table-column prop="amount" label="金额" width="120" align="right">
          <template #default="{ row }">{{ formatAmount(row.amount) }}</template>
        </el-table-column>
        <el-table-column prop="counterparty" label="对方" min-width="140" show-overflow-tooltip />
        <el-table-column prop="description" label="描述" min-width="160" show-overflow-tooltip />
        <el-table-column label="命中规则" min-width="160">
          <template #default="{ row }">
            <el-tag
              v-for="r in row.matchedRules"
              :key="r.ruleId"
              type="info"
              size="small"
              style="margin-right: 4px; margin-bottom: 2px"
            >{{ r.ruleName }}</el-tag>
          </template>
        </el-table-column>
        <el-table-column label="将新增标签" min-width="180">
          <template #default="{ row }">
            <el-tag
              v-for="t in row.tagsToAdd"
              :key="t.tagId"
              :color="t.tagColor || undefined"
              :effect="t.tagColor ? 'dark' : 'light'"
              size="small"
              style="margin-right: 4px; margin-bottom: 2px"
            >{{ t.tagName }}</el-tag>
          </template>
        </el-table-column>
      </el-table>

      <div v-if="previewData.candidates.length > 0" style="margin-top: 8px; color: var(--text-placeholder); font-size: 12px">
        已勾选 <strong>{{ selectedIds.length }}</strong> / {{ previewData.candidates.length }} 条
      </div>
    </div>

    <!-- Step 3: 结果 -->
    <el-result
      v-else-if="step === 'result' && result"
      icon="success"
      title="标签规则执行完成"
    >
      <template #sub-title>
        <div style="line-height: 2">
          <div>扫描记录：<strong>{{ result.scannedCount }}</strong> 条</div>
          <div>新增标签：<strong>{{ result.addedCount }}</strong> 条</div>
          <div>跳过记录：<strong>{{ result.skippedCount }}</strong> 条</div>
        </div>
      </template>
    </el-result>

    <template #footer>
      <template v-if="step === 'params'">
        <el-button @click="handleClose">取消</el-button>
        <el-button type="primary" :loading="loading" @click="handlePreview">预览匹配</el-button>
      </template>
      <template v-else-if="step === 'preview'">
        <el-button @click="backToParams">上一步</el-button>
        <el-button
          type="primary"
          :loading="loading"
          :disabled="selectedIds.length === 0"
          @click="handleConfirm"
        >
          确认写入{{ selectedIds.length > 0 ? `（${selectedIds.length} 条）` : '' }}
        </el-button>
      </template>
      <template v-else-if="step === 'result'">
        <el-button type="primary" @click="handleClose">关闭</el-button>
      </template>
    </template>
  </el-dialog>
</template>

<script setup lang="ts">
import { ref, computed, watch } from 'vue'
import { ElMessage } from 'element-plus'
import { previewTagRulesRerun, confirmTagRulesRerun } from '@/features/reconciliation/api/tagRule'
import type {
  RerunPreviewResponse,
  RerunConfirmResponse,
  RerunCandidate
} from '@/features/reconciliation/types/tagRule'

interface Props {
  modelValue: boolean
}

const props = defineProps<Props>()
const emit = defineEmits<{
  (e: 'update:modelValue', value: boolean): void
  (e: 'success'): void
}>()

type Step = 'params' | 'preview' | 'result'

const step = ref<Step>('params')
const loading = ref(false)
const previewData = ref<RerunPreviewResponse | null>(null)
const selectedIds = ref<number[]>([])
const result = ref<RerunConfirmResponse | null>(null)

const form = ref({
  targetScope: 'Transaction'
})

const visible = computed({
  get: () => props.modelValue,
  set: (value) => emit('update:modelValue', value)
})

const dialogWidth = computed(() => step.value === 'preview' ? '1000px' : '500px')

const previewAlertTitle = computed(() => {
  if (!previewData.value) return ''
  const { totalScanned, totalAffected, totalTagsToAdd } = previewData.value
  return `扫描 ${totalScanned} 条交易，${totalAffected} 条会被新增标签，共将新增 ${totalTagsToAdd} 个标签。勾选要执行的交易后点"确认写入"。`
})

const formatDate = (value: string) => value ? value.slice(0, 10) : ''
const formatAmount = (value: number) => {
  if (value == null) return ''
  return value.toLocaleString('zh-CN', { minimumFractionDigits: 2, maximumFractionDigits: 2 })
}

const onSelectionChange = (rows: RerunCandidate[]) => {
  selectedIds.value = rows.map(r => r.transactionId)
}

const resetAll = () => {
  step.value = 'params'
  previewData.value = null
  selectedIds.value = []
  result.value = null
  form.value = { targetScope: 'Transaction' }
}

const handlePreview = async () => {
  loading.value = true
  try {
    const { data } = await previewTagRulesRerun({ targetScope: form.value.targetScope })
    previewData.value = data.data!
    // 默认全选所有候选交易
    selectedIds.value = previewData.value.candidates.map(c => c.transactionId)
    step.value = 'preview'
  } catch (error) {
    console.error('预览标签规则失败:', error)
    ElMessage.error('预览失败')
  } finally {
    loading.value = false
  }
}

const handleConfirm = async () => {
  if (selectedIds.value.length === 0) return
  loading.value = true
  try {
    const { data } = await confirmTagRulesRerun({
      targetScope: form.value.targetScope,
      transactionIds: selectedIds.value
    })
    result.value = data.data!
    step.value = 'result'
    emit('success')
  } catch (error) {
    console.error('确认标签规则重跑失败:', error)
    ElMessage.error('执行失败')
  } finally {
    loading.value = false
  }
}

const backToParams = () => {
  step.value = 'params'
  previewData.value = null
  selectedIds.value = []
}

const handleClose = () => {
  visible.value = false
}

watch(visible, (val) => {
  if (!val) resetAll()
})
</script>
