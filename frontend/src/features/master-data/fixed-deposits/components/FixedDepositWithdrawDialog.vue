<template>
  <el-dialog
    :model-value="modelValue"
    title="支取定期存款"
    width="720px"
    @close="handleClose"
  >
    <div v-if="deposit" class="withdraw-header-card">
      <div class="withdraw-header-main">
        <div class="withdraw-title">{{ deposit.accountName }}</div>
        <div class="withdraw-meta">
          <span>本金 {{ formatMoney(deposit.principal) }}</span>
          <span>利率 {{ formatRate(deposit.interestRate) }}</span>
          <span>到期 {{ formatDate(deposit.maturityDate) }}</span>
        </div>
      </div>
      <el-tag :type="statusTagType">{{ statusText }}</el-tag>
    </div>

    <el-alert
      type="warning"
      :closable="false"
      show-icon
      class="maturity-alert"
    >
      <template #default>
        <div style="font-size: 13px; line-height: 1.6;">
          <strong>建议先完成账户/内部转账或导入流水匹配，再执行定期资金取出。</strong>
          <br />
          推荐顺序如下：
          <ul style="margin: 8px 0 0 20px;">
            <li>优先通过账户转账或内部转账在活期与定期账户之间调拨资金。</li>
            <li>如需人工处理，请在普通交易中标记为内部转账，并使用“转为内部转账”同步定期记录。</li>
            <li>导入流水并关联后再发起取款，避免直接新建定期支出交易。</li>
          </ul>
        </div>
      </template>
    </el-alert>

    <el-form
      ref="formRef"
      :model="form"
      :rules="rules"
      label-width="100px"
      class="withdraw-form"
    >
      <el-form-item label="关联交易" prop="transactionId" required>
        <el-select
          v-model="form.transactionId"
          placeholder="选择关联的交易记录"
          filterable
          :loading="candidatesLoading"
          style="width: 100%"
          @focus="loadCandidates"
        >
          <el-option
            v-for="transaction in candidates"
            :key="transaction.id"
            :label="formatTransactionLabel(transaction)"
            :value="transaction.id"
          >
            <div class="transaction-option">
              <div class="transaction-main">
                <span class="transaction-date">{{ formatDate(transaction.transactionDate) }}</span>
                <span class="transaction-amount">{{ formatMoney(transaction.amount) }}</span>
                <el-tag v-if="transaction.transactionType === 'Transfer'" size="small" type="success">转账</el-tag>
                <el-tag v-else size="small">支出</el-tag>
              </div>
              <div class="transaction-desc">{{ transaction.description || transaction.counterparty || '-' }}</div>
            </div>
          </el-option>
        </el-select>
        <div v-if="candidatesError" class="field-tip error-tip">
          {{ candidatesError }}。请先通过账户转账/标记为内部转账或导入流水完成匹配，再回到此处提取。
        </div>
        <div v-else-if="!candidatesLoading && candidates.length === 0" class="field-tip error-tip">
          未找到可关联的记录。请先完成账户转账/内部转账或导入流水后再次尝试。
        </div>
        <div v-else-if="!candidatesLoading" class="field-tip">
          系统会列出匹配的转账、内部转账或导入流水候选，先使用匹配项再执行取款；如无候选，请先完成转账或导入后重试。
        </div>
      </el-form-item>

      <el-form-item label="支取日期" prop="withdrawalDate">
        <el-date-picker
          v-model="form.withdrawalDate"
          type="date"
          placeholder="请选择支取日期"
          format="YYYY-MM-DD"
          value-format="YYYY-MM-DD"
          style="width: 100%"
        />
      </el-form-item>

      <el-form-item label="实际利息" prop="actualInterest">
        <el-input-number
          v-model="form.actualInterest"
          :precision="2"
          :step="100"
          :min="0"
          :controls="false"
          style="width: 100%"
          placeholder="留空则按系统规则自动计算"
        />
        <div class="field-tip">可按银行实际结息录入；不填则由后端自动计算。</div>
      </el-form-item>
    </el-form>

    <div v-if="deposit" class="settlement-preview">
      <div class="preview-item">
        <span class="preview-label">本金</span>
        <span class="preview-value">{{ formatMoney(deposit.principal) }}</span>
      </div>
      <div class="preview-item">
        <span class="preview-label">预计收益</span>
        <span class="preview-value is-soft">{{ formatMoney(deposit.expectedInterest) }}</span>
      </div>
      <div class="preview-item emphasis">
        <span class="preview-label">预期总额</span>
        <span class="preview-value">{{ formatMoney(deposit.principal + deposit.expectedInterest) }}</span>
      </div>
    </div>

    <template #footer>
      <el-button @click="handleClose">取消</el-button>
      <el-button
        type="primary"
        :loading="submitting"
        :disabled="!form.transactionId"
        @click="handleSubmit"
      >
        确认支取
      </el-button>
    </template>
  </el-dialog>
</template>

<script setup lang="ts">
import { computed, reactive, ref, watch } from 'vue'
import { ElMessage } from 'element-plus'
import type { FormInstance, FormRules } from 'element-plus'
import dayjs from 'dayjs'
import { formatDateTime, formatMoney } from '@/shared/utils/formatters'
import { withdrawFixedDeposit, getWithdrawalCandidates } from '@/features/master-data/fixed-deposits/api/fixedDeposit'
import type { FixedDeposit, WithdrawFixedDepositRequest } from '@/features/master-data/fixed-deposits/types/fixedDeposit'
import type { Transaction } from '@/features/transactions/types/transaction'

interface Props {
  modelValue: boolean
  deposit: FixedDeposit | null
}

const props = defineProps<Props>()
const emit = defineEmits<{
  'update:modelValue': [value: boolean]
  success: []
}>()

const formRef = ref<FormInstance>()
const submitting = ref(false)
const candidatesLoading = ref(false)
const candidatesError = ref('')
const candidates = ref<Transaction[]>([])

const form = reactive<WithdrawFixedDepositRequest>({
  withdrawalDate: '',
  actualInterest: undefined,
  transactionId: 0
})

const rules: FormRules<WithdrawFixedDepositRequest> = {
  transactionId: [{ required: true, message: '请选择关联的交易记录', trigger: 'change', type: 'number', min: 1 }],
  withdrawalDate: [{ required: true, message: '请选择支取日期', trigger: 'change' }],
  actualInterest: [{ type: 'number', min: 0, message: '实际利息不能小于 0', trigger: 'blur' }]
}

const normalizedStatus = computed(() => {
  if (!props.deposit) return 'Active'
  if (props.deposit.status === 'Withdrawn') return 'Withdrawn'
  if (dayjs(props.deposit.maturityDate).isBefore(dayjs(), 'day')) return 'Matured'
  return 'Active'
})

const statusText = computed(() => {
  if (normalizedStatus.value === 'Withdrawn') return '已支取'
  if (normalizedStatus.value === 'Matured') return '已到期'
  return '存续中'
})

const statusTagType = computed(() => {
  if (normalizedStatus.value === 'Withdrawn') return 'info'
  if (normalizedStatus.value === 'Matured') return 'warning'
  return 'success'
})

const loadCandidates = async () => {
  if (!props.deposit || candidates.value.length > 0) return

  candidatesLoading.value = true
  candidatesError.value = ''

  try {
    const { data } = await getWithdrawalCandidates(props.deposit.id)
    candidates.value = data.data

    if (data.data.length === 0) {
      candidatesError.value = '未找到可关联的交易记录'
    }
  } catch (error: any) {
    console.error('加载候选交易失败:', error)
    candidatesError.value = error.response?.data?.message || '加载候选交易失败，请重试'
    ElMessage.error('加载候选交易失败')
    candidates.value = []
  } finally {
    candidatesLoading.value = false
  }
}

watch(() => props.modelValue, async (visible) => {
  if (!visible) return
  form.withdrawalDate = dayjs().format('YYYY-MM-DD')
  form.actualInterest = props.deposit?.expectedInterest
  form.transactionId = 0
  candidates.value = []
  candidatesError.value = ''
  formRef.value?.clearValidate()

  // 自动加载候选交易
  await loadCandidates()
})

const handleClose = () => {
  emit('update:modelValue', false)
}

const handleSubmit = async () => {
  if (!formRef.value || !props.deposit) return

  const valid = await formRef.value.validate().catch(() => false)
  if (!valid) return

  if (!form.transactionId) {
    ElMessage.warning('请选择关联的交易记录')
    return
  }

  submitting.value = true
  try {
    await withdrawFixedDeposit(props.deposit.id, {
      withdrawalDate: form.withdrawalDate || undefined,
      actualInterest: form.actualInterest,
      transactionId: form.transactionId
    })
    ElMessage.success('定期存款支取成功')
    emit('success')
    handleClose()
  } catch (error) {
    console.error('支取定期存款失败:', error)
  } finally {
    submitting.value = false
  }
}

const formatRate = (value?: number) => {
  if (value == null) return '-'
  return `${value.toFixed(2)}%`
}

const formatDate = (value?: string) => {
  if (!value) return '-'
  return formatDateTime(value, 'date')
}

const formatTransactionLabel = (transaction: Transaction) => {
  return `${formatDate(transaction.transactionDate)} - ${formatMoney(transaction.amount)} - ${transaction.description || transaction.counterparty || '无描述'}`
}
</script>

<style scoped>
.withdraw-header-card {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  gap: 16px;
  margin-bottom: 16px;
  padding: 16px;
  border-radius: 10px;
  border: 1px solid var(--border-light);
  background: linear-gradient(180deg, rgba(245, 158, 11, 0.08), rgba(245, 158, 11, 0.02));
}

.withdraw-title {
  font-size: 16px;
  font-weight: 600;
  color: var(--text-primary);
}

.withdraw-meta {
  display: flex;
  flex-wrap: wrap;
  gap: 12px;
  margin-top: 6px;
  font-size: 12px;
  color: var(--text-secondary);
}

.maturity-alert {
  margin-bottom: 16px;
}

.withdraw-form :deep(.el-form-item) {
  margin-bottom: 18px;
}

.field-tip {
  margin-top: 6px;
  font-size: 12px;
  color: var(--text-placeholder);
  line-height: 1.4;
}

.field-tip.error-tip {
  color: var(--el-color-danger);
  font-weight: 500;
}

.transaction-option {
  display: flex;
  flex-direction: column;
  gap: 4px;
}

.transaction-main {
  display: flex;
  align-items: center;
  gap: 8px;
}

.transaction-date {
  font-size: 13px;
  color: var(--text-secondary);
}

.transaction-amount {
  font-weight: 600;
  color: var(--text-primary);
}

.transaction-desc {
  font-size: 12px;
  color: var(--text-placeholder);
}

.settlement-preview {
  display: grid;
  grid-template-columns: repeat(3, minmax(0, 1fr));
  gap: 12px;
  margin-top: 4px;
}

.preview-item {
  padding: 14px 16px;
  border-radius: 10px;
  background: var(--bg-page);
  border: 1px solid var(--border-light);
}

.preview-item.emphasis {
  background: linear-gradient(180deg, rgba(103, 194, 58, 0.1), rgba(103, 194, 58, 0.03));
  border-color: var(--color-success-light-5);
}

.preview-label {
  display: block;
  font-size: 12px;
  color: var(--text-secondary);
}

.preview-value {
  display: block;
  margin-top: 6px;
  font-size: 18px;
  font-weight: 700;
  color: var(--text-primary);
}

.preview-value.is-soft {
  font-size: 16px;
}

@media (max-width: 768px) {
  .settlement-preview {
    grid-template-columns: 1fr;
  }
}
</style>
