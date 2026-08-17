<template>
  <div class="page-header" :class="{ 'page-header--mobile': isMobile }">
    <div class="page-header__text">
      <h2 class="page-title">{{ title }}</h2>
      <p v-if="description" class="page-desc">{{ description }}</p>
    </div>
    <div v-if="hasActions" class="page-header__actions">
      <template v-if="!isMobile">
        <el-button
          v-for="action in visibleActions"
          :key="action.command"
          :type="action.type || 'default'"
          @click="emit('action', action.command)"
        >
          {{ action.label }}
        </el-button>
      </template>
      <el-dropdown
        v-else-if="visibleActions.length"
        trigger="click"
        @command="emit('action', $event)"
      >
        <el-button class="page-header__more">
          更多
          <el-icon class="el-icon--right"><ArrowDown /></el-icon>
        </el-button>
        <template #dropdown>
          <el-dropdown-menu>
            <el-dropdown-item
              v-for="action in visibleActions"
              :key="action.command"
              :command="action.command"
            >
              {{ action.label }}
            </el-dropdown-item>
          </el-dropdown-menu>
        </template>
      </el-dropdown>
      <slot name="primary" />
    </div>
  </div>
</template>

<script setup lang="ts">
import { computed, useSlots } from 'vue'
import { ArrowDown } from '@element-plus/icons-vue'
import { useBreakpoint } from '@/shared/composables/useBreakpoint'

export interface PageHeaderAction {
  label: string
  command: string
  type?: 'default' | 'primary' | 'success' | 'warning' | 'danger' | 'info'
  visible?: boolean
}

const props = withDefaults(defineProps<{
  title: string
  description?: string
  actions?: PageHeaderAction[]
}>(), {
  actions: () => []
})

const emit = defineEmits<{
  action: [command: string]
}>()

const slots = useSlots()
const { isMobile } = useBreakpoint()

const visibleActions = computed(() =>
  props.actions.filter((action) => action.visible !== false)
)

const hasActions = computed(() =>
  visibleActions.value.length > 0 || Boolean(slots.primary)
)
</script>

<style scoped>
.page-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: var(--spacing-base);
  margin-bottom: var(--spacing-xl);
}

.page-title {
  margin: 0;
}

.page-desc {
  margin: 4px 0 0;
  font-size: var(--font-size-sm);
  color: var(--text-secondary);
}

.page-header__actions {
  display: flex;
  align-items: center;
  gap: var(--spacing-md);
  flex-shrink: 0;
}

.page-header--mobile {
  align-items: flex-start;
}

.page-header--mobile .page-header__actions {
  width: 100%;
  justify-content: flex-end;
}
</style>
