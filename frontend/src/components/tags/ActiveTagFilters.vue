<template>
  <div v-if="selectedTags.length > 0" class="active-tag-filters">
    <span class="filter-label">标签筛选：</span>
    <el-tag
      v-for="tag in selectedTags"
      :key="tag.id"
      closable
      size="small"
      :color="tag.color"
      :style="tag.color ? { color: getTextColor(tag.color), borderColor: tag.color } : {}"
      @close="handleRemove(tag.id)"
      class="filter-tag"
    >
      {{ tag.name }}
    </el-tag>
    <el-button link type="primary" size="small" @click="handleClearAll">
      清除全部
    </el-button>
  </div>
</template>

<script setup lang="ts">
import { computed } from 'vue'
import { useTagStore } from '@/features/master-data/tags/stores/tagStore'

interface Props {
  tagIds: number[]
  scope: string
}

const props = defineProps<Props>()

const emit = defineEmits<{
  (e: 'remove', tagId: number): void
  (e: 'clear'): void
}>()

const tagStore = useTagStore()

const selectedTags = computed(() => {
  const tags = tagStore.tagsByScope[props.scope] || []
  return props.tagIds
    .map(id => tags.find((t: any) => t.id === id))
    .filter(Boolean)
    .map((tag: any) => ({
      id: tag.id,
      name: tag.name,
      color: tag.color
    }))
})

const handleRemove = (tagId: number) => {
  emit('remove', tagId)
}

const handleClearAll = () => {
  emit('clear')
}

const getTextColor = (bgColor: string): string => {
  try {
    let hex = bgColor.replace('#', '')
    if (hex.length === 3) {
      hex = hex.split('').map(c => c + c).join('')
    }
    if (hex.length !== 6) return '#333333'

    const r = parseInt(hex.slice(0, 2), 16)
    const g = parseInt(hex.slice(2, 4), 16)
    const b = parseInt(hex.slice(4, 6), 16)
    const yiq = (r * 299 + g * 587 + b * 114) / 1000
    return yiq >= 128 ? '#333333' : '#ffffff'
  } catch {
    return '#333333'
  }
}
</script>

<style scoped>
.active-tag-filters {
  display: flex;
  align-items: center;
  gap: 8px;
  padding: 12px 16px;
  background: var(--el-fill-color-light);
  border-radius: 8px;
  margin-bottom: 16px;
  flex-wrap: wrap;
}

.filter-label {
  font-size: 13px;
  color: var(--el-text-color-secondary);
  font-weight: 500;
}

.filter-tag {
  border-radius: 4px;
}
</style>
