<template>
  <el-dialog
    v-model="visible"
    title="标记为内部转账"
    width="720px"
    :close-on-click-modal="false"
    @close="handleClose"
  >
    <div class="convert-transfer-note">
      转为内部转账用于标记活期/定期之间的互转、定期支取或导入流水后的匹配，本流程不会产生成本或经营收入/支出。
    </div>
    <div v-if="transaction" class="dialog-body">
      <el-alert
        type="info"
        :closable="false"
        show-icon
      >
        <template #default>
          <div class="alert-content">
            <div class="alert-main">将此交易标记为账户间资金转移，不计入收支统计。</div>
            <div class="alert-detail">适用于转存定期、活期互转、跨行转账等场景。</div>
            <div class="alert-impact">
              <span class="impact-label">统计影响：</span>
              <span class="impact-text">总收入/总支出不变，转账总额增加，净收益不受影响。</span>
            </div>
          </div>
        </template>
      </el-alert>

      <div class="source-card">
        <div class="source-title">当前流水</div>
        <div class="source-grid">
          <div>账户：{{ transaction.accountName }}</div>
          <div>日期：{{ formatDate(transaction.transactionDate) }}</div>
          <div>金额：{{ formatAmount(transaction.amount) }}</div>
          <div>方向：{{ transaction.transactionType === 'Expense' ? '转出侧' : '转入侧' }}</div>
        </div>
        <div class="source-desc">摘要：{{ transaction.description || transaction.counterparty || '-' }}</div>
      </div>

      <el-form label-width="100px">
        <el-form-item label="目标账户" required>
          <el-select
            v-model="form.targetAccountId"
            placeholder="选择目标账户"
            filterable
            :loading="accountsLoading"
            style="width: 100%"
          >
            <el-option
              v-for="account in targetAccounts"
              :key="account.id"
              :label="formatTransferAccountLabel(account)"
              :value="account.id"
            />
          </el-select>
        </el-form-item>

        <el-form-item label="转账备注">
          <el-input
            v-model="form.description"
            type="textarea"
            :rows="3"
            placeholder="留空则自动生成“转账至/转账自 + 对方账户名”"
            maxlength="300"
            show-word-limit
          />
        </el-form-item>
      </el-form>

      <div v-if="form.targetAccountId" class="candidate-section">
        <div class="candidate-header">
          <span>目标流水处理方式</span>
          <el-tag v-if="candidateLoading" size="small" type="info">加载中</el-tag>
        </div>

        <el-radio-group v-model="selectionMode" class="candidate-list">
          <el-radio value="create" class="candidate-item">
            <div class="candidate-main">
              <div class="candidate-title">自动补建目标账户交易</div>
              <div class="candidate-desc">如果对方账户没有导入对应流水，系统会自动创建另一腿转账记录。</div>
            </div>
          </el-radio>

          <el-radio
            v-for="candidate in candidates"
            :key="candidate.id"
            :value="String(candidate.id)"
            class="candidate-item"
          >
            <div class="candidate-main">
              <div class="candidate-title">
                匹配现有流水
                <el-tag size="small" type="success">{{ candidate.accountName }}</el-tag>
              </div>
              <div class="candidate-meta">
                {{ formatDate(candidate.transactionDate) }} · {{ formatAmount(candidate.amount) }} · {{ candidate.description || candidate.counterparty || '-' }}
              </div>
            </div>
          </el-radio>
        </el-radio-group>

        <el-empty
          v-if="!candidateLoading && candidates.length === 0"
          description="没有找到可配对的候选流水，提交后会自动补建目标账户交易。"
        />
      </div>
    </div>

    <template #footer>
      <el-button @click="handleClose">取消</el-button>
      <el-button type="primary" :loading="submitting" @click="handleSubmit">确认标记</el-button>
    </template>
  </el-dialog>
</template>

<script setup lang="ts">
import { computed, reactive, ref, watch } from 'vue'
import { ElMessage } from 'element-plus'
import { convertTransactionToTransfer, getTransferCandidates } from '@/features/transactions/api/transaction'
import { getActiveAccounts } from '@/features/master-data/accounts/api/account'
import type { Account } from '@/features/master-data/accounts/types/account'
import type { Transaction } from '@/features/transactions/types/transaction'
import { formatTransferAccountLabel, mergeTransferAccounts } from '@/features/transactions/utils/transferAccounts'
import { formatDateTime } from '@/shared/utils/formatters'

interface Props {
  modelValue: boolean
  transaction: Transaction | null
  accounts: Account[]
}

interface Emits {
  (e: 'update:modelValue', value: boolean): void
  (e: 'success'): void
}

const props = defineProps<Props>()
const emit = defineEmits<Emits>()

const visible = computed({
  get: () => props.modelValue,
  set: (value: boolean) => emit('update:modelValue', value)
})

const form = reactive({
  targetAccountId: undefined as number | undefined,
  description: ''
})

const accountsLoading = ref(false)
const activeAccounts = ref<Account[]>([])
const candidates = ref<Transaction[]>([])
const candidateLoading = ref(false)
const submitting = ref(false)
const selectionMode = ref('create')

const selectableAccounts = computed(() => {
  return mergeTransferAccounts(activeAccounts.value, props.accounts)
})

const targetAccounts = computed(() => {
  return selectableAccounts.value.filter(account => account.id !== props.transaction?.accountId)
})

const loadTargetAccounts = async () => {
  accountsLoading.value = true

  try {
    const { data } = await getActiveAccounts()
    activeAccounts.value = data.data
  } catch (error) {
    console.error('加载活跃账户失败:', error)
  } finally {
    accountsLoading.value = false
  }
}

watch(
  () => [visible.value, form.targetAccountId, props.transaction?.id] as const,
  async ([isVisible, targetAccountId, transactionId]) => {
    if (!isVisible || !targetAccountId || !transactionId) {
      candidates.value = []
      selectionMode.value = 'create'
      return
    }

    candidateLoading.value = true
    try {
      const { data } = await getTransferCandidates(transactionId, targetAccountId)
      candidates.value = data.data
    } catch (error: any) {
      candidates.value = []
      console.error('候选流水加载失败:', error)
    } finally {
      candidateLoading.value = false
      selectionMode.value = 'create'
    }
  }
)

watch(visible, async (isVisible) => {
  if (isVisible) {
    form.targetAccountId = undefined
    form.description = ''
    candidates.value = []
    selectionMode.value = 'create'
    await loadTargetAccounts()
  }
})

const handleSubmit = async () => {
  if (!props.transaction) return
  if (!form.targetAccountId) {
    ElMessage.warning('请选择目标账户')
    return
  }

  submitting.value = true
  try {
    await convertTransactionToTransfer(props.transaction.id, {
      targetAccountId: form.targetAccountId,
      matchedTransactionId: selectionMode.value === 'create' ? undefined : Number(selectionMode.value),
      description: form.description.trim() || undefined
    })

    ElMessage.success('已标记为内部转账')
    emit('success')
    handleClose()
  } catch (error: any) {
    console.error('标记为内部转账失败:', error)
  } finally {
    submitting.value = false
  }
}

const handleClose = () => {
  visible.value = false
}

const formatDate = (value: string) => formatDateTime(value, 'date')
const formatAmount = (value: number) => `¥${value.toFixed(2)}`
</script>

<style scoped>
.dialog-body {
  display: flex;
  flex-direction: column;
  gap: 16px;
}

.alert-content {
  display: flex;
  flex-direction: column;
  gap: 6px;
}

.alert-main {
  font-weight: 500;
  color: var(--text-primary);
  line-height: 1.5;
}

.alert-detail {
  color: var(--text-secondary);
  font-size: 13px;
  line-height: 1.5;
}

.alert-impact {
  margin-top: 4px;
  padding: 8px 12px;
  background: rgba(64, 158, 255, 0.08);
  border-radius: 6px;
  font-size: 13px;
  line-height: 1.5;
}

.impact-label {
  font-weight: 500;
  color: var(--text-primary);
}

.impact-text {
  color: var(--text-secondary);
}

.convert-transfer-note {
  margin: 0 24px 12px;
  padding: 12px 14px;
  border-radius: 10px;
  border: 1px dashed var(--border-light);
  background: var(--bg-page);
  color: var(--text-secondary);
  font-size: 13px;
}

.source-card {
  padding: 16px;
  border: 1px solid var(--border-base);
  border-radius: 12px;
  background: var(--bg-page);
}

.source-title {
  font-weight: 600;
  color: var(--text-primary);
  margin-bottom: 8px;
}

.source-grid {
  display: grid;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  gap: 8px 16px;
  color: var(--text-secondary);
  font-size: 13px;
}

.source-desc {
  margin-top: 10px;
  color: var(--text-secondary);
  font-size: 13px;
}

.candidate-section {
  border-top: 1px solid var(--border-base);
  padding-top: 16px;
}

.candidate-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: 12px;
  color: var(--text-primary);
  font-weight: 600;
}

.candidate-list {
  display: flex;
  flex-direction: column;
  gap: 10px;
  width: 100%;
}

.candidate-item {
  margin-right: 0;
  border: 1px solid var(--border-base);
  border-radius: 12px;
  padding: 12px 14px;
  width: 100%;
}

.candidate-main {
  display: flex;
  flex-direction: column;
  gap: 6px;
}

.candidate-title {
  display: flex;
  align-items: center;
  gap: 8px;
  color: var(--text-primary);
  font-weight: 600;
}

.candidate-desc,
.candidate-meta {
  color: var(--text-secondary);
  font-size: 13px;
  line-height: 1.5;
}

@media (max-width: 768px) {
  .source-grid {
    grid-template-columns: 1fr;
  }
}
</style>
