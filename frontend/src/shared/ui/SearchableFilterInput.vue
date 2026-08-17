<template>
  <SearchableInput
    :model-value="modelValue"
    @update:model-value="$emit('update:modelValue', $event)"
    :fetch-options="fetchOptions"
    :label-field="labelField"
    :placeholder="placeholder"
    :size="size"
    :clearable="clearable"
    :disabled="disabled"
    :width="width"
  />
</template>

<script setup lang="ts">
import SearchableInput from './SearchableInput.vue'

interface Props {
  modelValue: string
  fetchOptions: () => Promise<any>
  labelField?: string
  placeholder?: string
  size?: 'default' | 'small' | 'large'
  clearable?: boolean
  disabled?: boolean
  width?: string
}

withDefaults(defineProps<Props>(), {
  labelField: 'name',
  placeholder: '请输入或选择',
  size: 'default',
  clearable: false,
  disabled: false,
  width: '280px'
})

defineEmits<{
  'update:modelValue': [value: string]
}>()
</script>
