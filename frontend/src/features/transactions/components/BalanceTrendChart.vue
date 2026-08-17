<template>
  <div class="chart-card">
    <div class="chart-header">
      <div class="chart-heading">
        <h3 class="chart-title">余额趋势</h3>
        <p class="chart-subtitle">
          {{ hasData ? `最近 ${trends.length} 个时间点的余额变化` : '查看账户余额的阶段性变化' }}
        </p>
      </div>
      <el-radio-group v-model="selectedRange" size="small" class="range-switch" @change="onRangeChange">
        <el-radio-button :value="1">1个月</el-radio-button>
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
      <el-empty description="暂无趋势数据" :image-size="72" />
    </div>

    <v-chart
      v-else
      class="chart"
      :option="chartOption"
      autoresize
    />
  </div>
</template>

<script setup lang="ts">
import { computed, ref } from 'vue'
import { use } from 'echarts/core'
import { CanvasRenderer } from 'echarts/renderers'
import { LineChart } from 'echarts/charts'
import {
  DataZoomComponent,
  GridComponent,
  TitleComponent,
  TooltipComponent
} from 'echarts/components'
import { Loading } from '@element-plus/icons-vue'
import VChart from 'vue-echarts'
import type { BalanceTrendItem } from '@/features/master-data/accounts/types/account'
import { CHART_COLORS, CHART_AXIS, CHART_TOOLTIP, CHART_GRADIENT } from '@/shared/constants/colors'
import { toDateOnlyString } from '@/shared/utils/date'

use([
  CanvasRenderer,
  LineChart,
  TitleComponent,
  TooltipComponent,
  GridComponent,
  DataZoomComponent
])

const props = defineProps<{
  trends: BalanceTrendItem[]
  loading: boolean
}>()

const emit = defineEmits<{
  rangeChange: [months: number]
}>()

const selectedRange = ref(6)

const hasData = computed(() => props.trends.length > 0)

const onRangeChange = (val: number) => {
  emit('rangeChange', val)
}

const chartOption = computed(() => {
  if (!hasData.value) return {}

  const dates = props.trends.map((item) => {
    const date = toDateOnlyString(item.date)
    return date ? date.slice(0, 7) : item.date
  })
  const balances = props.trends.map(item => item.balance)

  return {
    tooltip: {
      trigger: 'axis',
      backgroundColor: CHART_TOOLTIP.bg,
      borderWidth: 1,
      borderColor: CHART_TOOLTIP.border,
      textStyle: {
        color: CHART_TOOLTIP.text
      },
      extraCssText: `box-shadow: ${CHART_TOOLTIP.shadow}`,
      formatter: (params: any) => {
        const point = params[0]
        return `${point.axisValue}<br/>余额: ¥${Number(point.value).toLocaleString('zh-CN', {
          minimumFractionDigits: 2,
          maximumFractionDigits: 2
        })}`
      }
    },
    grid: {
      left: '3%',
      right: '4%',
      bottom: '2%',
      top: '12px',
      containLabel: true
    },
    xAxis: {
      type: 'category',
      data: dates,
      boundaryGap: false,
      axisLine: { lineStyle: { color: CHART_AXIS.axisLine } },
      axisLabel: { color: CHART_AXIS.axisLabel, fontSize: 12 },
      axisTick: { show: false }
    },
    yAxis: {
      type: 'value',
      axisLine: { show: false },
      axisTick: { show: false },
      splitLine: { lineStyle: { color: CHART_AXIS.splitLine, type: 'dashed' } },
      axisLabel: {
        color: CHART_AXIS.axisLabel,
        fontSize: 12,
        formatter: (val: number) => {
          if (Math.abs(val) >= 10000) return `¥${(val / 10000).toFixed(1)}万`
          return `¥${val.toLocaleString()}`
        }
      }
    },
    series: [
      {
        name: '余额',
        type: 'line',
        data: balances,
        smooth: true,
        symbol: 'circle',
        symbolSize: 7,
        lineStyle: {
          color: CHART_COLORS.balance,
          width: 3
        },
        itemStyle: {
          color: CHART_COLORS.balance,
          borderWidth: 2,
          borderColor: 'var(--bg-card)'
        },
        areaStyle: {
          color: {
            type: 'linear',
            x: 0,
            y: 0,
            x2: 0,
            y2: 1,
            colorStops: [
              { offset: 0, color: CHART_GRADIENT.balance.start },
              { offset: 1, color: CHART_GRADIENT.balance.end }
            ]
          }
        }
      }
    ]
  }
})
</script>

<style scoped>
.chart-card {
  background: var(--bg-page);
  border-radius: 12px;
  padding: 16px;
  height: 100%;
  display: flex;
  flex-direction: column;
  min-height: 0;
}

.chart-header {
  display: flex;
  justify-content: space-between;
  align-items: flex-start;
  gap: 16px;
  margin-bottom: 16px;
}

.chart-heading {
  min-width: 0;
}

.chart-title {
  font-size: 14px;
  font-weight: 600;
  color: var(--text-primary);
  margin: 0;
}

.chart-subtitle {
  margin: 6px 0 0;
  font-size: 12px;
  line-height: 1.5;
  color: var(--text-secondary);
}

.range-switch {
  flex-shrink: 0;
}

.chart {
  width: 100%;
  height: 248px;
}

.chart-loading,
.chart-empty {
  height: 248px;
  display: flex;
  align-items: center;
  justify-content: center;
}

.chart-loading {
  gap: 8px;
  color: var(--text-placeholder);
  font-size: 14px;
}

@media (max-width: 768px) {
  .chart-header {
    flex-direction: column;
    align-items: stretch;
  }

  .range-switch {
    width: 100%;
  }

  .range-switch :deep(.el-radio-button) {
    flex: 1;
  }

  .range-switch :deep(.el-radio-button__inner) {
    width: 100%;
  }

  .chart,
  .chart-loading,
  .chart-empty {
    height: 220px;
  }
}
</style>
