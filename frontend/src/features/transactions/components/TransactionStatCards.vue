<template>
  <el-row :gutter="24" class="transaction-stat-cards" v-loading="loading">
    <el-col :xs="24" :sm="12" :md="6">
      <StatCard
        :icon="TrendCharts"
        :value="formatAmount(statistics?.totalIncome ?? 0)"
        label="总收入"
        :count="`${statistics?.incomeCount ?? 0} 笔收入`"
        theme="income"
      />
    </el-col>
    <el-col :xs="24" :sm="12" :md="6">
      <StatCard
        :icon="Minus"
        :value="formatAmount(statistics?.totalExpense ?? 0)"
        label="总支出"
        :count="`${statistics?.expenseCount ?? 0} 笔支出`"
        theme="expense"
      />
    </el-col>
    <el-col :xs="24" :sm="12" :md="6">
      <StatCard
        :icon="Coin"
        :value="netProfitDisplay"
        label="净收益"
        :count="`共 ${statistics?.totalCount ?? 0} 笔交易`"
        theme="profit"
      />
    </el-col>
    <el-col :xs="24" :sm="12" :md="6">
      <StatCard
        :icon="Sort"
        :value="formatAmount(statistics?.totalTransfer ?? 0)"
        label="转账总额"
        :count="`${statistics?.transferCount ?? 0} 笔转账`"
        theme="transfer"
      />
    </el-col>
  </el-row>
</template>

<script setup lang="ts">
import { computed } from 'vue'
import { TrendCharts, Minus, Coin, Sort } from '@element-plus/icons-vue'
import StatCard from '@/shared/ui/StatCard.vue'
import type { TransactionStatistics } from '@/features/transactions/types/transaction'

interface Props {
  statistics: TransactionStatistics | null
  loading?: boolean
}

const props = withDefaults(defineProps<Props>(), {
  loading: false
})

const formatAmount = (value: number) => {
  return `¥${Number(value).toLocaleString('zh-CN', {
    minimumFractionDigits: 2,
    maximumFractionDigits: 2
  })}`
}

const netProfitDisplay = computed(() => {
  const net = props.statistics?.netProfit ?? 0
  const prefix = net >= 0 ? '+' : ''
  return `${prefix}${formatAmount(net)}`
})
</script>

<style scoped>
.transaction-stat-cards {
  margin-bottom: 16px;
}
</style>
