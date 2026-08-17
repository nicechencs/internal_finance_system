<template>
  <DetailSummaryCards :items="cards" :layout="layout" />
</template>

<script setup lang="ts">
import { computed } from 'vue'
import DetailSummaryCards, { type DetailSummaryCardItem } from '@/shared/ui/DetailSummaryCards.vue'
import type { TransactionStatistics } from '@/features/transactions/types/transaction'

interface Props {
  statistics: TransactionStatistics
  layout?: 'default' | 'stacked'
}

const props = withDefaults(defineProps<Props>(), {
  layout: 'default'
})

const formatAmount = (value: number) => {
  return `¥${Number(value).toLocaleString('zh-CN', {
    minimumFractionDigits: 2,
    maximumFractionDigits: 2
  })}`
}

const cards = computed<DetailSummaryCardItem[]>(() => {
  const { statistics } = props

  return [
    {
      key: 'income',
      label: '总收入',
      value: formatAmount(statistics.totalIncome),
      meta: `${statistics.incomeCount} 笔收入`,
      tone: 'income',
      valueClass: ''
    },
    {
      key: 'expense',
      label: '总支出',
      value: formatAmount(statistics.totalExpense),
      meta: `${statistics.expenseCount} 笔支出`,
      tone: 'expense',
      valueClass: ''
    },
    {
      key: 'profit',
      label: '净收益',
      value: `${statistics.netProfit >= 0 ? '+' : ''}${formatAmount(statistics.netProfit)}`,
      meta: '收入减去支出',
      tone: 'profit',
      valueClass: statistics.netProfit >= 0 ? 'positive' : 'negative'
    },
    {
      key: 'total',
      label: '交易总数',
      value: String(statistics.totalCount),
      meta: statistics.totalTransfer > 0
        ? `转账金额 ${formatAmount(statistics.totalTransfer)}`
        : '当前暂无转账统计',
      tone: 'info',
      valueClass: 'summary-value--count'
    }
  ]
})
</script>
