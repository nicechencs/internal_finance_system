<template>
  <div v-if="maturingDeposits.length > 0" class="maturity-alert-card">
    <div class="alert-header">
      <div class="alert-title">
        <el-icon :size="18" color="var(--color-warning)"><Bell /></el-icon>
        <span>定期存款到期提醒</span>
        <el-tag type="warning" size="small" round>{{ maturingDeposits.length }}</el-tag>
      </div>
      <el-button type="primary" link size="small" @click="$router.push('/fixed-deposits')">
        查看全部
        <el-icon class="el-icon--right"><ArrowRight /></el-icon>
      </el-button>
    </div>
    <div class="alert-body">
      <div
        v-for="deposit in maturingDeposits"
        :key="deposit.id"
        class="alert-item"
      >
        <div class="alert-item-left">
          <div class="alert-item-name">{{ deposit.accountName }}</div>
          <div class="alert-item-info">
            <span>本金 {{ formatMoney(deposit.principal) }}</span>
            <span class="rate-badge">{{ deposit.interestRate }}%</span>
            <span>{{ formatDate(deposit.maturityDate) }} 到期</span>
          </div>
        </div>
        <div class="alert-item-right">
          <div class="alert-item-amount">剩余 {{ getDaysText(deposit) }}</div>
          <div class="alert-item-date" :class="getDateClass(deposit)">
            {{ getMaturityText(deposit) }}
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { Bell, ArrowRight } from '@element-plus/icons-vue'
import { getMaturingFixedDeposits } from '@/features/master-data/fixed-deposits/api/fixedDeposit'
import type { FixedDeposit } from '@/features/master-data/fixed-deposits/types/fixedDeposit'
import { formatDateTime, formatCurrency } from '@/shared/utils/formatters'

const maturingDeposits = ref<FixedDeposit[]>([])

const formatMoney = (value: number) => formatCurrency(value, 'CNY', 2)

const formatDate = (date: string) => formatDateTime(date, 'date')

const getDaysText = (deposit: FixedDeposit) => {
  if (deposit.daysToMaturity <= 0) return '0 天'
  return `${deposit.daysToMaturity} 天`
}

const getMaturityText = (deposit: FixedDeposit) => {
  if (deposit.daysToMaturity <= 0) return '今日到期'
  if (deposit.daysToMaturity <= 7) return `${deposit.daysToMaturity} 天后到期`
  return `${formatDate(deposit.maturityDate)} 到期`
}

const getDateClass = (deposit: FixedDeposit) => {
  if (deposit.daysToMaturity <= 0) return 'text-danger'
  if (deposit.daysToMaturity <= 7) return 'text-warning'
  return 'text-normal'
}

const loadMaturingDeposits = async () => {
  try {
    const response = await getMaturingFixedDeposits(30)
    maturingDeposits.value = response.data.data
  } catch (error) {
    console.error('加载到期提醒数据失败:', error)
  }
}

onMounted(() => {
  loadMaturingDeposits()
})
</script>

<style scoped>
.maturity-alert-card {
  background: var(--bg-card);
  border-radius: 12px;
  box-shadow: 0 1px 3px rgba(0, 0, 0, 0.04), 0 1px 2px rgba(0, 0, 0, 0.06);
  overflow: hidden;
  border-left: 4px solid var(--color-warning);
}

.alert-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 16px 20px;
  border-bottom: 1px solid var(--border-light);
}

.alert-title {
  display: flex;
  align-items: center;
  gap: 8px;
  font-size: 15px;
  font-weight: 600;
  color: var(--text-primary);
}

.alert-body {
  padding: 4px 0;
}

.alert-item {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 12px 20px;
  transition: background-color 0.2s;
}

.alert-item:hover {
  background-color: var(--bg-page);
}

.alert-item + .alert-item {
  border-top: 1px solid var(--border-light);
}

.alert-item-left {
  flex: 1;
  min-width: 0;
}

.alert-item-name {
  font-size: 14px;
  font-weight: 500;
  color: var(--text-primary);
  margin-bottom: 4px;
}

.alert-item-info {
  display: flex;
  align-items: center;
  gap: 8px;
  font-size: 12px;
  color: var(--text-placeholder);
}

.rate-badge {
  background: var(--primary-surface);
  color: var(--color-primary-light-1);
  padding: 1px 6px;
  border-radius: 4px;
  font-weight: 500;
}

.alert-item-right {
  text-align: right;
  flex-shrink: 0;
  margin-left: 16px;
}

.alert-item-amount {
  font-size: 14px;
  font-weight: 600;
  color: var(--text-primary);
  margin-bottom: 4px;
}

.alert-item-date {
  font-size: 12px;
}

.text-danger {
  color: var(--color-danger);
  font-weight: 600;
}

.text-warning {
  color: var(--color-warning);
  font-weight: 600;
}

.text-normal {
  color: var(--text-placeholder);
}
</style>
