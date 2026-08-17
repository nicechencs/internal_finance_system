<template>
  <div v-loading="loading">
    <el-descriptions v-if="receivable" :column="2" border>
      <el-descriptions-item label="项目">
        <el-link
          v-if="receivable.projectId"
          type="primary"
          @click="emit('go-to-entity', 'ProjectDetail', receivable.projectId)"
        >
          {{ receivable.projectName }}
        </el-link>
        <span v-else>{{ receivable.projectName || '-' }}</span>
      </el-descriptions-item>
      <el-descriptions-item label="客户">
        <el-link
          v-if="receivable.customerId"
          type="primary"
          @click="emit('go-to-entity', 'CustomerDetail', receivable.customerId)"
        >
          {{ receivable.customerName }}
        </el-link>
        <span v-else>{{ receivable.customerName || '-' }}</span>
      </el-descriptions-item>
      <el-descriptions-item label="业务类型">
        {{ receivable.receivableTypeName || '-' }}
      </el-descriptions-item>
      <el-descriptions-item label="应收金额">
        <span style="font-size: 18px; font-weight: bold; color: var(--color-primary)">
          {{ formatCurrency(receivable.totalAmount) }}
        </span>
      </el-descriptions-item>
      <el-descriptions-item label="已收金额">
        <span style="font-size: 16px; color: var(--color-success)">
          {{ formatCurrency(receivable.receivedAmount) }}
        </span>
      </el-descriptions-item>
      <el-descriptions-item label="未收金额">
        <span style="font-size: 16px; color: var(--color-danger)">
          {{ formatCurrency(receivable.remainingAmount) }}
        </span>
      </el-descriptions-item>
      <el-descriptions-item label="状态">
        <el-tag :type="getStatusType(receivable.status)">
          {{ getStatusText(receivable.status) }}
        </el-tag>
      </el-descriptions-item>
      <el-descriptions-item label="到期日期">
        <span :class="{ overdue: isOverdue(receivable) }">
          {{ receivable.dueDate ? formatDate(receivable.dueDate) : '-' }}
        </span>
      </el-descriptions-item>
      <el-descriptions-item label="结清日期">
        {{ receivable.settledAt ? formatDateTime(receivable.settledAt) : '-' }}
      </el-descriptions-item>
      <el-descriptions-item label="描述" :span="2">
        {{ receivable.description || '-' }}
      </el-descriptions-item>
      <el-descriptions-item label="标签" :span="2">
        <TagDisplay :tags="receivable.tags || []" />
      </el-descriptions-item>
      <el-descriptions-item label="创建时间" :span="2">
        {{ formatDateTime(receivable.createdAt) }}
      </el-descriptions-item>
    </el-descriptions>

    <div v-if="receivable && receivable.details.length > 0" class="payment-details">
      <el-divider>收款明细</el-divider>
      <el-table
        :data="receivable.details"
        border
        class="resizable-table clickable-rows"
        allow-drag-last-column
        @header-dragend="handleHeaderDragend"
        @row-click="handleViewTransaction"
      >
        <el-table-column prop="transactionId" label="交易" :width="getColumnWidth('transactionId', 120)">
          <template #default="{ row }">
            <el-link
              v-if="row.transactionId > 0"
              type="primary"
              @click.stop="handleViewTransaction(row)"
            >
              #{{ row.transactionId }}
            </el-link>
            <span v-else class="text-gray-400">-</span>
          </template>
        </el-table-column>
        <el-table-column prop="paymentDate" label="收款日期" :width="getColumnWidth('paymentDate', TABLE_COLUMN_WIDTH.date)">
          <template #default="{ row }">
            {{ formatDate(row.paymentDate) }}
          </template>
        </el-table-column>
        <el-table-column prop="amount" label="收款金额" :width="getColumnWidth('amount', TABLE_COLUMN_WIDTH.amount)" align="right">
          <template #default="{ row }">
            {{ formatCurrency(row.amount) }}
          </template>
        </el-table-column>
        <el-table-column prop="paymentMethod" label="支付方式" :width="getColumnWidth('paymentMethod', TABLE_COLUMN_WIDTH.account)">
          <template #default="{ row }">
            {{ row.paymentMethod || '-' }}
          </template>
        </el-table-column>
        <el-table-column prop="description" label="备注" :min-width="getColumnMinWidth('description', TABLE_COLUMN_WIDTH.description)" show-overflow-tooltip />
        <el-table-column prop="createdAt" label="登记时间" :width="getColumnWidth('createdAt', TABLE_COLUMN_WIDTH.dateTime)">
          <template #default="{ row }">
            {{ formatDateTime(row.createdAt) }}
          </template>
        </el-table-column>
      </el-table>
    </div>

    <div v-if="userStore.canEdit && receivable && receivable.status !== 'settled'" class="payment-form">
      <el-divider>收款登记</el-divider>
      <el-form :model="paymentForm" :rules="paymentRules" :ref="setPaymentFormInstance" label-width="100px">
        <el-form-item label="交易筛选">
          <div style="display: flex; gap: 8px; align-items: center; width: 100%">
            <el-checkbox v-model="showAllTransactions" @change="loadAvailableTransactions">显示全部</el-checkbox>
            <el-input
              v-model="transactionKeyword"
              placeholder="按备注/银行对方/摘要搜索"
              clearable
              style="flex: 1"
              @keyup.enter="loadAvailableTransactions"
              @clear="loadAvailableTransactions"
            >
              <template #append>
                <el-button @click="loadAvailableTransactions">搜索</el-button>
              </template>
            </el-input>
          </div>
        </el-form-item>
        <el-form-item label="选择交易" prop="transactionId">
          <el-select
            v-model="paymentForm.transactionId"
            filterable
            placeholder="搜索交易..."
            style="width: 100%"
            popper-class="finance-transaction-select-dropdown"
            :loading="loadingTransactions"
            @change="onTransactionSelected"
          >
            <el-option
              v-for="tx in availableTransactions"
              :key="tx.id"
              :label="getTransactionOptionLabel(tx)"
              :value="tx.id"
            >
              <div class="transaction-option">
                <div class="transaction-option__header">
                  <span>{{ formatDate(tx.transactionDate) }} - {{ formatCurrency(tx.amount) }}</span>
                  <span class="transaction-option__available">可用: {{ formatCurrency(tx.availableAmount || 0) }}</span>
                </div>
                <div class="transaction-option__meta">
                  <span>付款方：{{ getPrimaryCounterpartyText(tx) }}</span>
                  <span>账户：{{ tx.accountName || '-' }}</span>
                  <span v-if="tx.projectName">项目：{{ tx.projectName }}</span>
                </div>
                <div v-if="getBankCounterpartyText(tx)" class="transaction-option__extra">
                  银行对方：{{ getBankCounterpartyText(tx) }}
                </div>
                <div v-if="getRemarkText(tx)" class="transaction-option__extra">
                  备注/摘要：{{ getRemarkText(tx) }}
                </div>
              </div>
            </el-option>
          </el-select>
          <div v-if="selectedTransaction" class="selected-transaction-summary">
            <div class="selected-transaction-summary__title">已选交易详情</div>
            <div class="selected-transaction-summary__header">
              <span>#{{ selectedTransaction.id }}</span>
              <span>{{ formatDate(selectedTransaction.transactionDate) }}</span>
              <span>{{ formatCurrency(selectedTransaction.amount) }}</span>
              <span class="transaction-option__available">
                可用: {{ formatCurrency(selectedTransaction.availableAmount || 0) }}
              </span>
            </div>
            <div class="selected-transaction-summary__meta">
              <span>付款方：{{ getPrimaryCounterpartyText(selectedTransaction) }}</span>
              <span>账户：{{ selectedTransaction.accountName || '-' }}</span>
              <span v-if="selectedTransaction.projectName">项目：{{ selectedTransaction.projectName }}</span>
            </div>
            <div v-if="getBankCounterpartyText(selectedTransaction)" class="selected-transaction-summary__line">
              银行对方：{{ getBankCounterpartyText(selectedTransaction) }}
            </div>
            <div v-if="getRemarkText(selectedTransaction)" class="selected-transaction-summary__line">
              备注/摘要：{{ getRemarkText(selectedTransaction) }}
            </div>
          </div>
        </el-form-item>
        <el-form-item>
          <el-button size="small" @click="emit('create-new-transaction')">+ 快捷创建新交易</el-button>
        </el-form-item>
        <el-alert
          v-if="showProjectBindingHint"
          :title="projectBindingHintMessage"
          type="warning"
          :closable="false"
          show-icon
          style="margin-bottom: 18px"
        />
        <el-alert
          v-if="counterpartyMismatchWarnings.length > 0"
          type="error"
          :closable="false"
          show-icon
          style="margin-bottom: 18px"
        >
          <template #title>
            <div>交易与应收款信息不一致，提交时将被拒绝：</div>
            <div v-for="(w, i) in counterpartyMismatchWarnings" :key="i" style="margin-top: 4px">· {{ w }}</div>
          </template>
        </el-alert>
        <el-alert
          v-if="personMismatchWarning"
          type="warning"
          :title="personMismatchWarning"
          :closable="false"
          show-icon
          style="margin-bottom: 18px"
        />
        <el-alert
          v-if="projectMismatchWarning"
          type="warning"
          :title="projectMismatchWarning"
          :closable="false"
          show-icon
          style="margin-bottom: 18px"
        />
        <el-form-item label="收款日期" prop="paymentDate">
          <el-date-picker
            v-model="paymentForm.paymentDate"
            type="date"
            placeholder="选择日期"
            format="YYYY-MM-DD"
            value-format="YYYY-MM-DD"
            style="width: 100%"
          />
        </el-form-item>
        <el-form-item label="收款金额" prop="amount">
          <el-input-number
            v-model="paymentForm.amount"
            :min="0.01"
            :max="receivable.remainingAmount"
            :precision="2"
            :disabled="!!paymentForm.transactionId"
            style="width: 100%"
          />
          <span style="margin-left: 10px; color: var(--text-placeholder)">
            <template v-if="paymentForm.transactionId">（自动计算）</template>
            <template v-else>剩余未收: {{ formatCurrency(receivable.remainingAmount) }}</template>
          </span>
        </el-form-item>
        <el-form-item label="支付方式" prop="paymentMethod">
          <el-select v-model="paymentForm.paymentMethod" placeholder="请选择" style="width: 100%">
            <el-option label="银行转账" value="bank_transfer" />
            <el-option label="现金" value="cash" />
            <el-option label="支票" value="check" />
            <el-option label="其他" value="other" />
          </el-select>
        </el-form-item>
        <el-form-item label="备注" prop="description">
          <el-input
            v-model="paymentForm.description"
            type="textarea"
            :rows="3"
            placeholder="请输入备注"
          />
        </el-form-item>
        <el-form-item>
          <el-button type="primary" :loading="submitting" @click="emit('submit-payment')">
            提交收款
          </el-button>
          <el-button @click="emit('reset-form')">重置</el-button>
        </el-form-item>
      </el-form>
    </div>
  </div>

  <TransactionDetail
    v-model:visible="txDetailVisible"
    :transaction-id="currentTransactionId"
  />
</template>

<script setup lang="ts">
import { computed, ref, watch } from 'vue'
import { ElMessage, ElMessageBox, type FormInstance, type FormRules } from 'element-plus'
import type { Receivable, ReceivePaymentRequest } from '@/features/finance/types/receivable'
import type { Transaction } from '@/features/transactions/types/transaction'
import { TABLE_COLUMN_WIDTH } from '@/shared/constants/table'
import { useResizableTableColumns } from '@/shared/composables/useResizableTableColumns'
import { formatRMB, formatDateTime } from '@/shared/utils/formatters'
import { useUserStore } from '@/features/auth/stores/user'
import { isDateBeforeToday, toDateOnlyString } from '@/shared/utils/date'
import { getAvailableTransactionsForReceivable } from '@/features/transactions/api/transaction'
import TagDisplay from '@/components/tags/TagDisplay.vue'
import TransactionDetail from '@/features/transactions/pages/TransactionDetailPage.vue'

interface Props {
  receivable: Receivable | null
  loading: boolean
  submitting: boolean
  paymentForm: ReceivePaymentRequest
  paymentRules: FormRules
  paymentFormRef?: (instance: FormInstance | undefined) => void
}

const props = defineProps<Props>()

const emit = defineEmits(['submit-payment', 'reset-form', 'go-to-entity', 'create-new-transaction'])
const userStore = useUserStore()

const { getColumnWidth, getColumnMinWidth, handleHeaderDragend } = useResizableTableColumns('receivable-detail-payments')

const txDetailVisible = ref(false)
const currentTransactionId = ref(0)

const handleViewTransaction = (row: { transactionId: number }) => {
  if (row.transactionId > 0) {
    currentTransactionId.value = row.transactionId
    txDetailVisible.value = true
  }
}

const availableTransactions = ref<Transaction[]>([])
const showAllTransactions = ref(false)
const transactionKeyword = ref('')
const loadingTransactions = ref(false)
const projectBindingHintMessage = '当前交易未关联项目，保存登记后会补齐为当前项目。'

const selectedTransaction = computed(() =>
  availableTransactions.value.find(t => t.id === props.paymentForm.transactionId)
)

function setPaymentFormInstance(instance: FormInstance | undefined) {
  props.paymentFormRef?.(instance)
}

const showProjectBindingHint = computed(() =>
  !!props.receivable?.projectId &&
  !!selectedTransaction.value &&
  !selectedTransaction.value.projectId
)

const counterpartyMismatchWarnings = computed(() => {
  const tx = selectedTransaction.value
  const receivable = props.receivable
  if (!tx || !receivable) return []

  const warnings: string[] = []
  if (receivable.supplierId && tx.supplierId && tx.supplierId !== receivable.supplierId) {
    warnings.push(`供应商不一致：应收款为"${receivable.supplierName || receivable.supplierId}"，交易为"${tx.supplierName || tx.supplierId}"`)
  }
  if (receivable.customerId && tx.customerId && tx.customerId !== receivable.customerId) {
    warnings.push(`客户不一致：应收款为"${receivable.customerName || receivable.customerId}"，交易为"${tx.customerName || tx.customerId}"`)
  }
  return warnings
})

const projectMismatchWarning = computed(() => {
  const tx = selectedTransaction.value
  const receivable = props.receivable
  if (!tx || !receivable) return ''
  if (receivable.projectId && tx.projectId && tx.projectId !== receivable.projectId) {
    return `项目不一致：应收款为"${receivable.projectName || receivable.projectId}"，交易为"${tx.projectName || tx.projectId}"（不影响提交，允许跨项目分配）`
  }
  return ''
})

const personMismatchWarning = computed(() => {
  const tx = selectedTransaction.value
  const receivable = props.receivable
  if (!tx || !receivable) return ''
  if (receivable.personId && tx.personId && tx.personId !== receivable.personId) {
    return `人员不一致：应收款为"${receivable.personName || receivable.personId}"，交易为"${tx.personName || tx.personId}"（不影响提交）`
  }
  return ''
})

async function loadAvailableTransactions() {
  if (!props.receivable) return

  loadingTransactions.value = true
  try {
    const params: { projectId?: number; customerId?: number; supplierId?: number; personId?: number; showAll?: boolean; keyword?: string } = {}
    if (!showAllTransactions.value) {
      if (props.receivable.projectId) {
        params.projectId = props.receivable.projectId
      }
      if (props.receivable.customerId) {
        params.customerId = props.receivable.customerId
      } else if (props.receivable.supplierId) {
        params.supplierId = props.receivable.supplierId
      } else if (props.receivable.personId) {
        params.personId = props.receivable.personId
      }
    } else {
      params.showAll = true
    }
    if (transactionKeyword.value.trim()) {
      params.keyword = transactionKeyword.value.trim()
    }

    availableTransactions.value = await getAvailableTransactionsForReceivable(params)
  } catch {
    ElMessage.error('加载可用交易失败')
    availableTransactions.value = []
  } finally {
    loadingTransactions.value = false
  }
}

function syncSelectedTransactionAmount() {
  if (!props.paymentForm.transactionId || !props.receivable || !selectedTransaction.value) return

  const availableAmount = selectedTransaction.value.availableAmount || 0
  const remainingAmount = props.receivable.remainingAmount
  props.paymentForm.amount = Math.min(availableAmount, remainingAmount)
}

function syncSelectedTransactionDate() {
  if (!props.paymentForm.transactionId || !selectedTransaction.value) return

  const transactionDate = toDateOnlyString(selectedTransaction.value.transactionDate)
  if (transactionDate) {
    props.paymentForm.paymentDate = transactionDate
  }
}

function syncSelectedTransactionFields() {
  syncSelectedTransactionAmount()
  syncSelectedTransactionDate()
}

async function onTransactionSelected() {
  syncSelectedTransactionFields()

  const errors = counterpartyMismatchWarnings.value
  const personHint = personMismatchWarning.value
  const allWarnings = [...errors, ...(personHint ? [personHint] : [])]
  if (allWarnings.length > 0) {
    try {
      await ElMessageBox.confirm(
        allWarnings.join('\n') + '\n\n确定要选择此交易吗？',
        '交易信息不一致',
        { type: 'warning', confirmButtonText: '继续选择', cancelButtonText: '取消选择' }
      )
    } catch {
      props.paymentForm.transactionId = 0
    }
  }
}

function getPrimaryCounterpartyText(transaction: Transaction) {
  return transaction.customerName || transaction.supplierName || transaction.personName || transaction.counterparty || '-'
}

function getBankCounterpartyText(transaction: Transaction) {
  if (!transaction.counterparty) return ''

  const primaryCounterparty = transaction.customerName || transaction.supplierName || transaction.personName
  return transaction.counterparty === primaryCounterparty ? '' : transaction.counterparty
}

function getRemarkText(transaction: Transaction) {
  const texts = [transaction.description?.trim(), transaction.memo?.trim()]
    .filter((text): text is string => !!text)
    .filter((text, index, list) => list.indexOf(text) === index)

  return texts.join(' / ')
}

function getTransactionOptionLabel(transaction: Transaction) {
  const segments = [
    formatDate(transaction.transactionDate),
    formatCurrency(transaction.amount),
    `付款方: ${getPrimaryCounterpartyText(transaction)}`
  ]

  const remarks = getRemarkText(transaction)
  if (remarks) {
    segments.push(`备注: ${remarks}`)
  }

  return segments.join(' | ')
}

watch(() => props.receivable, async (newReceivable) => {
  if (newReceivable && newReceivable.status !== 'settled') {
    await loadAvailableTransactions()
  } else {
    availableTransactions.value = []
  }
}, { immediate: true })

watch([() => props.paymentForm.transactionId, availableTransactions], () => {
  syncSelectedTransactionFields()
}, { deep: true })

const formatDate = (date: string) => formatDateTime(date, 'date')

const formatCurrency = (amount: number) => formatRMB(amount)

const getStatusType = (status: string) => {
  const statusMap: Record<string, string> = {
    pending: 'warning',
    partial: 'info',
    settled: 'success'
  }
  return statusMap[status] || ''
}

const getStatusText = (status: string) => {
  const statusMap: Record<string, string> = {
    pending: '待收款',
    partial: '部分收款',
    settled: '已结清'
  }
  return statusMap[status] || status
}

const isOverdue = (row: Receivable) => {
  if (!row.dueDate || row.status === 'settled') return false
  return isDateBeforeToday(row.dueDate) && row.remainingAmount > 0
}
</script>

<style>
/* 下拉弹出层挂载在 body 上，必须用非 scoped 样式 + popper-class 限定作用域 */
.finance-transaction-select-dropdown .el-select-dropdown__item {
  height: auto !important;
  padding-top: 8px;
  padding-bottom: 8px;
  overflow: visible;
  line-height: 1.5;
  white-space: normal;
  text-overflow: clip;
}
</style>

<style scoped>
.payment-details {
  margin-top: 20px;
}

.payment-form {
  margin-top: 20px;
}

.transaction-option {
  display: flex;
  flex-direction: column;
  gap: 4px;
  padding: 4px 0;
  line-height: 1.5;
  white-space: normal;
}

.transaction-option__header,
.selected-transaction-summary__header {
  display: flex;
  flex-wrap: wrap;
  justify-content: space-between;
  gap: 8px 12px;
}

.transaction-option__available {
  color: var(--color-success);
  font-size: 12px;
}

.transaction-option__meta,
.selected-transaction-summary__meta {
  display: flex;
  flex-wrap: wrap;
  gap: 4px 16px;
  color: var(--text-secondary, #606266);
  font-size: 12px;
}

.transaction-option__extra,
.selected-transaction-summary__line {
  color: var(--text-secondary, #606266);
  font-size: 12px;
  line-height: 1.5;
}

.selected-transaction-summary {
  margin-top: 12px;
  padding: 12px 14px;
  border: 1px solid var(--border-base, #dcdfe6);
  border-radius: 8px;
  background: var(--el-fill-color-lighter, #fafafa);
}

.selected-transaction-summary__title {
  margin-bottom: 8px;
  color: var(--text-regular, #303133);
  font-size: 13px;
  font-weight: 600;
}

.overdue {
  color: var(--color-danger);
  font-weight: bold;
}

.clickable-rows :deep(tr) {
  cursor: pointer;
}
</style>
