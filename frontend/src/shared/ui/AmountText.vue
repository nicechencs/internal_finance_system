<template>
  <span class="amount-text" :class="[`amount-text--${type}`, `amount-text--${size}`]">
    {{ displayValue }}
  </span>
</template>

<script setup lang="ts">
import { computed } from 'vue'
import { formatMoney, type AmountTone } from '@/shared/utils/formatters'

const props = withDefaults(defineProps<{
  value: number | string
  type?: AmountTone
  size?: 'sm' | 'md' | 'lg'
  signed?: boolean
}>(), {
  type: 'neutral',
  size: 'md',
  signed: true
})

const displayValue = computed(() => {
  const formatted = formatMoney(props.value)
  if (!props.signed || props.type === 'neutral') return formatted
  if (props.type === 'expense') return `-${formatted}`
  return `+${formatted}`
})
</script>

<style scoped>
.amount-text {
  font-family: var(--font-family-mono);
  font-variant-numeric: tabular-nums;
  font-feature-settings: 'tnum';
  font-weight: var(--font-weight-semibold);
  line-height: var(--line-height-tight);
}

.amount-text--sm {
  font-size: var(--font-size-sm);
}

.amount-text--md {
  font-size: var(--amount-font-size);
}

.amount-text--lg {
  font-size: var(--amount-font-size-lg);
  font-weight: var(--font-weight-bold);
}

.amount-text--income {
  color: var(--color-success-dark-1);
}

.amount-text--expense {
  color: var(--color-danger-dark-1);
}

.amount-text--neutral {
  color: var(--text-primary);
}
</style>
