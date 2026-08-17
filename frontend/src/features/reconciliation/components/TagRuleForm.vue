<template>
  <el-dialog
    :model-value="modelValue"
    :title="isEdit ? '编辑标签规则' : '新增标签规则'"
    width="620px"
    @close="handleClose"
  >
    <el-form
      ref="formRef"
      :model="formData"
      :rules="rules"
      label-width="100px"
    >
      <el-form-item label="规则名称" prop="ruleName">
        <el-input v-model="formData.ruleName" placeholder="请输入规则名称" />
      </el-form-item>

      <el-form-item label="目标范围" prop="targetScope">
        <el-select v-model="formData.targetScope" placeholder="请选择目标范围" style="width: 100%">
          <el-option label="交易记录" value="Transaction" />
        </el-select>
        <div style="color: var(--text-placeholder); font-size: 12px; margin-top: 4px">
          当前仅支持对交易记录执行标签规则
        </div>
      </el-form-item>

      <el-form-item label="匹配字段" prop="matchField">
        <el-select v-model="formData.matchField" placeholder="请选择匹配字段" style="width: 100%">
          <el-option label="对方名称" value="CounterpartyName" />
          <el-option label="描述/摘要" value="Description" />
          <el-option label="备注" value="Memo" />
          <el-option label="金额" value="Amount" />
        </el-select>
      </el-form-item>

      <el-form-item label="匹配操作符" prop="matchOperator">
        <el-select v-model="formData.matchOperator" placeholder="请选择匹配操作符" style="width: 100%">
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

      <el-form-item label="标签" prop="selectedTags">
        <el-select
          v-model="formData.selectedTags"
          multiple
          filterable
          allow-create
          default-first-option
          placeholder="选择已有标签或输入新标签名称"
          style="width: 100%"
          value-key="value"
        >
          <el-option
            v-for="tag in availableTags"
            :key="tag.id"
            :label="tag.name"
            :value="String(tag.id)"
          >
            <span
              v-if="tag.color"
              :style="{ display: 'inline-block', width: '10px', height: '10px', borderRadius: '50%', backgroundColor: tag.color, marginRight: '6px', verticalAlign: 'middle' }"
            />
            {{ tag.name }}
          </el-option>
        </el-select>
        <div style="color: var(--text-placeholder); font-size: 12px; margin-top: 4px">
          可选择已有标签，也可直接输入新标签名称按 Enter 创建
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
import { createTagRule, updateTagRule } from '@/features/reconciliation/api/tagRule'
import { getTags } from '@/features/master-data/tags/api/tag'
import type {
  TagRule,
  TagRuleMatchField,
  TagRuleMatchOperator
} from '@/features/reconciliation/types/tagRule'
import type { Tag } from '@/features/master-data/tags/types/tag'

const STRING_OPERATOR_OPTIONS: { label: string; value: TagRuleMatchOperator }[] = [
  { label: '包含', value: 'Contains' },
  { label: '精确匹配', value: 'Equals' },
  { label: '开头匹配', value: 'StartsWith' },
  { label: '结尾匹配', value: 'EndsWith' },
  { label: '正则表达式', value: 'Regex' }
]

const AMOUNT_OPERATOR_OPTIONS: { label: string; value: TagRuleMatchOperator }[] = [
  { label: '精确匹配', value: 'Equals' },
  { label: '区间', value: 'Range' }
]

interface Props {
  modelValue: boolean
  tagRule: TagRule | null
}

const props = defineProps<Props>()
const emit = defineEmits<{
  (e: 'update:modelValue', value: boolean): void
  (e: 'success'): void
}>()

const formRef = ref<FormInstance>()
const submitting = ref(false)
const availableTags = ref<Tag[]>([])

const isEdit = computed(() => !!props.tagRule)

const formData = reactive({
  ruleName: '',
  targetScope: 'Transaction',
  matchField: 'CounterpartyName' as TagRuleMatchField,
  matchOperator: 'Contains' as TagRuleMatchOperator,
  matchValue: '',
  matchValueMax: '',
  selectedTags: [] as string[],
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
  ruleName: [{ required: true, message: '请输入规则名称', trigger: 'blur' }],
  targetScope: [{ required: true, message: '请选择目标范围', trigger: 'change' }],
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
  () => props.modelValue,
  async (val) => {
    if (val) {
      if (props.tagRule) {
        formData.ruleName = props.tagRule.ruleName
        formData.targetScope = props.tagRule.targetScope
        formData.matchField = props.tagRule.matchField
        formData.matchOperator = props.tagRule.matchOperator
        formData.matchValue = props.tagRule.matchValue
        formData.matchValueMax = props.tagRule.matchValueMax ?? ''
        formData.priority = props.tagRule.priority
        formData.isActive = props.tagRule.isActive
        formData.selectedTags = props.tagRule.tags.map(t => String(t.tagId))
      } else {
        resetForm()
      }
      await loadTags()
    }
  }
)

const loadTags = async () => {
  try {
    const response = await getTags({ scope: 'transaction', isActive: true })
    availableTags.value = response.data.data
  } catch (error) {
    console.error('加载标签失败:', error)
  }
}

const resetForm = () => {
  formData.ruleName = ''
  formData.targetScope = 'Transaction'
  formData.matchField = 'CounterpartyName'
  formData.matchOperator = 'Contains'
  formData.matchValue = ''
  formData.matchValueMax = ''
  formData.selectedTags = []
  formData.priority = 0
  formData.isActive = true
  formRef.value?.clearValidate()
}

const handleClose = () => {
  emit('update:modelValue', false)
}

const parseSelectedTags = () => {
  const existingTagIds: number[] = []
  const newTagNames: string[] = []
  const tagIdSet = new Set(availableTags.value.map(t => String(t.id)))

  for (const val of formData.selectedTags) {
    if (tagIdSet.has(val)) {
      existingTagIds.push(Number(val))
    } else {
      newTagNames.push(val)
    }
  }
  return { tagIds: existingTagIds, newTagNames }
}

const handleSubmit = async () => {
  if (!formRef.value) return

  await formRef.value.validate(async (valid) => {
    if (!valid) return

    submitting.value = true
    try {
      const { tagIds, newTagNames } = parseSelectedTags()

      const matchValueMax = isRange.value && formData.matchValueMax !== ''
        ? formData.matchValueMax
        : null

      if (isEdit.value && props.tagRule) {
        await updateTagRule(props.tagRule.id, {
          ruleName: formData.ruleName,
          priority: formData.priority,
          targetScope: formData.targetScope,
          matchField: formData.matchField,
          matchOperator: formData.matchOperator,
          matchValue: formData.matchValue,
          matchValueMax,
          isActive: formData.isActive,
          tagIds,
          newTagNames
        })
        ElMessage.success('更新成功')
      } else {
        await createTagRule({
          ruleName: formData.ruleName,
          priority: formData.priority,
          targetScope: formData.targetScope,
          matchField: formData.matchField,
          matchOperator: formData.matchOperator,
          matchValue: formData.matchValue,
          matchValueMax,
          tagIds,
          newTagNames
        })
        ElMessage.success('创建成功')
      }
      emit('success')
      handleClose()
    } catch (error) {
      console.error('提交标签规则失败:', error)
    } finally {
      submitting.value = false
    }
  })
}
</script>
