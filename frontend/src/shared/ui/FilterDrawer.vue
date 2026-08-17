<template>
  <div class="filter-drawer-host">
    <template v-if="isMobile">
      <div class="filter-toolbar">
        <div class="filter-toolbar__leading">
          <slot name="leading" />
        </div>
        <el-button class="filter-toolbar__trigger" @click="visible = true">
          筛选
          <span v-if="activeCount > 0" class="filter-count">{{ activeCount }}</span>
        </el-button>
      </div>
      <el-drawer
        v-model="visible"
        class="mobile-filter-drawer"
        direction="btt"
        size="70%"
        title="筛选条件"
        :append-to-body="true"
      >
        <div class="filter-drawer-body">
          <slot />
        </div>
        <template #footer>
          <el-button @click="handleReset">重置</el-button>
          <el-button type="primary" @click="handleApply">应用</el-button>
        </template>
      </el-drawer>
    </template>
    <div v-else class="filter-inline">
      <slot />
    </div>
  </div>
</template>

<script setup lang="ts">
import { useBreakpoint } from '@/shared/composables/useBreakpoint'

withDefaults(defineProps<{
  activeCount?: number
}>(), {
  activeCount: 0
})

const visible = defineModel<boolean>({ default: false })

const emit = defineEmits<{
  apply: []
  reset: []
}>()

const { isMobile } = useBreakpoint()

const handleApply = () => {
  emit('apply')
  visible.value = false
}

const handleReset = () => {
  emit('reset')
}
</script>

<style scoped>
.filter-toolbar {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: var(--spacing-md);
  margin-bottom: var(--spacing-base);
}

.filter-toolbar__leading {
  flex: 1;
  min-width: 0;
}

.filter-toolbar__trigger {
  flex-shrink: 0;
}

.filter-count {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  min-width: 18px;
  height: 18px;
  margin-left: 6px;
  padding: 0 5px;
  border-radius: var(--radius-full);
  background: var(--color-primary);
  color: var(--text-inverse);
  font-size: 12px;
  line-height: 1;
}

.filter-inline {
  width: 100%;
}
</style>
