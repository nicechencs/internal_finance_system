<template>
  <el-menu
    :default-active="activeMenu"
    @select="emit('select', $event)"
    class="sidebar-menu"
  >
    <template v-for="group in menuGroups" :key="group.name">
      <el-menu-item-group :title="group.name">
        <el-menu-item v-for="item in group.items" :key="item.path" :index="item.path">
          <el-icon><component :is="item.icon" /></el-icon>
          <template #title>{{ item.title }}</template>
        </el-menu-item>
      </el-menu-item-group>
    </template>
  </el-menu>
</template>

<script setup lang="ts">
import { useAuth } from '@/shared/composables/useAuth'

defineProps<{
  activeMenu: string
}>()

const emit = defineEmits<{
  select: [index: string]
}>()

const { menuGroups } = useAuth()
</script>

<style scoped>
.sidebar-menu {
  flex: 1;
  border-right: none !important;
  overflow-y: auto;
  overflow-x: hidden;
}

:deep(.el-menu) {
  background-color: var(--bg-sidebar);
  border-right: none;
}

:deep(.sidebar-menu) {
  background-color: var(--bg-sidebar);
}

:deep(.el-menu-item-group__title) {
  color: var(--text-secondary) !important;
  font-size: 12px;
  font-weight: 600;
  text-transform: uppercase;
  letter-spacing: 0.5px;
  padding: 16px 20px 8px;
  line-height: 1;
}

:deep(.el-menu-item) {
  color: var(--text-on-dark) !important;
  height: 42px;
  line-height: 42px;
  min-height: var(--touch-target-min);
  font-size: 14px;
  margin: 2px 8px;
  border-radius: 6px;
  background-color: transparent !important;
  position: relative;
}

:deep(.el-menu-item .el-icon) {
  color: var(--text-placeholder);
}

:deep(.el-menu-item:hover) {
  background-color: var(--bg-sidebar-hover) !important;
  color: var(--bg-hover) !important;
}

:deep(.el-menu-item:hover .el-icon) {
  color: var(--border-base);
}

:deep(.el-menu-item.is-active) {
  background-color: var(--primary-soft-bg) !important;
  color: var(--color-primary-light-3) !important;
}

:deep(.el-menu-item.is-active .el-icon) {
  color: var(--color-primary-light-2);
}

:deep(.el-menu-item.is-active::before) {
  content: '';
  position: absolute;
  left: 0;
  top: 50%;
  transform: translateY(-50%);
  width: 3px;
  height: 60%;
  background-color: var(--color-primary);
  border-radius: 0 3px 3px 0;
}

.sidebar-menu::-webkit-scrollbar {
  width: 4px;
}

.sidebar-menu::-webkit-scrollbar-thumb {
  background-color: var(--bg-sidebar-hover);
  border-radius: 4px;
}

.sidebar-menu::-webkit-scrollbar-track {
  background: transparent;
}
</style>
