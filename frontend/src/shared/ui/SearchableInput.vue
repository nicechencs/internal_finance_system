<template>
  <el-select
    :model-value="modelValue"
    @update:model-value="$emit('update:modelValue', $event)"
    filterable
    allow-create
    default-first-option
    :placeholder="placeholder"
    :size="size"
    :clearable="clearable"
    :disabled="disabled"
    :loading="loading"
    :style="{ width }"
  >
    <el-option
      v-for="item in options"
      :key="item[labelField]"
      :label="item[labelField]"
      :value="item[labelField]"
    />
  </el-select>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { ElMessage } from 'element-plus'

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

const props = withDefaults(defineProps<Props>(), {
  labelField: 'name',
  placeholder: '请输入或选择',
  size: 'default',
  clearable: false,
  disabled: false,
  width: '100%'
})

defineEmits<{
  'update:modelValue': [value: string]
}>()

const options = ref<any[]>([])
const loading = ref(false)

onMounted(async () => {
  loading.value = true
  try {
    const response = await props.fetchOptions()
    // 支持 ApiResponse 包装格式 (response.data.data) 和直接数组
    const data = response?.data?.data ?? response?.data ?? response
    options.value = Array.isArray(data) ? data : []
  } catch (error) {
    console.error('Failed to load options:', error)
    ElMessage.warning('加载选项失败，您仍可手动输入')
  } finally {
    loading.value = false
  }
})
</script>

<style scoped>
/* 组件样式继承自 el-select */
</style>
