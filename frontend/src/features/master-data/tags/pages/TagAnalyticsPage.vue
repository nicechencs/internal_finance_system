<template>
  <div class="tag-analytics-page">
    <el-card class="filter-card">
      <div class="filter-row">
        <el-select v-model="summaryScope" placeholder="选择标签作用域" style="width: 160px">
          <el-option v-for="s in scopeOptions" :key="s.value" :label="s.label" :value="s.value" :disabled="s.disabled" />
        </el-select>
        <el-date-picker
          v-model="summaryDateRange"
          type="daterange"
          range-separator="至"
          start-placeholder="开始日期"
          end-placeholder="结束日期"
          value-format="YYYY-MM-DD"
          style="width: 260px"
        />
        <el-button type="primary" :loading="summaryLoading" @click="loadSummary">查询</el-button>
        <el-button @click="resetSummary">重置</el-button>
      </div>
    </el-card>

    <!-- 汇总统计卡片 -->
    <el-row v-if="summaryData" :gutter="12" class="stat-cards">
      <el-col :xs="12" :sm="12" :md="6">
        <el-card class="stat-card">
          <div class="stat-label">总交易数</div>
          <div class="stat-value">{{ summaryData.totalTransactionCount }}</div>
        </el-card>
      </el-col>
      <el-col :xs="12" :sm="12" :md="6">
        <el-card class="stat-card">
          <div class="stat-label">总收入</div>
          <div class="stat-value income">{{ formatAmount(summaryData.totalIncomeAmount) }}</div>
        </el-card>
      </el-col>
      <el-col :xs="12" :sm="12" :md="6">
        <el-card class="stat-card">
          <div class="stat-label">总支出</div>
          <div class="stat-value expense">{{ formatAmount(summaryData.totalExpenseAmount) }}</div>
        </el-card>
      </el-col>
      <el-col :xs="12" :sm="12" :md="6">
        <el-card class="stat-card">
          <div class="stat-label">净额</div>
          <div class="stat-value" :class="summaryData.totalNetAmount >= 0 ? 'income' : 'expense'">
            {{ formatAmount(summaryData.totalNetAmount) }}
          </div>
        </el-card>
      </el-col>
    </el-row>

    <!-- 标签汇总条形图 -->
    <el-card v-if="summaryData && summaryData.items.length > 0" class="chart-card">
      <template #header>
        <span>各标签交易分布</span>
        <el-tooltip content="注：同一交易可能命中多个标签，各标签统计之和可能大于总体" placement="top">
          <el-icon style="margin-left: 4px; cursor: help"><InfoFilled /></el-icon>
        </el-tooltip>
      </template>
      <v-chart class="bar-chart" :option="barChartOption" autoresize />
    </el-card>

    <!-- 标签明细表格 -->
    <el-card v-if="summaryData && summaryData.items.length > 0" class="table-card">
      <template #header><span>标签明细</span></template>
      <el-table :data="summaryData.items" stripe size="small" class="clickable-rows" @row-click="handleTagRowClick">
        <el-table-column label="标签" min-width="120">
          <template #default="{ row }">
            <span v-if="row.tagColor" class="color-dot" :style="{ backgroundColor: row.tagColor }" />
            {{ row.tagName }}
          </template>
        </el-table-column>
        <el-table-column prop="transactionCount" label="笔数" width="80" align="center" />
        <el-table-column label="收入" width="120" align="right">
          <template #default="{ row }">
            <span class="income">{{ formatAmount(row.incomeAmount) }}</span>
          </template>
        </el-table-column>
        <el-table-column label="支出" width="120" align="right">
          <template #default="{ row }">
            <span class="expense">{{ formatAmount(row.expenseAmount) }}</span>
          </template>
        </el-table-column>
        <el-table-column label="净额" width="120" align="right">
          <template #default="{ row }">
            <span :class="row.netAmount >= 0 ? 'income' : 'expense'">{{ formatAmount(row.netAmount) }}</span>
          </template>
        </el-table-column>
        <el-table-column label="收入占比" width="90" align="center">
          <template #default="{ row }">{{ row.incomePercentage }}%</template>
        </el-table-column>
        <el-table-column label="支出占比" width="90" align="center">
          <template #default="{ row }">{{ row.expensePercentage }}%</template>
        </el-table-column>
      </el-table>
    </el-card>

    <el-empty v-if="summaryData && summaryData.items.length === 0" description="该作用域暂无标签数据" />

    <!-- 交叉分析 -->
    <el-card class="cross-card">
      <template #header><span>标签交叉分析</span></template>
      <div class="filter-row">
        <el-select v-model="crossRowScope" placeholder="行维度（标签作用域）" style="width: 200px">
          <el-option v-for="s in scopeOptions" :key="s.value" :label="s.label" :value="s.value" :disabled="s.disabled" />
        </el-select>
        <el-select v-model="crossColScope" placeholder="列维度（标签作用域）" style="width: 200px">
          <el-option v-for="s in scopeOptions" :key="s.value" :label="s.label" :value="s.value" :disabled="s.disabled" />
        </el-select>
        <el-date-picker
          v-model="crossDateRange"
          type="daterange"
          range-separator="至"
          start-placeholder="开始日期"
          end-placeholder="结束日期"
          value-format="YYYY-MM-DD"
          style="width: 260px"
        />
        <el-button type="primary" :loading="crossLoading" @click="loadCross">查询</el-button>
        <el-button @click="resetCross">重置</el-button>
      </div>

      <div v-if="crossData" class="cross-table-wrapper">
        <el-table
          :data="crossTableRows"
          border
          size="small"
          style="margin-top: 16px"
        >
          <el-table-column label="行\列" fixed width="120">
            <template #default="{ row }">
              <span v-if="row.color" class="color-dot" :style="{ backgroundColor: row.color }" />
              {{ row.name }}
            </template>
          </el-table-column>
          <el-table-column
            v-for="colTag in crossData.colTags"
            :key="colTag.tagId"
            :label="colTag.tagName"
            min-width="100"
            align="center"
          >
            <template #default="{ row }">
              <template v-if="getCrossCell(row.tagId, colTag.tagId)">
                <div>{{ getCrossCell(row.tagId, colTag.tagId)!.transactionCount }} 笔</div>
                <div :class="getCrossCell(row.tagId, colTag.tagId)!.netAmount >= 0 ? 'income' : 'expense'" style="font-size: 12px">
                  {{ formatAmount(getCrossCell(row.tagId, colTag.tagId)!.netAmount) }}
                </div>
              </template>
              <span v-else class="cross-empty">-</span>
            </template>
          </el-table-column>
        </el-table>
      </div>
    </el-card>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { ElMessage } from 'element-plus'
import { InfoFilled } from '@element-plus/icons-vue'
import { use } from 'echarts/core'
import { CanvasRenderer } from 'echarts/renderers'
import { BarChart } from 'echarts/charts'
import { GridComponent, TooltipComponent, LegendComponent } from 'echarts/components'
import VChart from 'vue-echarts'
import { getTagSummary, getTagCrossAnalysis } from '../api/tag'
import type { TagSummaryDto, TagCrossAnalysisDto, TagCrossAnalysisCellDto } from '../types/tag'

use([CanvasRenderer, BarChart, GridComponent, TooltipComponent, LegendComponent])

const router = useRouter()

const handleTagRowClick = (row: { tagId: number }) => {
  router.push({ name: 'Transactions', query: { tagId: String(row.tagId) } })
}

// ── 常量 ──
const scopeOptions = [
  { label: '交易记录', value: 'transaction' },
  { label: '项目', value: 'project' },
  { label: '人员', value: 'person' },
  { label: '客户', value: 'customer' },
  { label: '供应商', value: 'supplier' },
  { label: '应收（仅可打标，暂不支持分析）', value: 'receivable', disabled: true },
  { label: '应付（仅可打标，暂不支持分析）', value: 'payable', disabled: true },
]

// ── 汇总统计 ──
const summaryScope = ref('transaction')
const summaryDateRange = ref<string[]>([])
const summaryLoading = ref(false)
const summaryData = ref<TagSummaryDto | null>(null)

const loadSummary = async () => {
  if (!summaryScope.value) {
    ElMessage.warning('请选择标签作用域')
    return
  }
  summaryLoading.value = true
  try {
    const params = summaryDateRange.value?.length === 2
      ? { dateFrom: summaryDateRange.value[0], dateTo: summaryDateRange.value[1] }
      : undefined
    const res = await getTagSummary(summaryScope.value, params)
    summaryData.value = res.data.data
  } catch {
    // 请求拦截器已自动弹出后端错误消息，此处不重复弹
  } finally {
    summaryLoading.value = false
  }
}

const resetSummary = () => {
  summaryScope.value = 'transaction'
  summaryDateRange.value = []
  summaryData.value = null
}

// ── 条形图 ──
const barChartOption = computed(() => {
  if (!summaryData.value) return {}
  const items = summaryData.value.items
  return {
    tooltip: { trigger: 'axis', axisPointer: { type: 'shadow' } },
    legend: { data: ['收入', '支出'] },
    grid: { left: 80, right: 20, bottom: 60, containLabel: true },
    xAxis: {
      type: 'category',
      data: items.map(i => i.tagName),
      axisLabel: { rotate: 30, interval: 0 }
    },
    yAxis: { type: 'value', axisLabel: { formatter: (v: number) => formatAmount(v) } },
    series: [
      {
        name: '收入',
        type: 'bar',
        data: items.map(i => i.incomeAmount),
        itemStyle: { color: '#409eff' },
        label: {
          show: true,
          position: 'top',
          formatter: (p: { dataIndex: number }) => {
            const item = items[p.dataIndex]
            return item && item.transactionCount > 0 ? `${item.transactionCount}笔` : ''
          }
        }
      },
      {
        name: '支出',
        type: 'bar',
        data: items.map(i => i.expenseAmount),
        itemStyle: { color: '#f56c6c' }
      }
    ]
  }
})

// ── 交叉分析 ──
const crossRowScope = ref('')
const crossColScope = ref('')
const crossDateRange = ref<string[]>([])
const crossLoading = ref(false)
const crossData = ref<TagCrossAnalysisDto | null>(null)

const crossCellMap = computed(() => {
  if (!crossData.value) return new Map<string, TagCrossAnalysisCellDto>()
  const map = new Map<string, TagCrossAnalysisCellDto>()
  for (const cell of crossData.value.cells) {
    map.set(`${cell.rowTagId}-${cell.colTagId}`, cell)
  }
  return map
})

const getCrossCell = (rowTagId: number, colTagId: number): TagCrossAnalysisCellDto | null => {
  return crossCellMap.value.get(`${rowTagId}-${colTagId}`) ?? null
}

const crossTableRows = computed(() => {
  if (!crossData.value) return []
  return crossData.value.rowTags.map(t => ({ tagId: t.tagId, name: t.tagName, color: t.tagColor }))
})

const loadCross = async () => {
  if (!crossRowScope.value || !crossColScope.value) {
    ElMessage.warning('请选择行维度和列维度')
    return
  }
  if (crossRowScope.value === crossColScope.value) {
    ElMessage.warning('行维度与列维度不能相同')
    return
  }
  crossLoading.value = true
  try {
    const params = crossDateRange.value?.length === 2
      ? { dateFrom: crossDateRange.value[0], dateTo: crossDateRange.value[1] }
      : undefined
    const res = await getTagCrossAnalysis(crossRowScope.value, crossColScope.value, params)
    crossData.value = res.data.data
  } catch {
    // 请求拦截器已自动弹出后端错误消息，此处不重复弹
  } finally {
    crossLoading.value = false
  }
}

const resetCross = () => {
  crossRowScope.value = ''
  crossColScope.value = ''
  crossDateRange.value = []
  crossData.value = null
}

// ── 工具函数 ──
const formatAmount = (val: number) => {
  if (val === 0) return '¥0'
  const abs = Math.abs(val)
  const formatted = abs >= 10000
    ? `¥${(abs / 10000).toFixed(2)}万`
    : `¥${abs.toFixed(2)}`
  return val < 0 ? `-${formatted}` : formatted
}

// ── 初始加载 ──
onMounted(() => {
  loadSummary()
})
</script>

<style scoped>
.tag-analytics-page {
  display: flex;
  flex-direction: column;
  gap: 16px;
}

.filter-card .filter-row {
  display: flex;
  flex-wrap: wrap;
  gap: 12px;
  align-items: center;
}

.stat-cards {
  margin: 0;
}

.stat-card {
  text-align: center;
}

.stat-label {
  font-size: 13px;
  color: var(--text-secondary);
  margin-bottom: 8px;
}

.stat-value {
  font-size: 22px;
  font-weight: 600;
  color: var(--text-primary);
}

.stat-value.income { color: var(--color-primary); }
.stat-value.expense { color: var(--color-danger); }

.income { color: var(--color-primary); }
.expense { color: var(--color-danger); }

.chart-card .bar-chart {
  height: 320px;
}

.color-dot {
  display: inline-block;
  width: 10px;
  height: 10px;
  border-radius: 50%;
  margin-right: 4px;
  border: 1px solid rgba(0,0,0,0.1);
  vertical-align: middle;
}

.cross-card .filter-row {
  display: flex;
  flex-wrap: wrap;
  gap: 12px;
  align-items: center;
}

.cross-table-wrapper {
  overflow-x: auto;
}

.cross-empty {
  color: var(--text-placeholder);
}

.clickable-rows :deep(tr) {
  cursor: pointer;
}
</style>
