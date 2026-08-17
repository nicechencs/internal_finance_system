<template>
  <div class="stat-card" :class="themeClass">
    <div class="stat-label">{{ label }}</div>
    <div class="stat-main">
      <div class="stat-icon">
        <el-icon :size="24"><component :is="icon" /></el-icon>
      </div>
      <div class="stat-info">
        <div class="stat-value">{{ value }}</div>
        <div v-if="count" class="stat-count">{{ count }}</div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { computed, type Component } from 'vue'

interface Props {
  icon: Component
  value: string
  label: string
  count?: string
  theme: 'income' | 'expense' | 'profit' | 'transfer' | 'balance' | 'info'
}

const props = defineProps<Props>()

const themeClass = computed(() => `${props.theme}-card`)
</script>

<style scoped>
.stat-card {
  display: flex;
  flex-direction: column;
  background: var(--bg-card);
  border-radius: 12px;
  padding: 16px 20px;
  box-shadow: 0 1px 3px rgba(0, 0, 0, 0.04);
  transition: transform 0.25s ease, box-shadow 0.25s ease;
  margin-bottom: 12px;
  position: relative;
  overflow: hidden;
}

@media (hover: hover) {
  .stat-card:hover {
    transform: translateY(-2px);
    box-shadow: 0 10px 25px rgba(0, 0, 0, 0.08);
  }
}

.stat-card::before {
  content: '';
  position: absolute;
  top: 0;
  right: 0;
  width: 100px;
  height: 100px;
  border-radius: 50%;
  opacity: 0.03;
}

.stat-label {
  font-size: 13px;
  color: var(--text-secondary);
  margin-bottom: 8px;
}

.stat-main {
  display: flex;
  align-items: center;
  gap: 16px;
}

.stat-info {
  flex: 1;
}

.stat-value {
  font-size: var(--amount-font-size-lg);
  font-weight: 700;
  color: var(--text-primary);
  line-height: 1.2;
  font-variant-numeric: tabular-nums;
}

.stat-count {
  font-size: 12px;
  color: var(--text-placeholder);
  margin-top: 4px;
}

/* 收入 - 绿色（success 语义色） */
.income-card {
  border-left: 4px solid var(--color-success-light-3);
}

.income-card::before {
  background: var(--color-success-light-2);
}

.income-card .stat-icon {
  background: var(--color-success-light-5);
  color: var(--color-success);
}

/* 支出 - 红色（danger 语义色） */
.expense-card {
  border-left: 4px solid var(--color-danger-light-3);
}

.expense-card::before {
  background: var(--color-danger-light-2);
}

.expense-card .stat-icon {
  background: var(--color-danger-light-5);
  color: var(--color-danger);
}

/* 利润/净收益 - 主色（primary） */
.profit-card {
  border-left: 4px solid var(--color-primary-light-3);
}

.profit-card::before {
  background: var(--color-primary-light-2);
}

.profit-card .stat-icon {
  background: var(--color-primary-light-6);
  color: var(--color-primary);
}

/* 转账 - 中性色（neutral + primary accent） */
.transfer-card {
  border-left: 4px solid var(--text-disabled);
}

.transfer-card::before {
  background: var(--text-disabled);
}

.transfer-card .stat-icon {
  background: var(--bg-hover);
  color: var(--text-secondary);
}

/* 余额 - 主色系浅层级（primary-soft） */
.balance-card {
  border-left: 4px solid var(--color-primary-light-4);
}

.balance-card::before {
  background: var(--color-primary-light-3);
}

.balance-card .stat-icon {
  background: var(--color-primary-light-6);
  color: var(--color-primary-light-2);
}

/* 信息 - 主色系浅层级（同 balance，不与语义色竞争） */
.info-card {
  border-left: 4px solid var(--color-primary-light-4);
}

.info-card::before {
  background: var(--color-primary-light-3);
}

.info-card .stat-icon {
  background: var(--color-primary-light-6);
  color: var(--color-primary-light-2);
}

.stat-icon {
  width: 48px;
  height: 48px;
  border-radius: 12px;
  display: flex;
  align-items: center;
  justify-content: center;
  color: var(--text-inverse);
  flex-shrink: 0;
}

</style>
