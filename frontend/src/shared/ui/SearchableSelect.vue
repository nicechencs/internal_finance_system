<template>
  <el-select
    :model-value="modelValue"
    @update:model-value="handleUpdate"
    filterable
    :clearable="clearable"
    :disabled="disabled"
    :placeholder="computedPlaceholder"
    :no-match-text="computedNoMatchText"
    :no-data-text="computedNoDataText"
    :style="{ width: width }"
    :loading="loading"
  >
    <el-option
      v-for="item in options"
      :key="item[valueField]"
      :label="item[labelField]"
      :value="item[valueField]"
    />
  </el-select>
</template>

<script setup lang="ts">
import { computed } from 'vue'

interface Props {
  modelValue: number | string | null | undefined
  options: any[]
  entityName?: string
  labelField?: string
  valueField?: string
  placeholder?: string
  noMatchText?: string
  noDataText?: string
  clearable?: boolean
  disabled?: boolean
  width?: string
  loading?: boolean
}

const props = withDefaults(defineProps<Props>(), {
  entityName: '',
  labelField: 'name',
  valueField: 'id',
  placeholder: '',
  noMatchText: '',
  noDataText: '',
  clearable: true,
  disabled: false,
  width: '100%',
  loading: false
})

const emit = defineEmits<{
  'update:modelValue': [value: number | string | null | undefined]
  'change': [value: number | string | null | undefined]
}>()

const computedPlaceholder = computed(() => {
  if (props.placeholder) return props.placeholder
  if (props.entityName) return `输入关键字搜索${props.entityName}`
  return '请选择'
})

const computedNoMatchText = computed(() => {
  if (props.noMatchText) return props.noMatchText
  if (props.entityName) return `无匹配${props.entityName}`
  return '无匹配数据'
})

const computedNoDataText = computed(() => {
  if (props.noDataText) return props.noDataText
  if (props.entityName) return `暂无${props.entityName}数据`
  return '暂无数据'
})

const handleUpdate = (value: number | string | null | undefined) => {
  emit('update:modelValue', value)
  emit('change', value)
}
</script>
