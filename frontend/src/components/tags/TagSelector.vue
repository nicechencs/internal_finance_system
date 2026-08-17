<template>
  <el-select
    :model-value="modelValue"
    :multiple="multiple"
    :placeholder="placeholder"
    :disabled="disabled"
    :loading="isLoading"
    clearable
    :max-collapse-tags="3"
    class="tag-selector"
    @change="handleChange"
  >
    <el-option
      v-for="tag in options"
      :key="tag.id"
      :label="tag.name"
      :value="tag.id"
    >
      <div class="tag-option">
        <span
          v-if="tag.color"
          class="color-dot"
          :style="{ backgroundColor: tag.color }"
        />
        <span>{{ tag.name }}</span>
      </div>
    </el-option>
  </el-select>
</template>

<script setup lang="ts">
import { ref, onMounted, computed, watch } from 'vue'
import { useTagStore } from '@/features/master-data/tags/stores/tagStore'
import type { Tag } from '@/features/master-data/tags/types/tag'

// ── Props ──
interface Props {
  modelValue: number[]
  scope: string
  placeholder?: string
  disabled?: boolean
  multiple?: boolean
}

const props = withDefaults(defineProps<Props>(), {
  placeholder: '请选择标签',
  disabled: false,
  multiple: true,
})

// ── Emits ──
const emit = defineEmits<{
  (e: 'update:modelValue', value: number[]): void
  (e: 'change', value: number[]): void
}>()

// ── Store ──
const tagStore = useTagStore()

// ── 状态 ──
const options = ref<Tag[]>([])
let activeLoadId = 0

const isLoading = computed(() => tagStore.loading[props.scope] ?? false)

// ── 数据加载 ──
const loadOptions = async () => {
  const loadId = ++activeLoadId
  const nextOptions = await tagStore.loadTagsByScope(props.scope)

  // Ignore stale responses when scope changes quickly.
  if (loadId !== activeLoadId) {
    return
  }

  options.value = nextOptions
}

// ── 事件处理 ──
const handleChange = (val: number | number[] | null | undefined) => {
  const nextValue = props.multiple
    ? Array.isArray(val) ? val : val !== null && val !== undefined ? [val] : []
    : val !== null && val !== undefined ? [val as number] : []

  emit('update:modelValue', nextValue)
  emit('change', nextValue)
}

// ── 初始化 ──
onMounted(() => {
  loadOptions()
})

// scope 变化时重新加载（应对父组件动态切换 scope 的场景）
watch(() => props.scope, (newScope, oldScope) => {
  if (newScope !== oldScope) {
    loadOptions()
  }
})
</script>

<style scoped>
.tag-selector {
  min-width: 200px;
  width: 100%;
}

.tag-option {
  display: flex;
  align-items: center;
  gap: 8px;
}

.color-dot {
  display: inline-block;
  width: 12px;
  height: 12px;
  border-radius: 50%;
  flex-shrink: 0;
  border: 1px solid rgba(0, 0, 0, 0.1);
}
</style>
