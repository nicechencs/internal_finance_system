<template>
  <el-dialog
    :model-value="visible"
    :title="isEdit ? '编辑规则' : '新增规则'"
    width="600px"
    @close="handleClose"
  >
    <el-form
      ref="formRef"
      :model="formData"
      :rules="rules"
      label-width="100px"
    >
      <el-form-item label="规则名称" prop="name">
        <el-input v-model="formData.name" placeholder="请输入规则名称" />
      </el-form-item>

      <el-form-item label="分类" prop="categoryId">
        <SearchableSelect
          v-model="formData.categoryId"
          :options="categories"
          entity-name="分类"
          :clearable="false"
        />
      </el-form-item>

      <el-form-item label="匹配字段" prop="matchField">
        <el-select
          v-model="formData.matchField"
          placeholder="请选择匹配字段"
          style="width: 100%"
        >
          <el-option label="对方名称" value="CounterpartyName" />
          <el-option label="交易描述" value="Description" />
          <el-option label="摘要" value="Memo" />
          <el-option label="金额" value="Amount" />
        </el-select>
      </el-form-item>

      <el-form-item label="匹配操作符" prop="matchOperator">
        <el-select
          v-model="formData.matchOperator"
          placeholder="请选择匹配操作符"
          style="width: 100%"
        >
          <el-option
            v-for="opt in operatorOptions"
            :key="opt.value"
            :label="opt.label"
            :value="opt.value"
          />
        </el-select>
      </el-form-item>

      <el-form-item :label="isRange ? '下限（含）' : '匹配值'" prop="matchValue">
        <el-input
          v-model="formData.matchValue"
          :placeholder="isRange ? '请输入金额下限' : '请输入匹配值'"
        />
      </el-form-item>

      <el-form-item v-if="isRange" label="上限（含）" prop="matchValueMax">
        <el-input
          v-model="formData.matchValueMax"
          placeholder="留空表示不限上限"
        />
        <div style="color: var(--text-placeholder); font-size: 12px; margin-top: 4px">
          区间使用 [下限, 上限] 闭区间比较；上限留空表示仅约束下限
        </div>
      </el-form-item>

      <el-form-item label="优先级" prop="priority">
        <el-input-number
          v-model="formData.priority"
          :min="0"
          :max="999"
          style="width: 100%"
        />
      </el-form-item>

      <el-form-item label="状态" prop="isActive" v-if="isEdit">
        <el-switch v-model="formData.isActive" />
      </el-form-item>
    </el-form>

    <template #footer>
      <el-button @click="handleClose">取消</el-button>
      <el-button type="primary" @click="handleSubmit" :loading="submitting">
        确定
      </el-button>
    </template>
  </el-dialog>
</template>

<script setup lang="ts">
import { ref, reactive, watch, computed } from 'vue'
import { ElMessage } from 'element-plus'
import type { FormInstance, FormRules } from 'element-plus'
import { createRule, updateRule } from '@/features/reconciliation/api/rule'
import { getActiveCategories } from '@/features/master-data/categories/api/category'
import type {
  Rule,
  RuleMatchField,
  RuleMatchOperator
} from '@/features/reconciliation/types/rule'
import type { Category } from '@/features/master-data/categories/types/category'
import SearchableSelect from '@/shared/ui/SearchableSelect.vue'

const STRING_OPERATOR_OPTIONS: { label: string; value: RuleMatchOperator }[] = [
  { label: '包含', value: 'Contains' },
  { label: '等于', value: 'Equals' },
  { label: '开头匹配', value: 'StartsWith' },
  { label: '结尾匹配', value: 'EndsWith' },
  { label: '正则表达式', value: 'Regex' }
]

const AMOUNT_OPERATOR_OPTIONS: { label: string; value: RuleMatchOperator }[] = [
  { label: '等于', value: 'Equals' },
  { label: '区间', value: 'Range' }
]

interface Props {
  visible: boolean
  rule: Rule | null
}

const props = defineProps<Props>()
const emit = defineEmits(['update:visible', 'success'])

const formRef = ref<FormInstance>()
const submitting = ref(false)
const categories = ref<Category[]>([])

const isEdit = computed(() => !!props.rule)

const formData = reactive({
  name: '',
  categoryId: 0,
  matchField: 'CounterpartyName' as RuleMatchField,
  matchOperator: 'Contains' as RuleMatchOperator,
  matchValue: '',
  matchValueMax: '',
  priority: 0,
  isActive: true
})

const operatorOptions = computed(() =>
  formData.matchField === 'Amount' ? AMOUNT_OPERATOR_OPTIONS : STRING_OPERATOR_OPTIONS
)

const isRange = computed(() => formData.matchOperator === 'Range')

// 字段切换时，若当前 operator 不在新字段的白名单内则回退到第一项
watch(() => formData.matchField, () => {
  const allowed = operatorOptions.value.map(o => o.value)
  const fallbackOperator = allowed[0]
  if (fallbackOperator && !allowed.includes(formData.matchOperator)) {
    formData.matchOperator = fallbackOperator
  }
})

// 切出 Range 时清空上限值，避免残留旧值误提交
watch(() => formData.matchOperator, (op) => {
  if (op !== 'Range') formData.matchValueMax = ''
})

const validateAmountNumeric = (_rule: unknown, value: string, callback: (err?: Error) => void) => {
  if (formData.matchField !== 'Amount') return callback()
  if (value === '' || value == null) return callback()
  if (Number.isFinite(Number(value))) return callback()
  callback(new Error('金额字段的匹配值必须为数字'))
}

const validateRangeMax = (_rule: unknown, value: string, callback: (err?: Error) => void) => {
  if (!isRange.value) return callback()
  if (value === '' || value == null) return callback() // 允许留空
  if (!Number.isFinite(Number(value))) return callback(new Error('上限必须为数字'))
  if (Number(value) < Number(formData.matchValue)) {
    return callback(new Error('上限必须大于或等于下限'))
  }
  callback()
}

const rules: FormRules = {
  name: [{ required: true, message: '请输入规则名称', trigger: 'blur' }],
  categoryId: [{ required: true, message: '请选择分类', trigger: 'change' }],
  matchField: [{ required: true, message: '请选择匹配字段', trigger: 'change' }],
  matchOperator: [{ required: true, message: '请选择匹配操作符', trigger: 'change' }],
  matchValue: [
    { required: true, message: '请输入匹配值', trigger: 'blur' },
    { validator: validateAmountNumeric, trigger: 'blur' }
  ],
  matchValueMax: [{ validator: validateRangeMax, trigger: 'blur' }],
  priority: [{ required: true, message: '请输入优先级', trigger: 'blur' }]
}

watch(
  () => props.visible,
  async (val) => {
    if (val) {
      if (props.rule) {
        formData.name = props.rule.name
        formData.categoryId = props.rule.categoryId
        formData.matchField = props.rule.matchField
        formData.matchOperator = props.rule.matchOperator
        formData.matchValue = props.rule.matchValue
        formData.matchValueMax = props.rule.matchValueMax ?? ''
        formData.priority = props.rule.priority
        formData.isActive = props.rule.isActive
      } else {
        resetForm()
      }
      await loadCategories()
    }
  }
)

const loadCategories = async () => {
  try {
    const response = await getActiveCategories()
    categories.value = response.data.data
  } catch (error) {
    console.error('Failed to load categories:', error)
  }
}

const resetForm = () => {
  formData.name = ''
  formData.categoryId = 0
  formData.matchField = 'CounterpartyName'
  formData.matchOperator = 'Contains'
  formData.matchValue = ''
  formData.matchValueMax = ''
  formData.priority = 0
  formData.isActive = true
  formRef.value?.clearValidate()
}

const handleClose = () => {
  emit('update:visible', false)
}

const handleSubmit = async () => {
  if (!formRef.value) return

  await formRef.value.validate(async (valid) => {
    if (!valid) return

    submitting.value = true
    try {
      const matchValueMax = isRange.value && formData.matchValueMax !== ''
        ? formData.matchValueMax
        : null

      if (isEdit.value && props.rule) {
        await updateRule(props.rule.id, {
          name: formData.name,
          categoryId: formData.categoryId,
          matchField: formData.matchField,
          matchOperator: formData.matchOperator,
          matchValue: formData.matchValue,
          matchValueMax,
          priority: formData.priority,
          isActive: formData.isActive
        })
        ElMessage.success('更新成功')
      } else {
        await createRule({
          name: formData.name,
          categoryId: formData.categoryId,
          matchField: formData.matchField,
          matchOperator: formData.matchOperator,
          matchValue: formData.matchValue,
          matchValueMax,
          priority: formData.priority
        })
        ElMessage.success('创建成功')
      }
      emit('success')
      handleClose()
    } catch (error) {
      console.error('Failed to submit rule:', error)
    } finally {
      submitting.value = false
    }
  })
}
</script>
