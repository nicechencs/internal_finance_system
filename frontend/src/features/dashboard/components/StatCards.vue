<template>
  <el-row :gutter="24" class="stat-cards">
    <el-col :xs="24" :sm="12" :md="6">
      <StatCard
        :icon="TrendCharts"
        :value="formatMoney(summary.totalIncome)"
        label="总收入"
        theme="income"
      />
    </el-col>
    <el-col :xs="24" :sm="12" :md="6">
      <StatCard
        :icon="Minus"
        :value="formatMoney(summary.totalExpense)"
        label="总支出"
        theme="expense"
      />
    </el-col>
    <el-col :xs="24" :sm="12" :md="6">
      <StatCard
        :icon="Coin"
        :value="formatMoney(summary.netProfit)"
        label="净利润"
        theme="profit"
      />
    </el-col>
    <el-col :xs="24" :sm="12" :md="6">
      <StatCard
        :icon="Wallet"
        :value="formatMoney(summary.totalBalance)"
        label="总余额"
        theme="balance"
      />
    </el-col>
  </el-row>
</template>

<script setup lang="ts">
import { TrendCharts, Minus, Coin, Wallet } from '@element-plus/icons-vue'
import type { DashboardSummary } from '@/features/dashboard/types/dashboard'
import StatCard from '@/shared/ui/StatCard.vue'
import { formatMoney } from '@/shared/utils/formatters'

interface Props {
  summary: DashboardSummary
}

defineProps<Props>()
</script>

<style scoped>
.stat-cards {
  margin-bottom: 24px;
}
</style>
