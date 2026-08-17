<template>
  <el-dialog
    v-model="visible"
    title="账户转账"
    width="600px"
    :close-on-click-modal="false"
    @close="handleClose"
  >
    <div class="transfer-note">
      <strong>仅用于活期与定期之间的资金流转、定期支取或内部资金划转。</strong>
      仅移动账户间的结余（包含定期账户），不会产生成本/收入/支出，本控件不用于经营流水录入。
    </div>
    <el-form
      ref="formRef"
      :model="form"
      :rules="rules"
      label-width="100px"
    >
      <el-form-item label="转出账户" prop="fromAccountId">
        <el-select
          v-model="form.fromAccountId"
          placeholder="选择转出账户"
          filterable
          :loading="accountsLoading"
          style="width: 100%"
          @change="handleFromAccountChange"
        >
          <el-option
            v-for="account in selectableAccounts"
            :key="account.id"
            :label="getFromAccountLabel(account)"
            :value="account.id"
          />
        </el-select>
      </el-form-item>

      <el-form-item label="转入账户" prop="toAccountId">
        <el-select
          v-model="form.toAccountId"
          placeholder="选择转入账户"
          filterable
          :loading="accountsLoading"
          style="width: 100%"
        >
          <el-option
            v-for="account in availableToAccounts"
            :key="account.id"
            :label="formatTransferAccountLabel(account)"
            :value="account.id"
          />
        </el-select>
      </el-form-item>

      <el-form-item label="转账金额" prop="amount">
        <el-input-number
          v-model="form.amount"
          :min="0.01"
          :max="effectiveMaxAmount"
          :precision="2"
          :controls="false"
          style="width: 100%"
          placeholder="请输入转账金额"
        />
        <div v-if="maxAmount > 0" style="color: var(--text-placeholder); font-size: 12px; margin-top: 4px">
          可用余额: ¥{{ maxAmount.toLocaleString() }}
        </div>
      </el-form-item>

      <el-form-item label="转账日期" prop="transactionDate">
        <el-date-picker
          v-model="form.transactionDate"
          type="date"
          placeholder="选择转账日期"
          style="width: 100%"
          value-format="YYYY-MM-DD"
        />
      </el-form-item>

      <el-form-item label="备注" prop="description">
        <el-input
          v-model="form.description"
          type="textarea"
          :rows="3"
          placeholder="请输入备注信息（可选）"
          maxlength="500"
          show-word-limit
        />
      </el-form-item>

      <template v-if="isTargetFixedDeposit">
        <el-divider content-position="left">定期存款参数</el-divider>
        <el-alert
          type="info"
          :closable="false"
          show-icon
          style="margin-bottom: 16px"
        >
          转入定期账户时，系统将自动创建定期存款台账记录。
        </el-alert>
        <el-form-item label="存款期限" prop="termMonths">
          <el-input-number
            v-model="form.termMonths"
            :min="1"
            :max="120"
            :precision="0"
            :controls="true"
            style="width: 100%"
            placeholder="请输入存款期限（月）"
          />
        </el-form-item>
        <el-form-item label="年利率(%)" prop="interestRate">
          <el-input-number
            v-model="form.interestRate"
            :min="0"
            :max="100"
            :precision="2"
            :step="0.1"
            :controls="true"
            style="width: 100%"
            placeholder="请输入年利率"
          />
        </el-form-item>
      </template>
    </el-form>

    <template #footer>
      <el-button @click="handleClose">取消</el-button>
      <el-button type="primary" :loading="loading" @click="handleSubmit">
        确认转账
      </el-button>
    </template>
  </el-dialog>
</template>

<script setup lang="ts">
import { ref, computed, watch } from 'vue'
import { ElMessage, type FormInstance, type FormRules } from 'element-plus'
import { createTransfer } from '@/features/transactions/api/transaction'
import { getActiveAccounts } from '@/features/master-data/accounts/api/account'
import type { Account } from '@/features/master-data/accounts/types/account'
import { formatTransferAccountLabel, mergeTransferAccounts } from '@/features/transactions/utils/transferAccounts'
import { getTodayDateString } from '@/shared/utils/date'

interface Props {
  modelValue: boolean
  accounts: Account[]
}

interface Emits {
  (e: 'update:modelValue', value: boolean): void
  (e: 'success'): void
}

const props = defineProps<Props>()
const emit = defineEmits<Emits>()

const formRef = ref<FormInstance>()
const loading = ref(false)
const accountsLoading = ref(false)
const activeAccounts = ref<Account[]>([])

const visible = computed({
  get: () => props.modelValue,
  set: (value) => emit('update:modelValue', value)
})

const createEmptyForm = () => ({
  fromAccountId: undefined as number | undefined,
  toAccountId: undefined as number | undefined,
  amount: undefined as number | undefined,
  transactionDate: getTodayDateString(),
  description: '',
  termMonths: undefined as number | undefined,
  interestRate: undefined as number | undefined
})

const form = ref(createEmptyForm())

const maxAmount = ref(0)

const getAccountBalance = (account: Account) => account.currentBalance ?? account.balance ?? account.openingBalance ?? 0

const selectableAccounts = computed(() => {
  return mergeTransferAccounts(activeAccounts.value, props.accounts)
})

const availableToAccounts = computed(() => {
  return selectableAccounts.value.filter(account => account.id !== form.value.fromAccountId)
})

const isTargetFixedDeposit = computed(() => {
  if (!form.value.toAccountId) return false
  const account = selectableAccounts.value.find(a => a.id === form.value.toAccountId)
  return account?.accountType === 'FixedDeposit'
})

const effectiveMaxAmount = computed(() => {
  return maxAmount.value > 0 ? maxAmount.value : undefined
})

const getFromAccountLabel = (account: Account) => {
  return `${formatTransferAccountLabel(account)} (余额: ¥${getAccountBalance(account).toLocaleString()})`
}

const loadSelectableAccounts = async () => {
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

const rules: FormRules = {
  fromAccountId: [
    { required: true, message: '请选择转出账户', trigger: 'change' }
  ],
  toAccountId: [
    { required: true, message: '请选择转入账户', trigger: 'change' },
    {
      validator: (rule, value, callback) => {
        if (value === form.value.fromAccountId) {
          callback(new Error('转出和转入账户不能相同'))
        } else {
          callback()
        }
      },
      trigger: 'change'
    }
  ],
  amount: [
    { required: true, message: '请输入转账金额', trigger: 'blur' },
    {
      validator: (rule, value, callback) => {
        if (!value || value <= 0) {
          callback(new Error('转账金额必须大于0'))
        } else if (maxAmount.value > 0 && value > maxAmount.value) {
          callback(new Error(`转账金额不能超过可用余额 ¥${maxAmount.value.toLocaleString()}`))
        } else {
          callback()
        }
      },
      trigger: 'blur'
    }
  ],
  transactionDate: [
    { required: true, message: '请选择转账日期', trigger: 'change' }
  ],
  termMonths: [
    {
      validator: (rule: any, value: any, callback: any) => {
        if (isTargetFixedDeposit.value && (!value || value <= 0)) {
          callback(new Error('转入定期账户时必须填写存款期限'))
        } else {
          callback()
        }
      },
      trigger: 'blur'
    }
  ],
  interestRate: [
    {
      validator: (rule: any, value: any, callback: any) => {
        if (isTargetFixedDeposit.value && (value == null || value < 0 || value > 100)) {
          callback(new Error('转入定期账户时必须填写有效利率（0-100%）'))
        } else {
          callback()
        }
      },
      trigger: 'blur'
    }
  ]
}

const handleFromAccountChange = () => {
  const account = selectableAccounts.value.find(a => a.id === form.value.fromAccountId)
  maxAmount.value = account ? getAccountBalance(account) : 0

  // 如果转入账户与转出账户相同，清空转入账户
  if (form.value.toAccountId === form.value.fromAccountId) {
    form.value.toAccountId = undefined
  }

  // 如果金额超过新的最大值，重置金额
  if (form.value.amount && maxAmount.value > 0 && form.value.amount > maxAmount.value) {
    form.value.amount = undefined
  }
}

const handleSubmit = async () => {
  if (!formRef.value) return

  try {
    await formRef.value.validate()
    loading.value = true

    const response = await createTransfer({
      fromAccountId: form.value.fromAccountId!,
      toAccountId: form.value.toAccountId!,
      amount: form.value.amount!,
      transactionDate: form.value.transactionDate!,
      description: form.value.description || undefined,
      termMonths: isTargetFixedDeposit.value ? form.value.termMonths : undefined,
      interestRate: isTargetFixedDeposit.value ? form.value.interestRate : undefined
    })

    const linkage = response.data?.data?.fixedDepositLinkage
    if (linkage?.message) {
      ElMessage.success(linkage.message)
    } else {
      ElMessage.success('转账成功')
    }
    emit('success')
    handleClose()
  } catch (error: any) {
    if (error !== false) {
      console.error('转账失败:', error)
    }
  } finally {
    loading.value = false
  }
}

const handleClose = () => {
  formRef.value?.resetFields()
  Object.assign(form.value, createEmptyForm())
  maxAmount.value = 0
  visible.value = false
}

// 监听对话框打开，重置表单
watch(visible, (newVal) => {
  if (newVal) {
    form.value.transactionDate = getTodayDateString()
    loadSelectableAccounts()
  }
})
</script>

<style scoped>
.transfer-note {
  margin: 0 24px 16px;
  padding: 12px 14px;
  border-radius: 10px;
  background: var(--bg-page);
  border: 1px dashed var(--border-light);
  color: var(--text-secondary);
  font-size: 13px;
}

.transfer-note strong {
  display: block;
  color: var(--text-primary);
  margin-bottom: 4px;
}
</style>

