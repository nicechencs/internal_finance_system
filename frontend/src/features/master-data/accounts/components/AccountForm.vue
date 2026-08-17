<template>
  <el-dialog
    :model-value="visible"
    :title="isEdit ? '编辑账户' : '新增账户'"
    width="640px"
    @close="handleClose"
  >
    <el-form
      ref="formRef"
      :model="formData"
      :rules="rules"
      label-width="100px"
    >
      <el-form-item label="账户名称" prop="name">
        <SearchableInput
          v-model="formData.name"
          :fetch-options="getActiveAccounts"
          placeholder="请输入或选择账户名称"
        />
      </el-form-item>

      <el-form-item v-if="!isEdit" label="账户类型" prop="accountType">
        <el-select v-model="formData.accountType" placeholder="请选择账户类型" style="width: 100%">
          <el-option label="银行账户" value="Bank" />
          <el-option label="支付宝" value="Alipay" />
          <el-option label="定期账户" value="FixedDeposit" />
        </el-select>
      </el-form-item>

      <el-form-item v-if="!isEdit" label="初始余额" prop="initialBalance">
        <el-input-number
          v-model="formData.initialBalance"
          :precision="2"
          :step="100"
          :min="0"
          :controls="false"
          style="width: 100%"
        />
      </el-form-item>

      <el-form-item v-if="!isEdit" label="币种" prop="currency">
        <el-select v-model="formData.currency" placeholder="请选择币种" style="width: 100%">
          <el-option label="人民币 (CNY)" value="CNY" />
          <el-option label="美元 (USD)" value="USD" />
          <el-option label="欧元 (EUR)" value="EUR" />
        </el-select>
      </el-form-item>

      <el-form-item label="银行名称" prop="bankName">
        <el-input v-model="formData.bankName" placeholder="请输入银行名称" />
      </el-form-item>

      <el-form-item label="账号" prop="accountNumber">
        <el-input v-model="formData.accountNumber" placeholder="请输入账号" />
      </el-form-item>

      <el-form-item v-if="isEdit" label="状态" prop="isActive">
        <el-switch v-model="formData.isActive" />
      </el-form-item>
    </el-form>

    <template #footer>
      <el-button @click="handleClose">取消</el-button>
      <el-button type="primary" :loading="loading" @click="handleSubmit">
        确定
      </el-button>
    </template>
  </el-dialog>
</template>

<script setup lang="ts">
import { computed, reactive, ref, watch } from 'vue'
import { ElMessage } from 'element-plus'
import type { FormInstance, FormRules } from 'element-plus'
import type { Account, CreateAccountRequest, UpdateAccountRequest } from '@/features/master-data/accounts/types/account'
import { createAccount, getActiveAccounts, updateAccount } from '@/features/master-data/accounts/api/account'
import SearchableInput from '@/shared/ui/SearchableInput.vue'

interface Props {
  visible: boolean
  account: Account | null
}

type AccountFormData = CreateAccountRequest & UpdateAccountRequest & {
  isActive: boolean
}

interface CreatedAccountPayload {
  accountId: number
  accountType: string
  name: string
}

const props = defineProps<Props>()
const emit = defineEmits<{
  (event: 'update:visible', value: boolean): void
  (event: 'success', payload?: CreatedAccountPayload): void
}>()

const formRef = ref<FormInstance>()
const loading = ref(false)

const isEdit = computed(() => !!props.account)

const formData = reactive<AccountFormData>({
  name: '',
  accountType: 'Bank',
  initialBalance: 0,
  currency: 'CNY',
  bankName: '',
  accountNumber: '',
  isActive: true
})

const rules: FormRules<AccountFormData> = {
  name: [{ required: true, message: '请输入账户名称', trigger: 'blur' }],
  accountType: [{ required: true, message: '请选择账户类型', trigger: 'change' }],
  initialBalance: [{ required: true, message: '请输入初始余额', trigger: 'blur' }],
  currency: [{ required: true, message: '请选择币种', trigger: 'change' }]
}

const resetFormData = () => {
  Object.assign(formData, {
    name: '',
    accountType: 'Bank',
    initialBalance: 0,
    currency: 'CNY',
    bankName: '',
    accountNumber: '',
    isActive: true
  })
}

const syncFormData = (account: Account | null) => {
  if (!account) {
    resetFormData()
    return
  }

  Object.assign(formData, {
    name: account.name,
    accountType: account.accountType,
    initialBalance: account.openingBalance ?? 0,
    currency: account.currency || 'CNY',
    bankName: account.bankName || '',
    accountNumber: account.accountNumber || '',
    isActive: account.isActive
  })
}

const normalizeText = (value?: string) => value || undefined

watch(() => props.visible, (visible) => {
  if (!visible) return
  syncFormData(props.account)
  formRef.value?.clearValidate()
})

const handleClose = () => {
  emit('update:visible', false)
}

const handleSubmit = async () => {
  if (!formRef.value) return

  const valid = await formRef.value.validate().catch(() => false)
  if (!valid) return

  loading.value = true
  try {
    let createdPayload: CreatedAccountPayload | undefined

    if (isEdit.value && props.account) {
      const updateData: UpdateAccountRequest = {
        name: formData.name,
        isActive: formData.isActive,
        bankName: normalizeText(formData.bankName),
        accountNumber: normalizeText(formData.accountNumber)
      }
      await updateAccount(props.account.id, updateData)
      ElMessage.success('更新成功')
    } else {
      const createData: CreateAccountRequest = {
        name: formData.name,
        accountType: formData.accountType,
        initialBalance: formData.initialBalance,
        currency: formData.currency,
        bankName: normalizeText(formData.bankName),
        accountNumber: normalizeText(formData.accountNumber)
      }
      const response = await createAccount(createData)
      const createdAccount = response.data?.data
      ElMessage.success('创建成功')
      if (createdAccount) {
        createdPayload = {
          accountId: createdAccount.id,
          accountType: createdAccount.accountType,
          name: createdAccount.name
        }
      }
    }

    emit('success', createdPayload)
    handleClose()
  } catch (error) {
    console.error('Account form submit failed:', error)
  } finally {
    loading.value = false
  }
}
</script>

<style scoped>
</style>
