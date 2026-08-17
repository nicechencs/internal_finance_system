<template>
  <span v-if="tags && tags.length > 0" class="tag-display">
    <el-tag
      v-for="tag in displayTags"
      :key="tag.tagId"
      :color="tag.tagColor || undefined"
      :size="size"
      :style="tag.tagColor ? { color: getTextColor(tag.tagColor), borderColor: tag.tagColor } : {}"
      class="tag-item"
    >
      {{ tag.tagName }}
    </el-tag>
    <el-tag v-if="hasMore" type="info" :size="size" class="tag-more">
      +{{ tags.length - maxDisplay }}
    </el-tag>
  </span>
  <span v-else class="tag-empty">-</span>
</template>

<script setup lang="ts">
import { computed } from 'vue'

// ── Props ──
interface TagItem {
  tagId: number
  tagName: string
  tagColor?: string
}

interface Props {
  tags: TagItem[]
  size?: 'small' | 'default' | 'large'
  maxDisplay?: number
}

const props = withDefaults(defineProps<Props>(), {
  size: 'small',
  maxDisplay: 3,
})

const displayTags = computed(() =>
  props.tags.slice(0, props.maxDisplay)
)

const hasMore = computed(() =>
  props.tags.length > props.maxDisplay
)

/**
 * 根据背景色自动计算合适的文字颜色（黑/白）
 * 使用相对亮度公式，确保可读性
 */
const getTextColor = (bgColor: string): string => {
  try {
    // 支持 #rrggbb 和 #rgb 格式
    let hex = bgColor.replace('#', '')
    if (hex.length === 3) {
      hex = hex.split('').map(c => c + c).join('')
    }
    if (hex.length !== 6) return '#333333'

    const r = parseInt(hex.slice(0, 2), 16)
    const g = parseInt(hex.slice(2, 4), 16)
    const b = parseInt(hex.slice(4, 6), 16)
    // YIQ 亮度公式
    const yiq = (r * 299 + g * 587 + b * 114) / 1000
    return yiq >= 128 ? '#333333' : '#ffffff'
  } catch {
    return '#333333'
  }
}
</script>

<style scoped>
.tag-display {
  display: inline-flex;
  flex-wrap: wrap;
  gap: 6px;
  align-items: center;
}

.tag-item {
  border-radius: 4px;
}

.tag-more {
  font-size: 12px;
}

.tag-empty {
  color: var(--text-placeholder);
}
</style>
