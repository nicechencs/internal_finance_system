<template>
  <div class="dashboard" :class="{ 'is-mobile': isMobile }" v-loading="dashboardLoading">
    <!-- 页面头部 -->
    <div class="dashboard-header">
      <div class="header-left">
        <h1 class="page-title">工作台</h1>
        <p class="welcome-text">{{ greeting }}，祝你开心每一天！</p>
      </div>
      <div class="header-right">
        <span class="header-date">{{ currentDate }}</span>
        <div class="header-actions">
          <el-button type="primary" @click="$router.push('/transactions')">
            <el-icon><Plus /></el-icon>
            新增交易
          </el-button>
          <el-button v-if="!isMobile" @click="$router.push('/import')">
            <el-icon><Upload /></el-icon>
            导入流水
          </el-button>
          <el-dropdown v-else trigger="click" @command="$router.push($event)">
            <el-button>
              更多
              <el-icon class="el-icon--right"><ArrowRight /></el-icon>
            </el-button>
            <template #dropdown>
              <el-dropdown-menu>
                <el-dropdown-item command="/import">导入流水</el-dropdown-item>
              </el-dropdown-menu>
            </template>
          </el-dropdown>
        </div>
      </div>
    </div>

    <!-- 统计卡片 -->
    <el-row :gutter="isMobile ? 12 : 24" class="stat-cards">
      <el-col :xs="12" :sm="12" :md="6">
        <StatCard
          :icon="TrendCharts"
          :value="formatMoney(summary.totalIncome)"
          label="总收入"
          theme="income"
        />
      </el-col>
      <el-col :xs="12" :sm="12" :md="6">
        <StatCard
          :icon="Minus"
          :value="formatMoney(summary.totalExpense)"
          label="总支出"
          theme="expense"
        />
      </el-col>
      <el-col :xs="12" :sm="12" :md="6">
        <StatCard
          :icon="Coin"
          :value="formatMoney(summary.netProfit)"
          label="净利润"
          theme="profit"
        />
      </el-col>
      <el-col :xs="12" :sm="12" :md="6">
        <StatCard
          :icon="Wallet"
          :value="formatMoney(summary.totalBalance)"
          label="总余额"
          theme="balance"
        />
      </el-col>
    </el-row>

    <!-- 次要统计指标 -->
    <el-row :gutter="isMobile ? 12 : 24" class="secondary-stat-cards">
      <el-col :xs="12" :sm="8" :md="8">
        <StatCard
          :icon="CreditCard"
          :value="String(summary.accountCount)"
          label="账户数量"
          count="已建账户"
          theme="info"
        />
      </el-col>
      <el-col :xs="12" :sm="8" :md="8">
        <StatCard
          :icon="List"
          :value="String(summary.transactionCount)"
          label="交易笔数"
          count="累计流水"
          theme="transfer"
        />
      </el-col>
      <el-col :xs="12" :sm="8" :md="8">
        <StatCard
          :icon="Folder"
          :value="String(summary.projectCount)"
          label="项目数量"
          count="项目档案"
          theme="income"
        />
      </el-col>
    </el-row>

    <!-- 定期存款到期提醒 -->
    <div class="maturity-alert-section">
      <MaturityAlert />
    </div>

    <!-- 图表区域 -->
    <el-row :gutter="isMobile ? 12 : 24" class="chart-area">
      <el-col :xs="24" :lg="16">
        <div class="chart-card">
          <div class="chart-card-header">
            <span class="chart-title">月度收支趋势</span>
          </div>
          <v-chart :option="monthlyChartOption" :autoresize="true" class="chart monthly-chart" />
        </div>
      </el-col>
      <el-col :xs="24" :lg="8">
        <div class="chart-card pie-card-wrapper">
          <div class="chart-card-header">
            <span class="chart-title">支出分类占比</span>
          </div>
          <v-chart :option="expensePieOption" :autoresize="true" class="chart pie-chart" />
        </div>
        <div class="chart-card pie-card-wrapper">
          <div class="chart-card-header">
            <span class="chart-title">收入分类占比</span>
          </div>
          <v-chart :option="incomePieOption" :autoresize="true" class="chart pie-chart" />
        </div>
      </el-col>
    </el-row>

    <!-- 近期交易表格 -->
    <div class="chart-card recent-transactions">
      <div class="chart-card-header">
        <span class="chart-title">近期交易</span>
        <el-button type="primary" link @click="$router.push('/transactions')">
          查看全部
          <el-icon class="el-icon--right"><ArrowRight /></el-icon>
        </el-button>
      </div>
      <ResponsiveList :items="recentTransactions">
        <template #table>
      <el-table :data="recentTransactions" class="modern-table resizable-table clickable-rows" style="width: 100%" border allow-drag-last-column @header-dragend="handleHeaderDragend" @row-click="handleViewTransaction">
        <template #empty>暂无交易记录</template>
        <el-table-column prop="transactionDate" label="日期" :width="getColumnWidth('transactionDate', TABLE_COLUMN_WIDTH.date)">
          <template #default="{ row }">
            {{ formatDateTime(row.transactionDate, 'date') }}
          </template>
        </el-table-column>
        <el-table-column prop="transactionType" label="类型" :width="getColumnWidth('transactionType', TABLE_COLUMN_WIDTH.type)">
          <template #default="{ row }">
            <TransactionTypeTag :transaction-type="row.transactionType" />
          </template>
        </el-table-column>
        <el-table-column prop="amount" label="金额" :width="getColumnWidth('amount', TABLE_COLUMN_WIDTH.amount)" align="right">
          <template #default="{ row }">
            <span :class="row.transactionType === 'Income' ? 'text-income' : 'text-expense'">
              {{ row.transactionType === 'Income' ? '+' : '-' }}{{ formatMoney(row.amount) }}
            </span>
          </template>
        </el-table-column>
        <el-table-column prop="accountName" label="账户" :width="getColumnWidth('accountName', TABLE_COLUMN_WIDTH.account)" />
        <el-table-column prop="categoryName" label="分类" :width="getColumnWidth('categoryName', TABLE_COLUMN_WIDTH.category)">
          <template #default="{ row }">
            {{ row.categoryName || '-' }}
          </template>
        </el-table-column>
        <el-table-column prop="counterpartyName" label="对方" :min-width="getColumnMinWidth('counterpartyName', TABLE_COLUMN_WIDTH.company)">
          <template #default="{ row }">
            {{ row.counterpartyName || '-' }}
          </template>
        </el-table-column>
        <el-table-column prop="description" label="描述" :min-width="getColumnMinWidth('description', TABLE_COLUMN_WIDTH.description)" show-overflow-tooltip>
          <template #default="{ row }">
            {{ row.description || '-' }}
          </template>
        </el-table-column>
        <el-table-column label="操作" width="80" fixed="right">
          <template #default="{ row }">
            <el-button link type="primary" size="small" @click.stop="handleViewTransaction(row)">详情</el-button>
          </template>
        </el-table-column>
      </el-table>
        </template>
        <template #card="{ item }">
          <MobileListCard
            :title="item.counterpartyName || item.description || '未命名交易'"
            :amount="item.amount"
            :amount-type="getAmountTone(item.transactionType)"
            @click="handleViewTransaction(item)"
          >
            <template #tag>
              <TransactionTypeTag :transaction-type="item.transactionType" />
            </template>
            <template #meta>
              {{ joinMeta(formatDateTime(item.transactionDate, 'date'), item.accountName, item.categoryName) }}
            </template>
          </MobileListCard>
        </template>
      </ResponsiveList>
    </div>
    <TransactionDetail
      v-model:visible="detailVisible"
      :transaction-id="currentTransactionId"
    />
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import {
  TrendCharts, Minus, Coin, Wallet, Plus, Upload,
  CreditCard, List, Folder, ArrowRight
} from '@element-plus/icons-vue'
import VChart from 'vue-echarts'
import { use } from 'echarts/core'
import { CanvasRenderer } from 'echarts/renderers'
import { BarChart, LineChart, PieChart } from 'echarts/charts'
import {
  TitleComponent,
  TooltipComponent,
  LegendComponent,
  GridComponent
} from 'echarts/components'
import {
  getDashboardSummary,
  getMonthlyStats,
  getExpenseByCategory,
  getIncomeByCategory,
  getRecentTransactions
} from '@/features/dashboard/api/dashboard'
import type {
  DashboardSummary,
  MonthlyStats,
  CategoryStats,
  RecentTransaction
} from '@/features/dashboard/types/dashboard'
import dayjs from 'dayjs'
import TransactionDetail from '@/features/transactions/pages/TransactionDetailPage.vue'
import TransactionTypeTag from '@/shared/ui/TransactionTypeTag.vue'
import MaturityAlert from '@/features/system/components/MaturityAlert.vue'
import StatCard from '@/shared/ui/StatCard.vue'
import ResponsiveList from '@/shared/ui/ResponsiveList.vue'
import MobileListCard from '@/shared/ui/MobileListCard.vue'
import { useBreakpoint } from '@/shared/composables/useBreakpoint'
import { TABLE_COLUMN_WIDTH } from '@/shared/constants/table'
import { useResizableTableColumns } from '@/shared/composables/useResizableTableColumns'
import { CHART_COLORS, CHART_PALETTE, CHART_AXIS, CHART_TOOLTIP, CHART_GRADIENT } from '@/shared/constants/colors'
import { formatMoney, formatDateTime, getAmountTone } from '@/shared/utils/formatters'
import { joinMeta } from '@/shared/utils/recordDisplay'

// Register ECharts components
use([
  CanvasRenderer,
  BarChart,
  LineChart,
  PieChart,
  TitleComponent,
  TooltipComponent,
  LegendComponent,
  GridComponent
])

// Data
const { isMobile } = useBreakpoint()
const { getColumnWidth, getColumnMinWidth, handleHeaderDragend } = useResizableTableColumns('dashboard-recent-transactions')
const summary = ref<DashboardSummary>({
  totalIncome: 0,
  totalExpense: 0,
  netProfit: 0,
  totalBalance: 0,
  accountCount: 0,
  transactionCount: 0,
  projectCount: 0
})
const monthlyData = ref<MonthlyStats[]>([])
const expenseByCategory = ref<CategoryStats[]>([])
const incomeByCategory = ref<CategoryStats[]>([])
const recentTransactions = ref<RecentTransaction[]>([])
const detailVisible = ref(false)
const currentTransactionId = ref(0)

const handleViewTransaction = (row: RecentTransaction) => {
  currentTransactionId.value = row.id
  detailVisible.value = true
}

// 动态问候语
const greeting = computed(() => {
  const hour = new Date().getHours()
  if (hour < 6) return '凌晨好'
  if (hour < 9) return '早上好'
  if (hour < 12) return '上午好'
  if (hour < 14) return '中午好'
  if (hour < 18) return '下午好'
  if (hour < 22) return '晚上好'
  return '夜深了'
})

// 当前日期
const currentDate = computed(() => {
  return dayjs().format('YYYY年MM月DD日 dddd')
})

// Loading state
const dashboardLoading = ref(true)

// 设计系统颜色
const chartColors = {
  income: CHART_COLORS.income,
  expense: CHART_COLORS.expense,
  profit: CHART_COLORS.profit,
  palette: [...CHART_PALETTE],
}

// Chart options
const monthlyChartOption = computed(() => ({
  tooltip: {
    trigger: 'axis',
    axisPointer: { type: 'cross' },
    backgroundColor: CHART_TOOLTIP.bg,
    borderColor: CHART_TOOLTIP.border,
    borderWidth: 1,
    textStyle: { color: CHART_TOOLTIP.text },
    extraCssText: `box-shadow: ${CHART_TOOLTIP.shadow}`,
    padding: [12, 16]
  },
  legend: {
    data: ['收入', '支出', '净利润'],
    bottom: 0,
    itemGap: 24,
    textStyle: { color: CHART_AXIS.axisLabel, fontSize: 12 },
    icon: 'roundRect',
    itemWidth: 12,
    itemHeight: 4
  },
  grid: {
    left: '3%',
    right: '4%',
    bottom: '14%',
    top: '6%',
    containLabel: true
  },
  xAxis: {
    type: 'category',
    data: monthlyData.value.map(m => m.month),
    axisLabel: {
      rotate: 30,
      fontSize: 11,
      color: CHART_AXIS.axisLabel
    },
    axisLine: { lineStyle: { color: CHART_AXIS.axisLine } },
    axisTick: { show: false }
  },
  yAxis: {
    type: 'value',
    axisLabel: {
      color: CHART_AXIS.axisLabel,
      fontSize: 11,
      formatter: (value: number) => {
        if (Math.abs(value) >= 10000) {
          return (value / 10000).toFixed(1) + '万'
        }
        return value.toString()
      }
    },
    splitLine: { lineStyle: { color: CHART_AXIS.splitLine, type: 'dashed' } },
    axisLine: { show: false },
    axisTick: { show: false }
  },
  series: [
    {
      name: '收入',
      type: 'bar',
      data: monthlyData.value.map(m => m.income),
      itemStyle: {
        color: chartColors.income,
        borderRadius: [4, 4, 0, 0]
      },
      barMaxWidth: 24
    },
    {
      name: '支出',
      type: 'bar',
      data: monthlyData.value.map(m => m.expense),
      itemStyle: {
        color: chartColors.expense,
        borderRadius: [4, 4, 0, 0]
      },
      barMaxWidth: 24
    },
    {
      name: '净利润',
      type: 'line',
      data: monthlyData.value.map(m => m.net),
      itemStyle: { color: chartColors.profit },
      lineStyle: { width: 2.5 },
      symbol: 'circle',
      symbolSize: 6,
      smooth: true,
      areaStyle: {
        color: {
          type: 'linear',
          x: 0, y: 0, x2: 0, y2: 1,
          colorStops: [
            { offset: 0, color: CHART_GRADIENT.profit.start },
            { offset: 1, color: CHART_GRADIENT.profit.end }
          ]
        }
      }
    }
  ]
}))

const createPieOption = (data: { categoryName: string; amount: number }[]) => ({
  tooltip: {
    trigger: 'item',
    formatter: '{b}: {c} ({d}%)',
    backgroundColor: CHART_TOOLTIP.bg,
    borderColor: CHART_TOOLTIP.border,
    borderWidth: 1,
    textStyle: { color: CHART_TOOLTIP.text },
    extraCssText: `box-shadow: ${CHART_TOOLTIP.shadow}`,
    padding: [8, 12]
  },
  legend: {
    orient: 'vertical',
    right: '4%',
    top: 'center',
    type: 'scroll',
    textStyle: { color: CHART_AXIS.axisLabel, fontSize: 11 },
    itemWidth: 8,
    itemHeight: 8,
    icon: 'circle',
    itemGap: 10
  },
  color: chartColors.palette,
  series: [{
    type: 'pie',
    radius: ['42%', '72%'],
    center: ['32%', '50%'],
    avoidLabelOverlap: false,
    itemStyle: {
      borderRadius: 6,
      borderColor: '#FFFFFF',
      borderWidth: 2
    },
    label: { show: false },
    emphasis: {
      label: { show: true, fontSize: 13, fontWeight: '600' },
      scaleSize: 6
    },
    labelLine: { show: false },
    data: data.map(c => ({ name: c.categoryName, value: c.amount }))
  }]
})

const expensePieOption = computed(() => createPieOption(expenseByCategory.value))
const incomePieOption = computed(() => createPieOption(incomeByCategory.value))

// Load data
const loadDashboardData = async () => {
  const results = await Promise.allSettled([
    getDashboardSummary(),
    getMonthlyStats(12),
    getExpenseByCategory(),
    getIncomeByCategory(),
    getRecentTransactions(10)
  ])

  if (results[0].status === 'fulfilled') summary.value = results[0].value.data.data
  if (results[1].status === 'fulfilled') monthlyData.value = results[1].value.data.data
  if (results[2].status === 'fulfilled') expenseByCategory.value = results[2].value.data.data
  if (results[3].status === 'fulfilled') incomeByCategory.value = results[3].value.data.data
  if (results[4].status === 'fulfilled') recentTransactions.value = results[4].value.data.data

  const failed = results.filter(r => r.status === 'rejected')
  if (failed.length > 0) {
    console.error(`仪表盘数据加载：${results.length - failed.length}/${results.length} 成功`)
  }

  dashboardLoading.value = false
}

const loadDashboardDataSafe = async () => {
  try {
    await loadDashboardData()
  } catch (error) {
    console.error('仪表盘加载失败:', error)
  } finally {
    dashboardLoading.value = false
  }
}

onMounted(() => {
  loadDashboardDataSafe()
})
</script>

<style scoped>
.dashboard {
  padding: 0;
}

/* ========== 页面头部 ========== */
.dashboard-header {
  display: flex;
  justify-content: space-between;
  align-items: flex-start;
  margin-bottom: var(--spacing-xl);
}

.page-title {
  font-size: var(--font-size-page-title);
  font-weight: 700;
  color: var(--text-primary);
  margin: 0 0 4px 0;
  letter-spacing: -0.02em;
}

.welcome-text {
  font-size: 14px;
  color: var(--text-secondary);
  margin: 0;
}

.header-right {
  display: flex;
  flex-direction: column;
  align-items: flex-end;
  gap: 12px;
}

.header-date {
  font-size: 14px;
  color: var(--text-placeholder);
}

.header-actions {
  display: flex;
  gap: 10px;
}

/* ========== 统计卡片 ========== */
.stat-cards {
  margin-bottom: var(--spacing-xl);
}

/* ========== 次要统计指标 ========== */
.secondary-stat-cards {
  margin-bottom: var(--spacing-xl);
}

/* ========== 图表卡片 ========== */
.chart-area {
  margin-bottom: var(--spacing-xl);
}

/* ========== 定期存款到期提醒 ========== */
.maturity-alert-section {
  margin-bottom: var(--spacing-xl);
}

.chart-card {
  background: var(--bg-card);
  border-radius: var(--radius-xl);
  box-shadow: var(--shadow-card);
  padding: 24px;
  transition: box-shadow 0.25s ease;
}

.chart-card:hover {
  box-shadow: var(--shadow-card-hover);
}

.chart-card-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 16px;
}

.chart-title {
  font-size: 16px;
  font-weight: 600;
  color: var(--text-primary);
}

.chart {
  width: 100%;
}

.monthly-chart {
  height: var(--chart-height-lg);
}

.pie-card-wrapper {
  margin-bottom: 24px;
}

.pie-card-wrapper:last-child {
  margin-bottom: 0;
}

.pie-chart {
  height: var(--chart-height-sm);
}

/* ========== 近期交易表格 ========== */
.recent-transactions {
  margin-bottom: var(--spacing-xl);
}

.modern-table {
  --el-table-border-color: transparent;
  --el-table-header-bg-color: var(--bg-page);
  --el-table-header-text-color: var(--color-text-secondary);
  --el-table-row-hover-bg-color: var(--bg-page);
}

.modern-table :deep(.el-table__header th) {
  font-weight: 600;
  font-size: 13px;
  border-bottom: 1px solid var(--border-light);
  padding: 14px 0;
}

.modern-table :deep(.el-table__body td) {
  border-bottom: 1px solid var(--border-light);
  padding: 12px 0;
  font-size: 13px;
  color: var(--text-primary);
}

.modern-table :deep(.el-table__body tr:last-child td) {
  border-bottom: none;
}

.modern-table :deep(.el-table__inner-wrapper::before) {
  display: none;
}

.text-income {
  color: var(--color-success-dark-1);
  font-weight: 600;
}

.text-expense {
  color: var(--color-danger-dark-1);
  font-weight: 600;
}

.clickable-rows :deep(tr) {
  cursor: pointer;
}

.dashboard.is-mobile .dashboard-header {
  flex-direction: column;
  gap: var(--spacing-base);
}

.dashboard.is-mobile .header-right {
  align-items: flex-start;
  width: 100%;
}
</style>
