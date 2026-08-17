<template>
  <div>
    <div class="records-summary mb-4">
      <el-descriptions :column="4" border size="small">
        <el-descriptions-item label="应收总额">
          <span class="amount-primary">{{ formatCurrency(summary.totalAmount) }}</span>
        </el-descriptions-item>
        <el-descriptions-item label="已收金额">
          <span class="amount-success">{{ formatCurrency(summary.receivedAmount) }}</span>
        </el-descriptions-item>
        <el-descriptions-item label="未收金额">
          <span class="amount-warning">{{ formatCurrency(summary.remainingAmount) }}</span>
        </el-descriptions-item>
        <el-descriptions-item label="逾期">
          <span :class="summary.overdueCount > 0 ? 'amount-danger' : ''">
            {{ summary.overdueCount }} 笔
          </span>
        </el-descriptions-item>
      </el-descriptions>
    </div>
    <el-table :data="records" v-loading="loading" border>
      <el-table-column prop="description" label="描述" min-width="150">
        <template #default="{ row }">{{ row.description || '-' }}</template>
      </el-table-column>
      <el-table-column prop="totalAmount" label="应收金额" width="140" align="right">
        <template #default="{ row }">{{ formatCurrency(row.totalAmount) }}</template>
      </el-table-column>
      <el-table-column prop="receivedAmount" label="已收金额" width="140" align="right">
        <template #default="{ row }">
          <span class="amount-success">{{ formatCurrency(row.receivedAmount) }}</span>
        </template>
      </el-table-column>
      <el-table-column prop="remainingAmount" label="未收金额" width="140" align="right">
        <template #default="{ row }">
          <span class="amount-warning">{{ formatCurrency(row.remainingAmount) }}</span>
        </template>
      </el-table-column>
      <el-table-column prop="dueDate" label="到期日" width="120">
        <template #default="{ row }">{{ row.dueDate ? formatDate(row.dueDate) : '-' }}</template>
      </el-table-column>
      <el-table-column prop="status" label="状态" width="100">
        <template #default="{ row }">
          <el-tag :type="getStatusType(row.status)" size="small">
            {{ getStatusText(row.status) }}
          </el-tag>
        </template>
      </el-table-column>
      <el-table-column prop="settledAt" label="结算时间" width="120">
        <template #default="{ row }">{{ row.settledAt ? formatDate(row.settledAt) : '-' }}</template>
      </el-table-column>
    </el-table>
  </div>
</template>

<script setup lang="ts">
import { computed } from 'vue'
import type { Receivable } from '@/features/finance/types/receivable'
import { formatDateTime, formatRMB } from '@/shared/utils/formatters'

const props = defineProps<{
  records: Receivable[]
  loading: boolean
}>()

const formatCurrency = (amount: number) => formatRMB(amount)
const formatDate = (date: string) => formatDateTime(date, 'date')

const summary = computed(() => {
  const now = new Date()
  const today = `${now.getUTCFullYear()}-${String(now.getUTCMonth() + 1).padStart(2, '0')}-${String(now.getUTCDate()).padStart(2, '0')}`
  return {
    totalAmount: props.records.reduce((sum, r) => sum + r.totalAmount, 0),
    receivedAmount: props.records.reduce((sum, r) => sum + r.receivedAmount, 0),
    remainingAmount: props.records.reduce((sum, r) => sum + r.remainingAmount, 0),
    overdueCount: props.records.filter(r => r.status !== 'settled' && r.dueDate && r.dueDate < today).length
  }
})

const getStatusType = (status: string) => {
  const map: Record<string, string> = { pending: 'warning', partial: 'info', settled: 'success' }
  return map[status] || 'info'
}

const getStatusText = (status: string) => {
  const map: Record<string, string> = { pending: '待收款', partial: '部分收款', settled: '已结清' }
  return map[status] || status
}
</script>

<style scoped>
.records-summary { margin-bottom: 16px; }
.amount-primary { color: var(--color-primary); font-weight: 600; }
.amount-success { color: var(--color-success); font-weight: 600; }
.amount-warning { color: var(--color-warning); font-weight: 600; }
.amount-danger { color: var(--color-danger); font-weight: 600; }
.mb-4 { margin-bottom: 16px; }
</style>
