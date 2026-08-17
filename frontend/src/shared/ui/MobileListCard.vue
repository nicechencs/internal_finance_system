<template>
  <article
    class="mobile-list-card"
    :class="{ 'is-clickable': clickable }"
    @click="clickable && emit('click')"
  >
    <div class="mobile-list-card__row">
      <div class="mobile-list-card__main">
        <div class="mobile-list-card__title-row">
          <h3 class="mobile-list-card__title">{{ title }}</h3>
          <div v-if="$slots.tag" class="mobile-list-card__tag">
            <slot name="tag" />
          </div>
        </div>
        <div v-if="$slots.meta" class="mobile-list-card__meta">
          <slot name="meta" />
        </div>
      </div>
      <div v-if="showAmount" class="mobile-list-card__amount">
        <AmountText :value="amount!" :type="amountType" size="md" :signed="signed" />
      </div>
    </div>
    <div v-if="$slots.footer" class="mobile-list-card__footer">
      <slot name="footer" />
    </div>
  </article>
</template>

<script setup lang="ts">
import { computed } from 'vue'
import AmountText from '@/shared/ui/AmountText.vue'
import type { AmountTone } from '@/shared/utils/formatters'

const props = withDefaults(defineProps<{
  title: string
  amount?: number | string
  amountType?: AmountTone
  signed?: boolean
  clickable?: boolean
}>(), {
  amountType: 'neutral',
  signed: true,
  clickable: true
})

const emit = defineEmits<{
  click: []
}>()

const showAmount = computed(() => props.amount !== undefined && props.amount !== null && props.amount !== '')
</script>

<style scoped>
.mobile-list-card {
  background: var(--bg-card);
  border: 1px solid var(--border-base);
  border-radius: var(--radius-xl);
  padding: 12px 16px;
  box-shadow: var(--shadow-xs);
}

.mobile-list-card + .mobile-list-card {
  margin-top: var(--spacing-sm);
}

.mobile-list-card.is-clickable {
  cursor: pointer;
}

.mobile-list-card.is-clickable:active {
  background: var(--bg-hover);
}

.mobile-list-card__row {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  gap: var(--spacing-md);
}

.mobile-list-card__main {
  min-width: 0;
  flex: 1;
}

.mobile-list-card__title-row {
  display: flex;
  align-items: center;
  gap: var(--spacing-sm);
  min-width: 0;
}

.mobile-list-card__title {
  margin: 0;
  font-size: var(--font-size-card-title);
  font-weight: var(--font-weight-medium);
  color: var(--text-primary);
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.mobile-list-card__tag {
  flex-shrink: 0;
}

.mobile-list-card__meta {
  margin-top: 4px;
  font-size: var(--font-size-sm);
  color: var(--text-secondary);
  line-height: var(--line-height-snug);
}

.mobile-list-card__amount {
  flex-shrink: 0;
  padding-top: 1px;
}

.mobile-list-card__footer {
  margin-top: var(--spacing-sm);
  padding-top: var(--spacing-sm);
  border-top: 1px solid var(--border-light);
  font-size: var(--font-size-sm);
  color: var(--text-secondary);
}
</style>
