<template>
  <el-dialog
    :model-value="modelValue"
    :title="isEditMode ? '编辑定期存款' : '新增定期存款'"
    width="640px"
    @close="handleClose"
  >
    <div class="dialog-intro">
      <div class="intro-title">{{ isEditMode ? '修改定期存款信息' : '登记新的定期存款' }}</div>
      <div class="intro-desc">{{ isEditMode ? '修改后将重新计算到期日和预期收益。' : '创建后将纳入到期提醒、收益统计与支取流程管理。' }}</div>
    </div>

    <el-form
      ref="formRef"
      :model="form"
      :rules="rules"
      label-width="100px"
      class="dialog-form"
    >
      <el-form-item label="定期账户" prop="accountId">
        <SearchableSelect
          v-model="form.accountId"
          :options="accounts"
          entity-name="定期账户"
          placeholder="请选择定期账户"
          :clearable="false"
        />
      </el-form-item>

      <el-form-item label="本金" prop="principal">
        <el-input-number
          v-model="form.principal"
          :precision="2"
          :step="1000"
          :min="0.01"
          :controls="false"
          style="width: 100%"
          placeholder="请输入本金"
        />
      </el-form-item>

      <el-row :gutter="16">
        <el-col :span="12">
          <el-form-item label="起息日" prop="depositDate">
            <el-date-picker
              v-model="form.depositDate"
              type="date"
              placeholder="请选择起息日"
              format="YYYY-MM-DD"
              value-format="YYYY-MM-DD"
              style="width: 100%"
            />
          </el-form-item>
        </el-col>
        <el-col :span="12">
          <el-form-item label="期限(月)" prop="termMonths">
            <el-select v-model="form.termMonths" placeholder="请选择期限" style="width: 100%">
              <el-option v-for="item in termOptions" :key="item" :label="`${item} 个月`" :value="item" />
            </el-select>
          </el-form-item>
        </el-col>
      </el-row>

      <el-row :gutter="16">
        <el-col :span="12">
          <el-form-item label="年利率" prop="interestRate">
            <el-input-number
              v-model="form.interestRate"
              :precision="2"
              :step="0.05"
              :min="0"
              :max="100"
              :controls="false"
              style="width: 100%"
              placeholder="请输入年利率"
            />
            <div class="field-tip">请输入百分比数值，例如 2.35 表示 2.35%</div>
          </el-form-item>
        </el-col>
        <el-col :span="12">
          <el-form-item label="到期预览">
            <div class="preview-box">
              <span class="preview-value">{{ previewMaturityDate }}</span>
              <span class="preview-sub">{{ previewInterestText }}</span>
            </div>
          </el-form-item>
        </el-col>
      </el-row>

      <el-form-item label="备注">
        <el-input
          v-model="form.notes"
          type="textarea"
          :rows="3"
          maxlength="200"
          show-word-limit
          placeholder="可填写产品说明、存单号、办理网点等"
        />
      </el-form-item>
    </el-form>

    <template #footer>
      <el-button @click="handleClose">取消</el-button>
      <el-button type="primary" :loading="submitting" @click="handleSubmit">
        {{ isEditMode ? '确认更新' : '确认创建' }}
      </el-button>
    </template>
  </el-dialog>
</template>

<script setup lang="ts">
import { computed, reactive, ref, watch } from 'vue'
import { ElMessage } from 'element-plus'
import type { FormInstance, FormRules } from 'element-plus'
import dayjs from 'dayjs'
import SearchableSelect from '@/shared/ui/SearchableSelect.vue'
import type { Account } from '@/features/master-data/accounts/types/account'
import { createFixedDeposit, updateFixedDeposit } from '@/features/master-data/fixed-deposits/api/fixedDeposit'
import type { CreateFixedDepositRequest, FixedDeposit, UpdateFixedDepositRequest } from '@/features/master-data/fixed-deposits/types/fixedDeposit'
import { formatMoney } from '@/shared/utils/formatters'

interface Props {
  modelValue: boolean
  accounts: Account[]
  deposit?: FixedDeposit | null
  defaultAccountId?: number | null
}

const termOptions = [1, 3, 6, 12, 24, 36]

const props = defineProps<Props>()
const emit = defineEmits<{
  'update:modelValue': [value: boolean]
  success: []
}>()

const formRef = ref<FormInstance>()
const submitting = ref(false)

const isEditMode = computed(() => !!props.deposit)

const form = reactive<CreateFixedDepositRequest>({
  accountId: 0,
  principal: 0,
  termMonths: 3,
  interestRate: 0,
  depositDate: '',
  notes: ''
})

const rules: FormRules<CreateFixedDepositRequest> = {
  accountId: [{ required: true, message: '请选择定期账户', trigger: 'change' }],
  principal: [
    { required: true, message: '请输入本金', trigger: 'blur' },
    { type: 'number', min: 0.01, message: '本金必须大于 0', trigger: 'blur' }
  ],
  depositDate: [{ required: true, message: '请选择起息日', trigger: 'change' }],
  termMonths: [{ required: true, message: '请选择期限', trigger: 'change' }],
  interestRate: [
    { required: true, message: '请输入年利率', trigger: 'blur' },
    { type: 'number', min: 0, message: '利率不能小于 0', trigger: 'blur' }
  ]
}

const previewMaturityDate = computed(() => {
  if (!form.depositDate || !form.termMonths) return '待计算'
  return dayjs(form.depositDate).add(form.termMonths, 'month').format('YYYY-MM-DD')
})

const previewInterest = computed(() => {
  if (!form.principal || !form.interestRate || !form.termMonths) return 0
  return Number((form.principal * (form.interestRate / 100) * (form.termMonths / 12)).toFixed(2))
})

const previewInterestText = computed(() => {
  if (!previewInterest.value) return '预计收益待计算'
  return `预计收益 ${formatMoney(previewInterest.value)}`
})

const resetForm = () => {
  if (isEditMode.value && props.deposit) {
    // 编辑模式：填充现有数据
    Object.assign(form, {
      accountId: props.deposit.accountId,
      principal: props.deposit.principal,
      termMonths: props.deposit.termMonths,
      interestRate: props.deposit.interestRate,
      depositDate: dayjs(props.deposit.depositDate).format('YYYY-MM-DD'),
      notes: props.deposit.notes || ''
    })
  } else {
    // 创建模式：重置为默认值
    Object.assign(form, {
      accountId: props.accounts.find(item => item.id === props.defaultAccountId)?.id ?? props.accounts[0]?.id ?? 0,
      principal: 0,
      termMonths: 3,
      interestRate: 0,
      depositDate: dayjs().format('YYYY-MM-DD'),
      notes: ''
    })
  }
}

watch(() => props.modelValue, (visible) => {
  if (!visible) return
  resetForm()
  formRef.value?.clearValidate()
})

const handleClose = () => {
  emit('update:modelValue', false)
}

const handleSubmit = async () => {
  if (!formRef.value) return

  const valid = await formRef.value.validate().catch(() => false)
  if (!valid) return

  submitting.value = true
  try {
    if (isEditMode.value && props.deposit) {
      // 编辑模式
      const updateData: UpdateFixedDepositRequest = {
        accountId: form.accountId,
        principal: form.principal,
        depositDate: form.depositDate!,
        termMonths: form.termMonths,
        interestRate: form.interestRate,
        notes: form.notes?.trim() || undefined
      }
      await updateFixedDeposit(props.deposit.id, updateData)
      ElMessage.success('定期存款更新成功')
    } else {
      // 创建模式
      await createFixedDeposit({
        accountId: form.accountId,
        principal: form.principal,
        termMonths: form.termMonths,
        interestRate: form.interestRate,
        depositDate: form.depositDate || undefined,
        notes: form.notes?.trim() || undefined
      })
      ElMessage.success('定期存款创建成功')
    }
    emit('success')
    handleClose()
  } catch (error) {
    console.error(isEditMode.value ? '更新定期存款失败:' : '创建定期存款失败:', error)
  } finally {
    submitting.value = false
  }
}
</script>

<style scoped>
.dialog-intro {
  margin-bottom: 16px;
  padding: 14px 16px;
  border: 1px solid var(--border-light);
  border-radius: 10px;
  background: linear-gradient(180deg, rgba(64, 158, 255, 0.06), rgba(64, 158, 255, 0.02));
}

.intro-title {
  font-size: 14px;
  font-weight: 600;
  color: var(--text-primary);
}

.intro-desc {
  margin-top: 4px;
  font-size: 12px;
  line-height: 1.6;
  color: var(--text-secondary);
}

.dialog-form :deep(.el-form-item) {
  margin-bottom: 18px;
}

.field-tip {
  margin-top: 6px;
  font-size: 12px;
  color: var(--text-placeholder);
  line-height: 1.4;
}

.preview-box {
  width: 100%;
  min-height: 72px;
  padding: 12px 14px;
  border-radius: 10px;
  border: 1px dashed var(--border-base);
  background: var(--bg-page);
  display: flex;
  flex-direction: column;
  justify-content: center;
}

.preview-value {
  font-size: 18px;
  font-weight: 700;
  color: var(--text-primary);
  line-height: 1.2;
}

.preview-sub {
  margin-top: 6px;
  font-size: 12px;
  color: var(--text-secondary);
}
</style>
