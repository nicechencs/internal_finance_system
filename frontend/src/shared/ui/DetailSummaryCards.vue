<template>
  <div class="summary-grid" :class="`summary-grid--${layout}`">
    <div
      v-for="card in items"
      :key="card.key"
      class="summary-item"
      :style="toneStyle(card.tone)"
    >
      <div class="summary-label">{{ card.label }}</div>
      <div class="summary-value" :class="card.valueClass">{{ card.value }}</div>
      <div v-if="card.meta" class="summary-meta">{{ card.meta }}</div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { CARD_COLORS } from '@/shared/constants/colors'

/** tone → CARD_COLORS 键映射（info 向后兼容，映射到 neutral） */
const TONE_MAP: Record<string, keyof typeof CARD_COLORS> = {
  income:   'income',
  expense:  'expense',
  profit:   'profit',
  balance:  'balance',
  transfer: 'transfer',
  neutral:  'neutral',
  info:     'neutral',   // 向后兼容
}

function toneStyle(tone: string): Record<string, string> {
  const colorKey = TONE_MAP[tone] ?? 'neutral'
  const c = CARD_COLORS[colorKey]
  return {
    background: c.gradient,
    borderColor: c.border,
  }
}

export interface DetailSummaryCardItem {
  key: string
  label: string
  value: string
  meta?: string
  tone: 'income' | 'expense' | 'profit' | 'balance' | 'transfer' | 'neutral' | 'info'
  valueClass?: string
}

interface Props {
  items: DetailSummaryCardItem[]
  layout?: 'default' | 'stacked'
}

withDefaults(defineProps<Props>(), {
  layout: 'default'
})
</script>

<style scoped>
.summary-grid {
  display: grid;
  grid-template-columns: repeat(4, minmax(0, 1fr));
  gap: 12px;
  align-content: start;
}

.summary-grid--stacked {
  grid-template-columns: repeat(2, minmax(0, 1fr));
}

.summary-item {
  min-height: 112px;
  padding: 14px;
  border-radius: 14px;
  border: 1px solid transparent;
  display: flex;
  flex-direction: column;
  justify-content: space-between;
  overflow: hidden;
}

.summary-grid--stacked .summary-item {
  min-height: 88px;
  padding: 12px;
}

.summary-label {
  font-size: 12px;
  color: var(--text-regular);
}

.summary-value {
  font-size: 20px;
  line-height: 1.2;
  font-weight: 700;
  color: var(--text-primary);
  word-break: break-word;
}

.summary-value--count,
.summary-value.positive {
  color: var(--color-primary);
}

.summary-value.negative {
  color: var(--color-danger);
}

.summary-meta {
  font-size: 12px;
  line-height: 1.5;
  color: var(--text-secondary);
}

@media (max-width: 1100px) {
  .summary-grid {
    grid-template-columns: repeat(2, minmax(0, 1fr));
  }
}

@media (max-width: 640px) {
  .summary-grid {
    grid-template-columns: 1fr;
  }

  .summary-item {
    min-height: 96px;
  }
}
</style>
