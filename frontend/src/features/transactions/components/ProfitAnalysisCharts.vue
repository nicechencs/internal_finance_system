<template>
  <div class="charts-container">
    <!-- 时间范围选择 -->
    <div class="chart-header">
      <h3 class="chart-title">利润分析</h3>
      <el-radio-group v-model="selectedRange" size="small" @change="onRangeChange">
        <el-radio-button :value="3">3个月</el-radio-button>
        <el-radio-button :value="6">6个月</el-radio-button>
        <el-radio-button :value="12">1年</el-radio-button>
      </el-radio-group>
    </div>

    <div v-if="loading" class="chart-loading">
      <el-icon class="is-loading"><Loading /></el-icon>
      <span>加载中...</span>
    </div>

    <div v-else-if="!hasData" class="chart-empty">
      <el-empty description="暂无分析数据" :image-size="80" />
    </div>

    <template v-else>
      <!-- 收入/支出对比 + 利润趋势 -->
      <el-row :gutter="16">
        <el-col :xs="24" :lg="14">
          <div class="chart-card">
            <div class="chart-card-title">收入/支出对比</div>
            <v-chart class="chart" :option="barChartOption" autoresize />
          </div>
        </el-col>
        <el-col :xs="24" :lg="10">
          <div class="chart-card">
            <div class="chart-card-title">费用分类占比</div>
            <v-chart
              v-if="data.expenseCategories.length > 0"
              class="chart"
              :option="pieChartOption"
              autoresize
            />
            <div v-else class="chart-empty-inner">
              <el-empty description="暂无费用数据" :image-size="60" />
            </div>
          </div>
        </el-col>
      </el-row>

      <!-- 利润趋势折线图 -->
      <div class="chart-card" style="margin-top: 16px;">
        <div class="chart-card-title">利润趋势</div>
        <v-chart class="chart" :option="lineChartOption" autoresize />
      </div>
    </template>
  </div>
</template>

<script setup lang="ts">
import { ref, computed } from 'vue'
import { use } from 'echarts/core'
import { CanvasRenderer } from 'echarts/renderers'
import { BarChart, LineChart, PieChart } from 'echarts/charts'
import {
  TitleComponent,
  TooltipComponent,
  GridComponent,
  LegendComponent
} from 'echarts/components'
import VChart from 'vue-echarts'
import { Loading } from '@element-plus/icons-vue'
import type { ProfitAnalysisResponse } from '@/features/master-data/projects/types/project'
import { CHART_COLORS, CHART_PALETTE, CHART_AXIS, CHART_TOOLTIP, CHART_GRADIENT } from '@/shared/constants/colors'

use([
  CanvasRenderer,
  BarChart,
  LineChart,
  PieChart,
  TitleComponent,
  TooltipComponent,
  GridComponent,
  LegendComponent
])

const props = defineProps<{
  data: ProfitAnalysisResponse
  loading: boolean
}>()

const emit = defineEmits<{
  rangeChange: [months: number]
}>()

const selectedRange = ref(12)

const hasData = computed(() =>
  props.data && props.data.monthlyData && props.data.monthlyData.length > 0
)

const onRangeChange = (val: number) => {
  emit('rangeChange', val)
}

const formatAmount = (val: number) => {
  if (Math.abs(val) >= 10000) return `¥${(val / 10000).toFixed(1)}万`
  return `¥${val.toLocaleString()}`
}

const barChartOption = computed(() => {
  if (!hasData.value) return {}

  const months = props.data.monthlyData.map(m => m.month)
  const incomes = props.data.monthlyData.map(m => m.income)
  const expenses = props.data.monthlyData.map(m => m.expense)

  return {
    tooltip: {
      trigger: 'axis',
      axisPointer: { type: 'shadow' },
      backgroundColor: CHART_TOOLTIP.bg,
      borderColor: CHART_TOOLTIP.border,
      borderWidth: 1,
      textStyle: { color: CHART_TOOLTIP.text },
      extraCssText: `box-shadow: ${CHART_TOOLTIP.shadow}`,
      formatter: (params: any) => {
        let html = `${params[0].axisValue}<br/>`
        params.forEach((p: any) => {
          html += `${p.marker} ${p.seriesName}: ¥${Number(p.value).toLocaleString('zh-CN', { minimumFractionDigits: 2 })}<br/>`
        })
        return html
      }
    },
    legend: {
      data: ['收入', '支出'],
      bottom: 0,
      textStyle: { color: CHART_AXIS.axisLabel, fontSize: 12 }
    },
    grid: {
      left: '3%',
      right: '4%',
      bottom: '40px',
      top: '10px',
      containLabel: true
    },
    xAxis: {
      type: 'category',
      data: months,
      axisLine: { lineStyle: { color: CHART_AXIS.axisLine } },
      axisLabel: { color: CHART_AXIS.axisLabel, fontSize: 11 },
      axisTick: { show: false }
    },
    yAxis: {
      type: 'value',
      axisLine: { show: false },
      axisTick: { show: false },
      splitLine: { lineStyle: { color: CHART_AXIS.splitLine, type: 'dashed' } },
      axisLabel: { color: CHART_AXIS.axisLabel, fontSize: 11, formatter: formatAmount }
    },
    series: [
      {
        name: '收入',
        type: 'bar',
        data: incomes,
        barWidth: '35%',
        itemStyle: {
          color: CHART_COLORS.income,
          borderRadius: [4, 4, 0, 0]
        }
      },
      {
        name: '支出',
        type: 'bar',
        data: expenses,
        barWidth: '35%',
        itemStyle: {
          color: CHART_COLORS.expense,
          borderRadius: [4, 4, 0, 0]
        }
      }
    ]
  }
})

const lineChartOption = computed(() => {
  if (!hasData.value) return {}

  const months = props.data.monthlyData.map(m => m.month)
  const profits = props.data.monthlyData.map(m => m.profit)

  return {
    tooltip: {
      trigger: 'axis',
      backgroundColor: CHART_TOOLTIP.bg,
      borderColor: CHART_TOOLTIP.border,
      borderWidth: 1,
      textStyle: { color: CHART_TOOLTIP.text },
      extraCssText: `box-shadow: ${CHART_TOOLTIP.shadow}`,
      formatter: (params: any) => {
        const p = params[0]
        const value = Number(p.value)
        const color = value >= 0 ? CHART_COLORS.income : CHART_COLORS.expense
        return `${p.axisValue}<br/>
          <span style="color:${color}">利润: ¥${value.toLocaleString('zh-CN', { minimumFractionDigits: 2 })}</span>`
      }
    },
    grid: {
      left: '3%',
      right: '4%',
      bottom: '3%',
      top: '10px',
      containLabel: true
    },
    xAxis: {
      type: 'category',
      data: months,
      boundaryGap: false,
      axisLine: { lineStyle: { color: CHART_AXIS.axisLine } },
      axisLabel: { color: CHART_AXIS.axisLabel, fontSize: 11 },
      axisTick: { show: false }
    },
    yAxis: {
      type: 'value',
      axisLine: { show: false },
      axisTick: { show: false },
      splitLine: { lineStyle: { color: CHART_AXIS.splitLine, type: 'dashed' } },
      axisLabel: { color: CHART_AXIS.axisLabel, fontSize: 11, formatter: formatAmount }
    },
    series: [
      {
        name: '利润',
        type: 'line',
        data: profits,
        smooth: true,
        symbol: 'circle',
        symbolSize: 6,
        lineStyle: { color: CHART_COLORS.profit, width: 3 },
        itemStyle: {
          color: CHART_COLORS.profit,
          borderWidth: 2,
          borderColor: 'var(--bg-card)'
        },
        areaStyle: {
          color: {
            type: 'linear',
            x: 0, y: 0, x2: 0, y2: 1,
            colorStops: [
              { offset: 0, color: CHART_GRADIENT.profit.start },
              { offset: 1, color: CHART_GRADIENT.profit.end }
            ]
          }
        },
        markLine: {
          silent: true,
          data: [
            {
              yAxis: 0,
              lineStyle: { color: CHART_AXIS.axisLine, type: 'dashed', width: 1 }
            }
          ],
          label: { show: false },
          symbol: 'none'
        }
      }
    ]
  }
})

const pieChartOption = computed(() => {
  if (!props.data.expenseCategories || props.data.expenseCategories.length === 0) return {}

  const colors = [...CHART_PALETTE]

  return {
    tooltip: {
      trigger: 'item',
      backgroundColor: CHART_TOOLTIP.bg,
      borderColor: CHART_TOOLTIP.border,
      borderWidth: 1,
      textStyle: { color: CHART_TOOLTIP.text },
      extraCssText: `box-shadow: ${CHART_TOOLTIP.shadow}`,
      formatter: (params: any) => {
        return `${params.name}<br/>金额: ¥${Number(params.value).toLocaleString('zh-CN', { minimumFractionDigits: 2 })}<br/>占比: ${params.percent}%`
      }
    },
    legend: {
      orient: 'vertical',
      right: '5%',
      top: 'center',
      textStyle: { color: CHART_AXIS.axisLabel, fontSize: 12 },
      formatter: (name: string) => {
        const item = props.data.expenseCategories.find(c => c.categoryName === name)
        if (item) return `${name}  ${item.percentage}%`
        return name
      }
    },
    series: [
      {
        type: 'pie',
        radius: ['40%', '70%'],
        center: ['35%', '50%'],
        avoidLabelOverlap: false,
        itemStyle: {
          borderRadius: 6,
          borderColor: 'var(--bg-card)',
          borderWidth: 2
        },
        label: { show: false },
        emphasis: {
          label: {
            show: true,
            fontSize: 14,
            fontWeight: 'bold'
          }
        },
        labelLine: { show: false },
        data: props.data.expenseCategories.map((c, i) => ({
          value: c.amount,
          name: c.categoryName,
          itemStyle: { color: colors[i % colors.length] }
        }))
      }
    ]
  }
})
</script>

<style scoped>
.charts-container {
  background: var(--bg-card);
  border-radius: 12px;
  padding: 20px 24px;
  margin-bottom: 16px;
  box-shadow: var(--shadow-card);
}

.chart-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 16px;
}

.chart-title {
  font-size: 16px;
  font-weight: 600;
  color: var(--text-primary);
  margin: 0;
}

.chart-card {
  background: var(--bg-page);
  border-radius: 8px;
  padding: 16px;
}

.chart-card-title {
  font-size: 14px;
  font-weight: 600;
  color: var(--text-regular);
  margin-bottom: 8px;
}

.chart {
  width: 100%;
  height: 300px;
}

.chart-loading {
  display: flex;
  align-items: center;
  justify-content: center;
  height: 300px;
  gap: 8px;
  color: var(--text-placeholder);
  font-size: 14px;
}

.chart-empty {
  height: 300px;
  display: flex;
  align-items: center;
  justify-content: center;
}

.chart-empty-inner {
  height: 260px;
  display: flex;
  align-items: center;
  justify-content: center;
}

@media (max-width: 768px) {
  .chart-header {
    flex-direction: column;
    align-items: flex-start;
    gap: 12px;
  }

  .chart {
    height: 240px;
  }
}
</style>
