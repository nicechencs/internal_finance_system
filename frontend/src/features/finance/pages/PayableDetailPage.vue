<template>
  <el-dialog
    v-if="isDialogMode"
    :model-value="visible"
    title="应付详情"
    width="900px"
    @close="handleClose"
  >
    <PayableDetailContent
      :payable="payable"
      :loading="loading"
      :submitting="submitting"
      :payment-form="paymentForm"
      :payment-rules="paymentRules"
      :payment-form-ref="setPaymentFormRef"
      @submit-payment="handleSubmitPayment"
      @reset-form="handleResetForm"
      @go-to-entity="goToEntityDetail"
      @create-transaction="handleCreateTransaction"
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
        <h2 class="page-title">应付详情</h2>
      </div>
    </div>

    <div class="page-content">
      <el-card shadow="never">
        <PayableDetailContent
          :payable="payable"
          :loading="loading"
          :submitting="submitting"
          :payment-form="paymentForm"
          :payment-rules="paymentRules"
          :payment-form-ref="setPaymentFormRef"
          @submit-payment="handleSubmitPayment"
          @reset-form="handleResetForm"
          @go-to-entity="goToEntityDetail"
          @create-transaction="handleCreateTransaction"
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
import { getPayableById, payPayment } from '@/features/finance/api/payable'
import type { Payable, PayPaymentRequest } from '@/features/finance/types/payable'
import PayableDetailContent from '@/features/finance/components/PayableDetailContent.vue'
import TransactionForm from '@/features/transactions/components/TransactionForm.vue'
import type { CreateTransactionRequest, Transaction } from '@/features/transactions/types/transaction'
import { useUserStore } from '@/features/auth/stores/user'
import { getTodayDateString, toDateOnlyString } from '@/shared/utils/date'

interface Props {
  visible?: boolean
  payableId?: number
}

const props = defineProps<Props>()
const emit = defineEmits(['update:visible', 'success'])
const router = useRouter()
const route = useRoute()
const userStore = useUserStore()

const isDialogMode = computed(() => props.visible !== undefined)

const effectiveId = computed(() => {
  if (props.payableId) return props.payableId
  const routeId = route.params.id
  return routeId ? Number(routeId) : null
})

const loading = ref(false)
const submitting = ref(false)
const payable = ref<Payable | null>(null)
const paymentFormRef = ref<FormInstance>()
const transactionFormVisible = ref(false)
const transactionDraft = ref<Partial<CreateTransactionRequest> | null>(null)

const paymentForm = reactive<PayPaymentRequest>({
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
  paymentDate: [{ required: true, message: '请选择付款日期', trigger: 'change' }],
  amount: [
    { required: true, message: '请输入付款金额', trigger: 'blur' },
    { type: 'number', min: 0.01, message: '金额必须大于 0', trigger: 'blur' }
  ]
}

watch(
  () => props.visible,
  async (visible) => {
    if (visible && effectiveId.value) {
      await loadPayable()
    }
  }
)

onMounted(() => {
  if (!isDialogMode.value && effectiveId.value) {
    void loadPayable()
  }
})

const loadPayable = async () => {
  if (!effectiveId.value) {
    ElMessage.error('缺少应付 ID')
    return
  }

  loading.value = true
  try {
    const { data } = await getPayableById(effectiveId.value)
    payable.value = data.data
    if (payable.value && payable.value.remainingAmount > 0) {
      paymentForm.amount = payable.value.remainingAmount
    }
  } catch {
    ElMessage.error('加载应付详情失败')
  } finally {
    loading.value = false
  }
}

const handleSubmitPayment = async () => {
  if (!userStore.canEdit) {
    ElMessage.error('无权限执行付款登记')
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

    await payPayment(id, paymentForm)
    ElMessage.success('付款登记成功')
    emit('success')
    await loadPayable()
    handleResetForm()
  } catch {
    ElMessage.error('付款登记失败')
  } finally {
    submitting.value = false
  }
}

const handleResetForm = () => {
  paymentFormRef.value?.resetFields()
  paymentForm.paymentDate = getTodayDateString()
  paymentForm.amount = payable.value?.remainingAmount || 0
  paymentForm.paymentMethod = undefined
  paymentForm.description = undefined
  paymentForm.transactionId = 0
}

const buildTransactionDraft = (): Partial<CreateTransactionRequest> | null => {
  if (!payable.value) {
    return null
  }

  return {
    transactionDate: paymentForm.paymentDate,
    transactionType: 'Expense',
    amount: payable.value.remainingAmount,
    projectId: payable.value.projectId,
    supplierId: payable.value.supplierId,
    customerId: payable.value.customerId,
    personId: payable.value.personId
  }
}

const handleCreateTransaction = () => {
  transactionDraft.value = buildTransactionDraft()
  transactionFormVisible.value = true
}

const handleTransactionFormSuccess = async (transaction: Transaction) => {
  transactionFormVisible.value = false
  transactionDraft.value = null

  await loadPayable()

  paymentForm.transactionId = transaction.id
  paymentForm.paymentDate = toDateOnlyString(transaction.transactionDate) || paymentForm.paymentDate
  const availableAmount = transaction.availableAmount ?? transaction.amount
  const remainingAmount = payable.value?.remainingAmount ?? availableAmount
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
