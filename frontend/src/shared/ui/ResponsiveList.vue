<template>
  <div class="responsive-list">
    <div v-if="!isMobile" class="responsive-list__table">
      <slot name="table" />
    </div>
    <div v-else class="responsive-list__cards" v-loading="loading">
      <template v-if="items.length">
        <slot
          v-for="item in items"
          :key="resolveKey(item)"
          name="card"
          :item="item"
        />
      </template>
      <el-empty v-else :description="emptyText" />
    </div>
    <div v-if="$slots.pagination" class="responsive-list__pagination">
      <slot name="pagination" />
    </div>
  </div>
</template>

<script setup lang="ts" generic="T extends Record<string, any>">
import { useBreakpoint } from '@/shared/composables/useBreakpoint'

const props = withDefaults(defineProps<{
  items: T[]
  loading?: boolean
  itemKey?: string
  emptyText?: string
}>(), {
  loading: false,
  itemKey: 'id',
  emptyText: '暂无数据'
})

const { isMobile } = useBreakpoint()

const resolveKey = (item: T) => item[props.itemKey] ?? item.id
</script>

<style scoped>
.responsive-list__cards {
  min-height: 80px;
}

.responsive-list__pagination {
  margin-top: var(--spacing-sm);
}
</style>
