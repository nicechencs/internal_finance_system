<template>
  <div v-if="visible" class="mobile-action-bar">
    <slot />
  </div>
</template>

<script setup lang="ts">
withDefaults(defineProps<{
  visible?: boolean
}>(), {
  visible: true
})
</script>

<style scoped>
.mobile-action-bar {
  position: sticky;
  bottom: 0;
  z-index: var(--z-sticky);
  display: flex;
  gap: var(--spacing-md);
  padding: 12px 16px calc(12px + var(--safe-area-bottom));
  background: var(--bg-card);
  border-top: 1px solid var(--border-base);
  box-shadow: var(--shadow-sm);
}

.mobile-action-bar :deep(.el-button) {
  flex: 1;
}
</style>
