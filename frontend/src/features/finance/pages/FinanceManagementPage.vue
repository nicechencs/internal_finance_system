<template>
  <div class="finance-management">
    <!-- 页面头部 -->
    <div class="page-header">
      <div>
        <h2 class="page-title">应收应付管理</h2>
        <p class="page-desc">统一管理应收账款和应付账款</p>
      </div>
    </div>

    <!-- 统计卡片 -->
    <el-row :gutter="24" class="stat-cards">
      <el-col :xs="24" :sm="12" :md="6">
        <StatCard
          :icon="TrendCharts"
          :value="safeFormatCurrency(receivableSummary.totalReceivable)"
          label="总应收"
          :count="`未收: ${safeFormatCurrency(receivableSummary.totalRemaining)}`"
          theme="income"
        />
      </el-col>
      <el-col :xs="24" :sm="12" :md="6">
        <StatCard
          :icon="Coin"
          :value="safeFormatCurrency(receivableSummary.totalReceived)"
          label="已收款"
          :count="`${receivableSummary.settledCount} 笔已结清`"
          theme="info"
        />
      </el-col>
      <el-col :xs="24" :sm="12" :md="6">
        <StatCard
          :icon="Minus"
          :value="safeFormatCurrency(payableSummary.totalPayable)"
          label="总应付"
          :count="`未付: ${safeFormatCurrency(payableSummary.totalRemaining)}`"
          theme="expense"
        />
      </el-col>
      <el-col :xs="24" :sm="12" :md="6">
        <StatCard
          :icon="Wallet"
          :value="safeFormatCurrency(payableSummary.totalPaid)"
          label="已付款"
          :count="`${payableSummary.settledCount} 笔已结清`"
          theme="balance"
        />
      </el-col>
    </el-row>

    <!-- 次要统计指标 -->
    <el-row :gutter="24" class="secondary-stat-cards">
      <el-col :xs="24" :sm="8" :md="8">
        <StatCard
          :icon="Warning"
          :value="String(receivableSummary.overdueCount + payableSummary.overdueCount)"
          label="逾期笔数"
          count="风险提醒"
          theme="expense"
        />
      </el-col>
      <el-col :xs="24" :sm="8" :md="8">
        <StatCard
          :icon="Clock"
          :value="String(receivableSummary.pendingCount + payableSummary.pendingCount)"
          label="待处理"
          count="待跟进记录"
          theme="transfer"
        />
      </el-col>
      <el-col :xs="24" :sm="8" :md="8">
        <StatCard
          :icon="CircleCheck"
          :value="String(receivableSummary.settledCount + payableSummary.settledCount)"
          label="已结清"
          count="完成处理"
          theme="income"
        />
      </el-col>
    </el-row>

    <!-- 图表区域 -->
    <el-row :gutter="24" class="chart-area">
      <el-col :xs="24" :lg="24">
        <div class="chart-card">
          <div class="chart-card-header">
            <span class="chart-title">应收应付趋势</span>
            <el-date-picker
              v-model="trendDateRange"
              type="monthrange"
              range-separator="至"
              start-placeholder="开始月份"
              end-placeholder="结束月份"
              size="small"
              style="width: 220px"
              :clearable="false"
              @change="loadTrendData"
            />
          </div>
          <v-chart v-if="hasTrendData" :option="trendChartOption" :autoresize="true" class="chart" />
          <div v-else class="chart-empty-state">
            <el-empty description="暂无趋势数据" :image-size="72" />
          </div>
        </div>
      </el-col>
    </el-row>

    <!-- 统一筛选条件 -->
    <div class="filter-section-top">
      <el-form :inline="true" :model="filters" class="filter-form" @submit.prevent="handleFilter">
        <el-form-item label="项目">
          <SearchableSelect
            v-model="filters.projectId"
            :options="projectStore.items"
            entity-name="项目"
            width="220px"
          />
        </el-form-item>
        <el-form-item :label="activeTab === 'receivable' ? '客户' : '供应商'">
          <SearchableSelect
            v-if="activeTab === 'receivable'"
            v-model="filters.customerId"
            :options="customerStore.items"
            entity-name="客户"
            width="220px"
          />
          <SearchableSelect
            v-else
            v-model="filters.supplierId"
            :options="supplierStore.items"
            entity-name="供应商"
            width="220px"
          />
        </el-form-item>
        <el-form-item label="状态">
          <el-select v-model="filters.status" placeholder="全部" clearable style="width: 150px">
            <el-option :label="getStatusLabel('pending')" value="pending" />
            <el-option :label="getStatusLabel('partial')" value="partial" />
            <el-option label="已结清" value="settled" />
          </el-select>
        </el-form-item>
        <el-form-item label="到期日期">
          <el-date-picker
            v-model="filters.dueDateRange"
            type="daterange"
            range-separator="至"
            start-placeholder="开始日期"
            end-placeholder="结束日期"
            :shortcuts="dateRangeShortcuts"
          />
        </el-form-item>
        <el-form-item class="search-buttons">
          <el-button type="primary" native-type="submit">查询</el-button>
          <el-button @click="handleReset">重置</el-button>
          <el-button v-if="activeTab === 'receivable'" type="primary" @click="handleCreateReceivable">新增应收</el-button>
          <el-button v-if="activeTab === 'payable'" type="primary" @click="handleCreatePayable">新增应付</el-button>
        </el-form-item>
      </el-form>
    </div>

    <!-- Tab 切换应收/应付 -->
    <div class="table-section">
      <el-tabs v-model="activeTab" @tab-change="handleTabChange">
        <el-tab-pane label="应收账款" name="receivable" />
        <el-tab-pane label="应付账款" name="payable" />
      </el-tabs>

      <div v-show="activeTab === 'receivable'">
        <ReceivableList
          ref="receivableListRef"
          :embedded="true"
          :external-filters="receivableFilters"
          :trigger-reload="filterTrigger"
        />
      </div>
      <div v-show="activeTab === 'payable'">
        <PayableList
          ref="payableListRef"
          :embedded="true"
          :external-filters="payableFilters"
          :trigger-reload="filterTrigger"
        />
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted, reactive, watch } from 'vue'
import { TrendCharts, Coin, Minus, Wallet, Warning, Clock, CircleCheck } from '@element-plus/icons-vue'
import VChart from 'vue-echarts'
import { use } from 'echarts/core'
import { CanvasRenderer } from 'echarts/renderers'
import { LineChart } from 'echarts/charts'
import {
  TitleComponent,
  TooltipComponent,
  LegendComponent,
  GridComponent
} from 'echarts/components'
import { getReceivableSummary, getReceivableTrend } from '@/features/finance/api/receivable'
import { getPayableSummary, getPayableTrend } from '@/features/finance/api/payable'
import { dateRangeShortcuts } from '@/shared/utils/dateShortcuts'
import { useDebounce } from '@/shared/composables/useDebounce'
import { useProjectStore } from '@/features/master-data/projects/stores/project'
import { useCustomerStore } from '@/features/master-data/customers/stores/customer'
import { useSupplierStore } from '@/features/master-data/suppliers/stores/supplier'
import type { ReceivableSummary, ReceivableTrend } from '@/features/finance/types/receivable'
import type { PayableSummary, PayableTrend } from '@/features/finance/types/payable'
import StatCard from '@/shared/ui/StatCard.vue'
import SearchableSelect from '@/shared/ui/SearchableSelect.vue'
import ReceivableList from '@/features/finance/pages/ReceivableListPage.vue'
import PayableList from '@/features/finance/pages/PayableListPage.vue'
import { CHART_COLORS, CHART_AXIS, CHART_TOOLTIP, CHART_GRADIENT } from '@/shared/constants/colors'
import { formatRMB } from '@/shared/utils/formatters'

// Register ECharts components
use([
  CanvasRenderer,
  LineChart,
  TitleComponent,
  TooltipComponent,
  LegendComponent,
  GridComponent
])

// Stores
const projectStore = useProjectStore()
const customerStore = useCustomerStore()
const supplierStore = useSupplierStore()

// Data
const activeTab = ref('receivable')
const receivableListRef = ref()
const payableListRef = ref()
const filterTrigger = ref(0)

// 筛选条件
const filters = reactive({
  projectId: null as number | null,
  customerId: null as number | null,
  supplierId: null as number | null,
  status: '',
  dueDateRange: null as [Date, Date] | null
})

// 计算属性：根据当前 Tab 生成对应的筛选条件
const receivableFilters = computed(() => ({
  projectId: filters.projectId,
  customerId: filters.customerId,
  status: filters.status,
  dueDateRange: filters.dueDateRange
}))

const payableFilters = computed(() => ({
  projectId: filters.projectId,
  supplierId: filters.supplierId,
  status: filters.status,
  dueDateRange: filters.dueDateRange
}))

// 切换 Tab 时清空客户/供应商筛选
watch(activeTab, (newTab, oldTab) => {
  if (newTab !== oldTab) {
    if (newTab === 'receivable') {
      filters.supplierId = null
    } else if (newTab === 'payable') {
      filters.customerId = null
    }
  }
})

const defaultReceivableSummary: ReceivableSummary = {
  totalReceivable: 0,
  totalReceived: 0,
  totalRemaining: 0,
  pendingCount: 0,
  partialCount: 0,
  settledCount: 0,
  overdueCount: 0
}

const defaultPayableSummary: PayableSummary = {
  totalPayable: 0,
  totalPaid: 0,
  totalRemaining: 0,
  pendingCount: 0,
  partialCount: 0,
  settledCount: 0,
  overdueCount: 0
}

const receivableSummary = ref<ReceivableSummary>({ ...defaultReceivableSummary })

const payableSummary = ref<PayableSummary>({ ...defaultPayableSummary })

// 趋势图时间范围（默认最近6个月）
const initTrendDateRange = (): [Date, Date] => {
  const end = new Date()
  const start = new Date(end.getFullYear(), end.getMonth() - 5, 1)
  return [start, end]
}
const trendDateRange = ref<[Date, Date]>(initTrendDateRange())

const trendData = ref<{ months: string[]; receivable: number[]; payable: number[] }>({
  months: [],
  receivable: [],
  payable: []
})

// Format helpers
const toSafeNumber = (value: unknown) => {
  const numericValue = Number(value)
  return Number.isFinite(numericValue) ? numericValue : 0
}

const normalizeReceivableSummary = (summary?: Partial<ReceivableSummary> | null): ReceivableSummary => ({
  ...defaultReceivableSummary,
  totalReceivable: toSafeNumber(summary?.totalReceivable),
  totalReceived: toSafeNumber(summary?.totalReceived),
  totalRemaining: toSafeNumber(summary?.totalRemaining),
  pendingCount: toSafeNumber(summary?.pendingCount),
  partialCount: toSafeNumber(summary?.partialCount),
  settledCount: toSafeNumber(summary?.settledCount),
  overdueCount: toSafeNumber(summary?.overdueCount)
})

const normalizePayableSummary = (summary?: Partial<PayableSummary> | null): PayableSummary => ({
  ...defaultPayableSummary,
  totalPayable: toSafeNumber(summary?.totalPayable),
  totalPaid: toSafeNumber(summary?.totalPaid),
  totalRemaining: toSafeNumber(summary?.totalRemaining),
  pendingCount: toSafeNumber(summary?.pendingCount),
  partialCount: toSafeNumber(summary?.partialCount),
  settledCount: toSafeNumber(summary?.settledCount),
  overdueCount: toSafeNumber(summary?.overdueCount)
})

const safeFormatCurrency = (value: number | null | undefined) => formatRMB(toSafeNumber(value))
const hasAnyNonZeroValue = (values: number[]) => values.some(value => toSafeNumber(value) !== 0)

const hasTrendData = computed(() =>
  trendData.value.months.length > 0
  && (hasAnyNonZeroValue(trendData.value.receivable) || hasAnyNonZeroValue(trendData.value.payable))
)

// Chart options
const chartColors = {
  receivable: CHART_COLORS.receivable,
  payable: CHART_COLORS.payable,
}

const trendChartOption = computed(() => ({
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
    data: ['应收', '应付'],
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
    data: trendData.value.months,
    axisLabel: {
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
      name: '应收',
      type: 'line',
      data: trendData.value.receivable,
      itemStyle: { color: chartColors.receivable },
      lineStyle: { width: 2.5 },
      symbol: 'circle',
      symbolSize: 6,
      smooth: true,
      areaStyle: {
        color: {
          type: 'linear',
          x: 0, y: 0, x2: 0, y2: 1,
          colorStops: [
            { offset: 0, color: CHART_GRADIENT.income.start },
            { offset: 1, color: CHART_GRADIENT.income.end }
          ]
        }
      }
    },
    {
      name: '应付',
      type: 'line',
      data: trendData.value.payable,
      itemStyle: { color: chartColors.payable },
      lineStyle: { width: 2.5 },
      symbol: 'circle',
      symbolSize: 6,
      smooth: true,
      areaStyle: {
        color: {
          type: 'linear',
          x: 0, y: 0, x2: 0, y2: 1,
          colorStops: [
            { offset: 0, color: CHART_GRADIENT.expense.start },
            { offset: 1, color: CHART_GRADIENT.expense.end }
          ]
        }
      }
    }
  ]
}))

// Load data
const loadSummaryData = async () => {
  const results = await Promise.allSettled([
    getReceivableSummary(),
    getPayableSummary()
  ])

  if (results[0].status === 'fulfilled') receivableSummary.value = normalizeReceivableSummary(results[0].value.data.data)
  if (results[1].status === 'fulfilled') payableSummary.value = normalizePayableSummary(results[1].value.data.data)

  const failed = results.filter(r => r.status === 'rejected')
  if (failed.length > 0) {
    console.error(`统计数据加载：${results.length - failed.length}/${results.length} 成功`)
  }
}

const formatLocalDate = (d: Date): string => {
  const y = d.getFullYear()
  const m = String(d.getMonth() + 1).padStart(2, '0')
  const day = String(d.getDate()).padStart(2, '0')
  return `${y}-${m}-${day}`
}

const loadTrendData = async () => {
  if (!trendDateRange.value) return
  const [start, end] = trendDateRange.value
  const params = {
    startDate: formatLocalDate(start),
    endDate: formatLocalDate(end)
  }

  const results = await Promise.allSettled([
    getReceivableTrend(params),
    getPayableTrend(params)
  ])

  if (results[0].status === 'fulfilled' && results[1].status === 'fulfilled') {
    const rTrend: ReceivableTrend = results[0].value.data.data
    const pTrend: PayableTrend = results[1].value.data.data

    // 合并月份轴（以应收为基准，两者后端返回范围相同）
    const months = rTrend.months
    const receivableMap = Object.fromEntries(rTrend.months.map((m, i) => [m, rTrend.amounts[i]]))
    const payableMap = Object.fromEntries(pTrend.months.map((m, i) => [m, pTrend.amounts[i]]))

    trendData.value = {
      months,
      receivable: months.map(m => receivableMap[m] ?? 0),
      payable: months.map(m => payableMap[m] ?? 0)
    }
  } else {
    console.error('趋势数据加载失败')
  }
}

const loadProjects = async () => {
  await projectStore.loadOptions()
}

const loadCustomers = async () => {
  await customerStore.loadOptions()
}

const loadSuppliers = async () => {
  await supplierStore.loadOptions()
}

const handleFilterImmediate = () => {
  filterTrigger.value++
}

// 使用防抖包装 handleFilter
const handleFilter = useDebounce(handleFilterImmediate, 300)

const handleReset = () => {
  filters.projectId = null
  filters.customerId = null
  filters.supplierId = null
  filters.status = ''
  filters.dueDateRange = null
  handleFilterImmediate()  // 直接调用，不走防抖
}

const handleTabChange = () => {
  filterTrigger.value++
}

const handleCreateReceivable = () => {
  receivableListRef.value?.handleCreate()
}

const handleCreatePayable = () => {
  payableListRef.value?.handleCreate()
}

const getStatusLabel = (status: string) => {
  if (activeTab.value === 'receivable') {
    return status === 'pending' ? '待收款' : status === 'partial' ? '部分收款' : '已结清'
  }
  return status === 'pending' ? '待付款' : status === 'partial' ? '部分付款' : '已结清'
}

onMounted(() => {
  loadSummaryData()
  loadTrendData()
  loadProjects()
  loadCustomers()
  loadSuppliers()
})
</script>

<style scoped>
.finance-management {
  padding: 0;
}

.page-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 20px;
}

.page-title {
  font-size: 20px;
  font-weight: 600;
  color: var(--text-primary);
  margin: 0;
}

.page-desc {
  font-size: 13px;
  color: var(--text-placeholder);
  margin: 4px 0 0 0;
}

/* 统计卡片 */
.stat-cards {
  margin-bottom: 24px;
}


/* 次要统计指标 */
.secondary-stat-cards {
  margin-bottom: 24px;
}

/* 图表区域 */
.chart-area {
  margin-bottom: 24px;
}

.chart-card {
  background: var(--bg-card);
  border-radius: 12px;
  box-shadow: var(--shadow-card);
  padding: 24px;
  transition: box-shadow 0.25s ease;
  margin-bottom: 12px;
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
  height: 350px;
}

.chart-empty-state {
  min-height: 350px;
  display: flex;
  align-items: center;
  justify-content: center;
}

/* 表格区域 */
.table-section {
  background: var(--bg-card);
  border-radius: 12px;
  padding: 20px;
  box-shadow: var(--shadow-card);
}

.table-section :deep(.el-tabs__header) {
  margin-bottom: 20px;
}

.table-section :deep(.el-tabs__item) {
  font-size: 15px;
  font-weight: 500;
}

.table-section :deep(.el-tabs__nav-wrap::after) {
  height: 1px;
  background-color: var(--border-light);
}

/* 筛选区域 */
.filter-section-top {
  background: var(--bg-page);
  border-radius: 8px;
  padding: 16px 16px 0 16px;
  margin-bottom: 20px;
}

.filter-form {
  display: flex;
  flex-wrap: wrap;
  gap: 0;
}

.search-buttons {
  margin-left: auto !important;
}

.filter-section-top :deep(.el-form-item__label) {
  color: var(--text-secondary);
  font-size: 13px;
}

.filter-section-top :deep(.el-input__wrapper),
.filter-section-top :deep(.el-select .el-input__wrapper) {
  border-radius: 8px;
}
</style>
