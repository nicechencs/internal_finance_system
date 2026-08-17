<template>
  <div class="summary-overview" v-loading="loading">
    <h3 class="summary-overview-title">{{ title }}</h3>
    <p class="summary-overview-subtitle">{{ subtitle }}</p>
    <template v-if="!loading && !empty">
      <slot />
    </template>
    <el-empty v-if="!loading && empty" :description="emptyText" :image-size="72" />
  </div>
</template>

<script setup lang="ts">
interface Props {
  title?: string
  subtitle?: string
  loading?: boolean
  empty?: boolean
  emptyText?: string
}

withDefaults(defineProps<Props>(), {
  title: '收支概览',
  subtitle: '快速查看收入、支出和净收益表现',
  loading: false,
  empty: false,
  emptyText: '暂无统计数据'
})
</script>

<style scoped>
.summary-overview {
  background: var(--bg-card);
  border: 1px solid var(--border-base);
  border-radius: 16px;
  padding: 18px 20px;
  box-shadow: 0 12px 28px rgba(15, 23, 42, 0.06);
  margin-top: 16px;
}

.summary-overview-title {
  margin: 0;
  font-size: 14px;
  font-weight: 600;
  color: var(--text-primary);
}

.summary-overview-subtitle {
  margin: 6px 0 16px;
  color: var(--text-secondary);
  font-size: 12px;
  line-height: 1.5;
}
</style>
