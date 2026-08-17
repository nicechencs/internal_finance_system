<template>
  <el-dialog
    v-if="isDialogMode"
    :model-value="visible"
    title="应收详情"
    width="900px"
    @close="handleClose"
  >
    <ReceivableDetailContent
      :receivable="receivable"
      :loading="loading"
      :submitting="submitting"
      :payment-form="paymentForm"
      :payment-rules="paymentRules"
      :payment-form-ref="setPaymentFormRef"
      @submit-payment="handleSubmitPayment"
      @reset-form="handleResetForm"
      @go-to-entity="goToEntityDetail"
      @create-new-transaction="handleCreateNewTransaction"
    />

    <template #footer>
      <el-button @click="handleClose">关闭</el-button>
    </template>
  </el-dialog>

  <div v-else class="page-container">
    <div class="page-header">
      <div class="page-header-left">
        <el-button text @click="router.back()">
          <el-icon><ArrowLeft /></el-icon>
          返回
        </el-button>
        <h2 class="page-title">应收详情</h2>
      </div>
    </div>

    <div class="page-content">
      <el-card shadow="never">
        <ReceivableDetailContent
          :receivable="receivable"
          :loading="loading"
          :submitting="submitting"
          :payment-form="paymentForm"
          :payment-rules="paymentRules"
          :payment-form-ref="setPaymentFormRef"
          @submit-payment="handleSubmitPayment"
          @reset-form="handleResetForm"
          @go-to-entity="goToEntityDetail"
          @create-new-transaction="handleCreateNewTransaction"
        />
      </el-card>
    </div>
  </div>

  <TransactionForm
    v-model:visible="transactionFormVisible"
    :draft="transactionDraft"
    @success="handleTransactionFormSuccess"
  />
</template>

<script setup lang="ts">
import { computed, onMounted, reactive, ref, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { ArrowLeft } from '@element-plus/icons-vue'
import { ElMessage, type FormInstance, type FormRules } from 'element-plus'
import { getReceivableById, receivePayment } from '@/features/finance/api/receivable'
import type { Receivable, ReceivePaymentRequest } from '@/features/finance/types/receivable'
import ReceivableDetailContent from '@/features/finance/components/ReceivableDetailContent.vue'
import TransactionForm from '@/features/transactions/components/TransactionForm.vue'
import type { CreateTransactionRequest, Transaction } from '@/features/transactions/types/transaction'
import { useUserStore } from '@/features/auth/stores/user'
import { getTodayDateString, toDateOnlyString } from '@/shared/utils/date'

interface Props {
  visible?: boolean
  receivableId?: number
}

const props = defineProps<Props>()
const emit = defineEmits(['update:visible', 'success'])
const router = useRouter()
const route = useRoute()
const userStore = useUserStore()

const isDialogMode = computed(() => props.visible !== undefined)

const effectiveId = computed(() => {
  if (props.receivableId) return props.receivableId
  const routeId = route.params.id
  return routeId ? Number(routeId) : null
})

const loading = ref(false)
const submitting = ref(false)
const receivable = ref<Receivable | null>(null)
const paymentFormRef = ref<FormInstance>()
const transactionFormVisible = ref(false)
const transactionDraft = ref<Partial<CreateTransactionRequest> | null>(null)

const paymentForm = reactive<ReceivePaymentRequest>({
  paymentDate: getTodayDateString(),
  amount: 0,
  paymentMethod: undefined,
  description: undefined,
  transactionId: 0
})

const setPaymentFormRef = (instance: FormInstance | undefined) => {
  paymentFormRef.value = instance
}

const paymentRules: FormRules = {
  transactionId: [
    { required: true, type: 'number', min: 1, message: '请选择交易', trigger: 'change' }
  ],
  paymentDate: [{ required: true, message: '请选择收款日期', trigger: 'change' }],
  amount: [
    { required: true, message: '请输入收款金额', trigger: 'blur' },
    { type: 'number', min: 0.01, message: '金额必须大于 0', trigger: 'blur' }
  ]
}

watch(
  () => props.visible,
  async (visible) => {
    if (visible && effectiveId.value) {
      await loadReceivable()
    }
  }
)

onMounted(() => {
  if (!isDialogMode.value && effectiveId.value) {
    void loadReceivable()
  }
})

const loadReceivable = async () => {
  if (!effectiveId.value) {
    ElMessage.error('缺少应收 ID')
    return
  }

  loading.value = true
  try {
    const { data } = await getReceivableById(effectiveId.value)
    receivable.value = data.data
    if (receivable.value && receivable.value.remainingAmount > 0) {
      paymentForm.amount = receivable.value.remainingAmount
    }
  } catch {
    ElMessage.error('加载应收详情失败')
  } finally {
    loading.value = false
  }
}

const handleSubmitPayment = async () => {
  if (!userStore.canEdit) {
    ElMessage.error('无权限执行收款登记')
    return
  }

  if (submitting.value) {
    return
  }

  const id = effectiveId.value
  const form = paymentFormRef.value
  if (!form || !id) {
    ElMessage.error('表单尚未初始化完成，请稍后重试')
    return
  }

  submitting.value = true
  try {
    const valid = await form.validate().catch(() => false)
    if (!valid) {
      return
    }

    await receivePayment(id, paymentForm)
    ElMessage.success('收款登记成功')
    emit('success')
    await loadReceivable()
    handleResetForm()
  } catch {
    ElMessage.error('收款登记失败')
  } finally {
    submitting.value = false
  }
}

const handleResetForm = () => {
  paymentFormRef.value?.resetFields()
  paymentForm.paymentDate = getTodayDateString()
  paymentForm.amount = receivable.value?.remainingAmount || 0
  paymentForm.paymentMethod = undefined
  paymentForm.description = undefined
  paymentForm.transactionId = 0
}

const buildTransactionDraft = (): Partial<CreateTransactionRequest> | null => {
  if (!receivable.value) {
    return null
  }

  return {
    transactionDate: paymentForm.paymentDate,
    transactionType: 'Income',
    amount: receivable.value.remainingAmount,
    projectId: receivable.value.projectId,
    customerId: receivable.value.customerId,
    supplierId: receivable.value.supplierId,
    personId: receivable.value.personId
  }
}

const handleCreateNewTransaction = () => {
  transactionDraft.value = buildTransactionDraft()
  transactionFormVisible.value = true
}

const handleTransactionFormSuccess = async (transaction: Transaction) => {
  transactionFormVisible.value = false
  transactionDraft.value = null

  await loadReceivable()

  paymentForm.transactionId = transaction.id
  paymentForm.paymentDate = toDateOnlyString(transaction.transactionDate) || paymentForm.paymentDate
  const availableAmount = transaction.availableAmount ?? transaction.amount
  const remainingAmount = receivable.value?.remainingAmount ?? availableAmount
  paymentForm.amount = Math.min(availableAmount, remainingAmount)
}

const handleClose = () => {
  emit('update:visible', false)
}

const goToEntityDetail = (routeName: string, id: number) => {
  if (isDialogMode.value) {
    handleClose()
  }
  void router.push({ name: routeName, params: { id } })
}
</script>

<style scoped>
.page-container {
  padding: 20px;
}

.page-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 20px;
}

.page-header-left {
  display: flex;
  align-items: center;
  gap: 12px;
}

.page-title {
  margin: 0;
  font-size: 20px;
  font-weight: 600;
}

.page-content {
  margin-top: 20px;
}
</style>
