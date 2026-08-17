<template>
  <el-dialog
    v-model="visible"
    title="规则重跑"
    width="950px"
    :close-on-click-modal="false"
    @close="handleClose"
  >
    <!-- 参数配置 -->
    <el-form v-if="!previewed" :model="form" label-width="100px" style="max-width: 500px">
      <el-form-item label="时间范围">
        <el-date-picker
          v-model="form.dateRange"
          type="daterange"
          range-separator="至"
          start-placeholder="开始日期"
          end-placeholder="结束日期"
          value-format="YYYY-MM-DD"
          style="width: 100%"
        />
        <div style="color: var(--text-placeholder); font-size: 12px; margin-top: 4px">
          不选择则对全部历史记录执行
        </div>
      </el-form-item>
      <el-form-item label="冲突策略">
        <el-radio-group v-model="form.strategy">
          <el-radio :value="1">
            保守策略
            <el-tooltip content="仅回填空白分类（不覆盖已有分类）" placement="top">
              <el-icon style="margin-left: 4px; color: var(--text-placeholder)"><QuestionFilled /></el-icon>
            </el-tooltip>
          </el-radio>
          <el-radio :value="2">
            覆盖策略
            <el-tooltip content="按最新规则重新分类所有记录（包括已有分类）" placement="top">
              <el-icon style="margin-left: 4px; color: var(--text-placeholder)"><QuestionFilled /></el-icon>
            </el-tooltip>
          </el-radio>
        </el-radio-group>
      </el-form-item>
    </el-form>

    <!-- 预览结果 -->
    <div v-if="previewing" style="text-align: center; padding: 40px 0">
      <el-icon class="is-loading" :size="24"><Loading /></el-icon>
      <p style="color: var(--text-placeholder); margin-top: 12px">正在分析匹配结果...</p>
    </div>

    <div v-else-if="previewed && candidates.length === 0" style="text-align: center; padding: 40px 0">
      <el-empty description="未找到可重新分类的交易记录" />
    </div>

    <div v-else-if="previewed">
      <el-alert
        :title="`共扫描 ${totalAffected} 条记录，其中 ${wouldUpdate} 条将被更新`"
        :type="wouldUpdate > 0 ? 'warning' : 'info'"
        :closable="false"
        show-icon
        style="margin-bottom: 16px"
      />

      <el-table
        ref="tableRef"
        :data="changeCandidates"
        row-key="transactionId"
        style="width: 100%"
        max-height="350"
        @selection-change="handleSelectionChange"
      >
        <el-table-column type="selection" width="45" reserve-selection />
        <el-table-column label="日期" width="110">
          <template #default="{ row }">
            {{ row.transactionDate?.substring(0, 10) }}
          </template>
        </el-table-column>
        <el-table-column label="类型" width="70">
          <template #default="{ row }">
            <TransactionTypeTag :transaction-type="row.transactionType" />
          </template>
        </el-table-column>
        <el-table-column label="金额" width="110" align="right">
          <template #default="{ row }">
            {{ formatRMB(row.amount) }}
          </template>
        </el-table-column>
        <el-table-column label="对方" prop="counterparty" min-width="120" show-overflow-tooltip />
        <el-table-column label="当前分类" width="120">
          <template #default="{ row }">
            <span style="color: var(--text-placeholder)">{{ row.currentCategoryName || '（未分类）' }}</span>
          </template>
        </el-table-column>
        <el-table-column label="" width="40" align="center">
          <template #default>→</template>
        </el-table-column>
        <el-table-column label="新分类" width="120">
          <template #default="{ row }">
            <span style="color: var(--color-success); font-weight: bold">{{ row.newCategoryName || '-' }}</span>
          </template>
        </el-table-column>
      </el-table>
    </div>

    <template #footer>
      <el-button @click="handleClose">取消</el-button>
      <el-button v-if="!previewed" type="primary" :loading="previewing" @click="handlePreview">
        预览匹配结果
      </el-button>
      <template v-else>
        <el-button @click="handleBack">返回修改</el-button>
        <el-button
          type="primary"
          :loading="confirming"
          :disabled="selectedIds.length === 0"
          @click="handleConfirm"
        >
          确认执行 ({{ selectedIds.length }})
        </el-button>
      </template>
    </template>
  </el-dialog>
</template>

<script setup lang="ts">
import { ref, computed, watch, nextTick } from 'vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import { Loading, QuestionFilled } from '@element-plus/icons-vue'
import { previewRuleRerun, confirmRuleRerun } from '@/features/transactions/api/link'
import TransactionTypeTag from '@/shared/ui/TransactionTypeTag.vue'
import { RuleRerunStrategy, type RuleRerunCandidateDto } from '@/features/transactions/types/link'
import { useRuleRerunChangeCandidates } from '@/features/reconciliation/composables/useRuleRerunChangeCandidates'
import { formatRMB } from '@/shared/utils/formatters'

interface Props {
  modelValue: boolean
}

interface Emits {
  (e: 'update:modelValue', value: boolean): void
  (e: 'success'): void
}

const props = defineProps<Props>()
const emit = defineEmits<Emits>()

const previewing = ref(false)
const confirming = ref(false)
const previewed = ref(false)
const candidates = ref<RuleRerunCandidateDto[]>([])
const selectedIds = ref<number[]>([])
const totalAffected = ref(0)
const wouldUpdate = ref(0)
const tableRef = ref()
const { changeCandidates } = useRuleRerunChangeCandidates(candidates)

const form = ref({
  dateRange: null as [string, string] | null,
  strategy: RuleRerunStrategy.Conservative as number
})

const visible = computed({
  get: () => props.modelValue,
  set: (value) => emit('update:modelValue', value)
})

const handleSelectionChange = (rows: RuleRerunCandidateDto[]) => {
  selectedIds.value = rows.map(r => r.transactionId)
}

const handlePreview = async () => {
  previewing.value = true
  try {
    const { data } = await previewRuleRerun({
      startDate: form.value.dateRange?.[0],
      endDate: form.value.dateRange?.[1],
      strategy: form.value.strategy
    })
    const result = data.data!
    candidates.value = result.candidates
    totalAffected.value = result.totalAffected
    wouldUpdate.value = result.wouldUpdate
    previewed.value = true

    // 默认全选将变更的记录
    await nextTick()
    changeCandidates.value.forEach(row => {
      tableRef.value?.toggleRowSelection(row, true)
    })
  } catch (error: any) {
    console.error('预览规则重跑失败:', error)
    ElMessage.error('预览失败')
  } finally {
    previewing.value = false
  }
}

const handleConfirm = async () => {
  if (selectedIds.value.length === 0) return

  try {
    await ElMessageBox.confirm(
      `确认对 ${selectedIds.value.length} 条交易记录重新执行分类规则？`,
      '确认规则重跑',
      { confirmButtonText: '确认执行', cancelButtonText: '取消', type: 'warning' }
    )
  } catch {
    return
  }

  confirming.value = true
  try {
    const { data } = await confirmRuleRerun({
      startDate: form.value.dateRange?.[0],
      endDate: form.value.dateRange?.[1],
      strategy: form.value.strategy,
      transactionIds: selectedIds.value
    })
    ElMessage.success(data.data?.message || '规则重跑完成')
    emit('success')
    handleClose()
  } catch (error: any) {
    console.error('规则重跑失败:', error)
    ElMessage.error('规则重跑失败')
  } finally {
    confirming.value = false
  }
}

const handleBack = () => {
  previewed.value = false
  candidates.value = []
  selectedIds.value = []
}

const handleClose = () => {
  previewed.value = false
  candidates.value = []
  selectedIds.value = []
  form.value = { dateRange: null, strategy: RuleRerunStrategy.Conservative }
  visible.value = false
}

watch(visible, (val) => {
  if (!val) {
    handleClose()
  }
})
</script>
