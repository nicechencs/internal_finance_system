<template>
  <div class="page-container reports-page">
    <div class="page-header">
      <div class="page-header-left">
        <h2 class="page-title">经营报表</h2>
        <p class="page-desc">按月、按区间或全年查看利润、现金流与成本构成。</p>
      </div>
    </div>

    <el-tabs v-model="activeTab" class="report-tabs" @tab-change="handleTabChange">
      <el-tab-pane label="月度利润" name="monthly">
        <div class="search-section search-section-top">
          <el-form :inline="true" class="search-form" @submit.prevent="loadMonthly">
            <el-form-item label="月份">
              <el-date-picker
                v-model="monthValue"
                type="month"
                placeholder="选择月份"
                value-format="YYYY-MM"
                :clearable="false"
              />
            </el-form-item>
            <el-form-item class="search-buttons">
              <el-button type="primary" native-type="submit" :loading="monthly.loading">查询</el-button>
            </el-form-item>
          </el-form>
        </div>

        <el-alert
          v-if="monthly.error"
          class="report-alert"
          type="error"
          :title="monthly.error"
          show-icon
          :closable="false"
        />

        <template v-else-if="monthly.data">
          <el-row :gutter="16" class="stat-cards">
            <el-col :xs="12" :sm="12" :md="6">
              <StatCard :icon="TrendCharts" :value="formatMoney(monthly.data.totalIncome)" label="收入" theme="income" />
            </el-col>
            <el-col :xs="12" :sm="12" :md="6">
              <StatCard :icon="Minus" :value="formatMoney(monthly.data.totalExpense)" label="支出" theme="expense" />
            </el-col>
            <el-col :xs="12" :sm="12" :md="6">
              <StatCard :icon="Coin" :value="formatMoney(monthly.data.netProfit)" label="净利润" theme="profit" />
            </el-col>
            <el-col :xs="12" :sm="12" :md="6">
              <StatCard :icon="DataAnalysis" :value="formatRate(monthly.data.profitRate)" label="利润率" theme="info" />
            </el-col>
          </el-row>

          <el-row :gutter="16">
            <el-col :xs="24" :md="12">
              <div class="table-section">
                <h3 class="section-title">收入分类</h3>
                <el-table :data="monthly.data.incomeByCategory" v-loading="monthly.loading" border>
                  <template #empty>本月暂无收入分类</template>
                  <el-table-column prop="categoryName" label="分类" min-width="140" />
                  <el-table-column label="金额" min-width="140" align="right">
                    <template #default="{ row }">
                      <span class="text-income">{{ formatMoney(row.amount) }}</span>
                    </template>
                  </el-table-column>
                </el-table>
              </div>
            </el-col>
            <el-col :xs="24" :md="12">
              <div class="table-section">
                <h3 class="section-title">支出分类</h3>
                <el-table :data="monthly.data.expenseByCategory" v-loading="monthly.loading" border>
                  <template #empty>本月暂无支出分类</template>
                  <el-table-column prop="categoryName" label="分类" min-width="140" />
                  <el-table-column label="金额" min-width="140" align="right">
                    <template #default="{ row }">
                      <span class="text-expense">{{ formatMoney(row.amount) }}</span>
                    </template>
                  </el-table-column>
                </el-table>
              </div>
            </el-col>
          </el-row>
        </template>
        <el-empty v-else-if="!monthly.loading" description="暂无月度利润数据" />
      </el-tab-pane>

      <el-tab-pane label="现金流" name="cashflow">
        <div class="search-section search-section-top">
          <el-form :inline="true" class="search-form" @submit.prevent="loadCashflow">
            <el-form-item label="日期范围">
              <el-date-picker
                v-model="dateRange"
                type="daterange"
                range-separator="至"
                start-placeholder="开始日期"
                end-placeholder="结束日期"
                value-format="YYYY-MM-DD"
                :clearable="false"
              />
            </el-form-item>
            <el-form-item class="search-buttons">
              <el-button type="primary" native-type="submit" :loading="cashflow.loading">查询</el-button>
            </el-form-item>
          </el-form>
        </div>

        <el-alert
          v-if="cashflow.error"
          class="report-alert"
          type="error"
          :title="cashflow.error"
          show-icon
          :closable="false"
        />

        <template v-else-if="cashflow.data">
          <el-row :gutter="16" class="stat-cards">
            <el-col :xs="12" :sm="12" :md="6">
              <StatCard :icon="Wallet" :value="formatMoney(cashflow.data.openingBalance)" label="期初余额" theme="balance" />
            </el-col>
            <el-col :xs="12" :sm="12" :md="6">
              <StatCard :icon="TrendCharts" :value="formatMoney(cashflow.data.totalIncome)" label="流入" theme="income" />
            </el-col>
            <el-col :xs="12" :sm="12" :md="6">
              <StatCard :icon="Minus" :value="formatMoney(cashflow.data.totalExpense)" label="流出" theme="expense" />
            </el-col>
            <el-col :xs="12" :sm="12" :md="6">
              <StatCard :icon="Wallet" :value="formatMoney(cashflow.data.closingBalance)" label="期末余额" theme="profit" />
            </el-col>
          </el-row>

          <div class="table-section">
            <h3 class="section-title">分月明细</h3>
            <el-table :data="cashflow.data.monthlyDetail" v-loading="cashflow.loading" border>
              <template #empty>该区间暂无现金流明细</template>
              <el-table-column prop="month" label="月份" min-width="110" />
              <el-table-column label="期初" min-width="130" align="right">
                <template #default="{ row }">{{ formatMoney(row.openingBalance) }}</template>
              </el-table-column>
              <el-table-column label="流入" min-width="130" align="right">
                <template #default="{ row }">
                  <span class="text-income">{{ formatMoney(row.income) }}</span>
                </template>
              </el-table-column>
              <el-table-column label="流出" min-width="130" align="right">
                <template #default="{ row }">
                  <span class="text-expense">{{ formatMoney(row.expense) }}</span>
                </template>
              </el-table-column>
              <el-table-column label="期末" min-width="130" align="right">
                <template #default="{ row }">{{ formatMoney(row.closingBalance) }}</template>
              </el-table-column>
            </el-table>
          </div>
        </template>
        <el-empty v-else-if="!cashflow.loading" description="暂无现金流数据" />
      </el-tab-pane>

      <el-tab-pane label="项目利润" name="project">
        <el-alert
          v-if="project.error"
          class="report-alert"
          type="error"
          :title="project.error"
          show-icon
          :closable="false"
        />
        <template v-else-if="project.data">
          <el-row :gutter="16" class="stat-cards">
            <el-col :xs="12" :sm="12" :md="6">
              <StatCard :icon="Briefcase" :value="formatMoney(project.data.summary.totalContract)" label="合同额" theme="info" />
            </el-col>
            <el-col :xs="12" :sm="12" :md="6">
              <StatCard :icon="TrendCharts" :value="formatMoney(project.data.summary.totalReceived)" label="已收款" theme="income" />
            </el-col>
            <el-col :xs="12" :sm="12" :md="6">
              <StatCard :icon="Minus" :value="formatMoney(project.data.summary.totalCost)" label="成本" theme="expense" />
            </el-col>
            <el-col :xs="12" :sm="12" :md="6">
              <StatCard
                :icon="Coin"
                :value="formatMoney(project.data.summary.totalProfit)"
                :count="`平均利润率 ${formatRate(project.data.summary.avgProfitRate)}`"
                label="利润"
                theme="profit"
              />
            </el-col>
          </el-row>
          <div class="table-section">
            <el-table :data="project.data.projects" v-loading="project.loading" border>
              <template #empty>暂无项目利润数据</template>
              <el-table-column prop="projectName" label="项目" min-width="160" show-overflow-tooltip />
              <el-table-column prop="customerName" label="客户" min-width="140" show-overflow-tooltip>
                <template #default="{ row }">{{ row.customerName || '-' }}</template>
              </el-table-column>
              <el-table-column label="合同额" min-width="130" align="right">
                <template #default="{ row }">{{ formatMoney(row.contractAmount) }}</template>
              </el-table-column>
              <el-table-column label="已收款" min-width="130" align="right">
                <template #default="{ row }">
                  <span class="text-income">{{ formatMoney(row.receivedAmount) }}</span>
                </template>
              </el-table-column>
              <el-table-column label="成本" min-width="130" align="right">
                <template #default="{ row }">
                  <span class="text-expense">{{ formatMoney(row.totalCost) }}</span>
                </template>
              </el-table-column>
              <el-table-column label="利润" min-width="130" align="right">
                <template #default="{ row }">{{ formatMoney(row.profitAmount) }}</template>
              </el-table-column>
              <el-table-column label="利润率" width="110" align="right">
                <template #default="{ row }">{{ formatRate(row.profitRate) }}</template>
              </el-table-column>
            </el-table>
          </div>
        </template>
        <el-empty v-else-if="!project.loading" description="暂无项目利润数据" />
      </el-tab-pane>

      <el-tab-pane label="人员成本" name="person">
        <el-alert
          v-if="person.error"
          class="report-alert"
          type="error"
          :title="person.error"
          show-icon
          :closable="false"
        />
        <template v-else-if="person.data">
          <el-row :gutter="16" class="stat-cards">
            <el-col :xs="12" :sm="12" :md="6">
              <StatCard :icon="UserFilled" :value="formatMoney(person.data.summary.totalSalary)" label="工资" theme="info" />
            </el-col>
            <el-col :xs="12" :sm="12" :md="6">
              <StatCard :icon="Coin" :value="formatMoney(person.data.summary.totalCommission)" label="提成" theme="profit" />
            </el-col>
            <el-col :xs="12" :sm="12" :md="6">
              <StatCard :icon="Wallet" :value="formatMoney(person.data.summary.totalReimbursement)" label="报销" theme="balance" />
            </el-col>
            <el-col :xs="12" :sm="12" :md="6">
              <StatCard :icon="Minus" :value="formatMoney(person.data.summary.totalCost)" label="总成本" theme="expense" />
            </el-col>
          </el-row>
          <div class="table-section">
            <el-table :data="person.data.persons" v-loading="person.loading" border>
              <template #empty>暂无人员成本数据</template>
              <el-table-column prop="personName" label="人员" min-width="120" />
              <el-table-column label="类型" width="100">
                <template #default="{ row }">{{ formatPersonType(row.personType) }}</template>
              </el-table-column>
              <el-table-column label="工资" min-width="120" align="right">
                <template #default="{ row }">{{ formatMoney(row.salary) }}</template>
              </el-table-column>
              <el-table-column label="提成" min-width="120" align="right">
                <template #default="{ row }">{{ formatMoney(row.commission) }}</template>
              </el-table-column>
              <el-table-column label="报销" min-width="120" align="right">
                <template #default="{ row }">{{ formatMoney(row.reimbursement) }}</template>
              </el-table-column>
              <el-table-column label="分红" min-width="120" align="right">
                <template #default="{ row }">{{ formatMoney(row.dividend) }}</template>
              </el-table-column>
              <el-table-column label="合计" min-width="130" align="right">
                <template #default="{ row }">
                  <span class="text-expense">{{ formatMoney(row.totalCost) }}</span>
                </template>
              </el-table-column>
            </el-table>
          </div>
        </template>
        <el-empty v-else-if="!person.loading" description="暂无人员成本数据" />
      </el-tab-pane>

      <el-tab-pane label="供应商费用" name="supplier">
        <el-alert
          v-if="supplier.error"
          class="report-alert"
          type="error"
          :title="supplier.error"
          show-icon
          :closable="false"
        />
        <template v-else-if="supplier.data">
          <el-row :gutter="16" class="stat-cards">
            <el-col :xs="12" :sm="12" :md="6">
              <StatCard :icon="Shop" :value="String(supplier.data.summary.supplierCount)" label="供应商数" theme="info" />
            </el-col>
            <el-col :xs="12" :sm="12" :md="6">
              <StatCard :icon="Minus" :value="formatMoney(supplier.data.summary.totalExpense)" label="费用合计" theme="expense" />
            </el-col>
          </el-row>
          <div class="table-section">
            <el-table :data="supplier.data.suppliers" v-loading="supplier.loading" border>
              <template #empty>暂无供应商费用数据</template>
              <el-table-column prop="rank" label="排名" width="80" align="center" />
              <el-table-column prop="supplierName" label="供应商" min-width="180" show-overflow-tooltip />
              <el-table-column prop="transactionCount" label="笔数" width="100" align="right" />
              <el-table-column label="费用" min-width="140" align="right">
                <template #default="{ row }">
                  <span class="text-expense">{{ formatMoney(row.totalExpense) }}</span>
                </template>
              </el-table-column>
            </el-table>
          </div>
        </template>
        <el-empty v-else-if="!supplier.loading" description="暂无供应商费用数据" />
      </el-tab-pane>

      <el-tab-pane label="年度总览" name="annual">
        <div class="search-section search-section-top">
          <el-form :inline="true" class="search-form" @submit.prevent="loadAnnual">
            <el-form-item label="年份">
              <el-date-picker
                v-model="yearValue"
                type="year"
                placeholder="选择年份"
                value-format="YYYY"
                :clearable="false"
              />
            </el-form-item>
            <el-form-item class="search-buttons">
              <el-button type="primary" native-type="submit" :loading="annual.loading">查询</el-button>
            </el-form-item>
          </el-form>
        </div>

        <el-alert
          v-if="annual.error"
          class="report-alert"
          type="error"
          :title="annual.error"
          show-icon
          :closable="false"
        />

        <template v-else-if="annual.data">
          <el-row :gutter="16" class="stat-cards">
            <el-col :xs="12" :sm="12" :md="6">
              <StatCard :icon="TrendCharts" :value="formatMoney(annual.data.totalIncome)" label="全年收入" theme="income" />
            </el-col>
            <el-col :xs="12" :sm="12" :md="6">
              <StatCard :icon="Minus" :value="formatMoney(annual.data.totalExpense)" label="全年支出" theme="expense" />
            </el-col>
            <el-col :xs="12" :sm="12" :md="6">
              <StatCard
                :icon="Coin"
                :value="formatMoney(annual.data.netProfit)"
                :count="`利润率 ${formatRate(annual.data.profitRate)}`"
                label="净利润"
                theme="profit"
              />
            </el-col>
            <el-col :xs="12" :sm="12" :md="6">
              <StatCard
                :icon="Money"
                :value="formatMoney(annual.data.totalReceivable)"
                :count="`应付 ${formatMoney(annual.data.totalPayable)}`"
                label="应收"
                theme="balance"
              />
            </el-col>
          </el-row>

          <div class="table-section">
            <h3 class="section-title">月度趋势</h3>
            <el-table :data="annual.data.monthlyTrend" v-loading="annual.loading" border>
              <template #empty>该年暂无月度数据</template>
              <el-table-column label="月份" width="90">
                <template #default="{ row }">{{ row.month }}月</template>
              </el-table-column>
              <el-table-column label="收入" min-width="130" align="right">
                <template #default="{ row }">
                  <span class="text-income">{{ formatMoney(row.income) }}</span>
                </template>
              </el-table-column>
              <el-table-column label="支出" min-width="130" align="right">
                <template #default="{ row }">
                  <span class="text-expense">{{ formatMoney(row.expense) }}</span>
                </template>
              </el-table-column>
              <el-table-column label="利润" min-width="130" align="right">
                <template #default="{ row }">{{ formatMoney(row.profit) }}</template>
              </el-table-column>
            </el-table>
          </div>

          <el-row :gutter="16">
            <el-col :xs="24" :md="8">
              <div class="table-section">
                <h3 class="section-title">项目 TOP</h3>
                <el-table :data="annual.data.topProjects" border>
                  <template #empty>暂无项目</template>
                  <el-table-column prop="name" label="名称" min-width="120" show-overflow-tooltip />
                  <el-table-column label="金额" min-width="120" align="right">
                    <template #default="{ row }">{{ formatMoney(row.amount) }}</template>
                  </el-table-column>
                </el-table>
              </div>
            </el-col>
            <el-col :xs="24" :md="8">
              <div class="table-section">
                <h3 class="section-title">客户 TOP</h3>
                <el-table :data="annual.data.topCustomers" border>
                  <template #empty>暂无客户</template>
                  <el-table-column prop="name" label="名称" min-width="120" show-overflow-tooltip />
                  <el-table-column label="金额" min-width="120" align="right">
                    <template #default="{ row }">{{ formatMoney(row.amount) }}</template>
                  </el-table-column>
                </el-table>
              </div>
            </el-col>
            <el-col :xs="24" :md="8">
              <div class="table-section">
                <h3 class="section-title">供应商 TOP</h3>
                <el-table :data="annual.data.topSuppliers" border>
                  <template #empty>暂无供应商</template>
                  <el-table-column prop="name" label="名称" min-width="120" show-overflow-tooltip />
                  <el-table-column label="金额" min-width="120" align="right">
                    <template #default="{ row }">{{ formatMoney(row.amount) }}</template>
                  </el-table-column>
                </el-table>
              </div>
            </el-col>
          </el-row>
        </template>
        <el-empty v-else-if="!annual.loading" description="暂无年度总览数据" />
      </el-tab-pane>
    </el-tabs>
  </div>
</template>

<script setup lang="ts">
import { onMounted, reactive, ref } from 'vue'
import {
  Briefcase,
  Coin,
  DataAnalysis,
  Minus,
  Money,
  Shop,
  TrendCharts,
  UserFilled,
  Wallet
} from '@element-plus/icons-vue'
import StatCard from '@/shared/ui/StatCard.vue'
import { formatMoney } from '@/shared/utils/formatters'
import {
  getAnnualOverviewReport,
  getCashflowReport,
  getMonthlyProfitReport,
  getPersonCostReport,
  getProjectProfitReport,
  getSupplierExpenseReport
} from '@/features/reporting/api/report'
import type {
  AnnualOverviewReport,
  CashflowReport,
  MonthlyProfitReport,
  PersonCostReport,
  ProjectProfitReport,
  SupplierExpenseReport
} from '@/features/reporting/types/report'
import {
  currentYearMonth,
  formatPersonType,
  formatRate,
  monthRange,
  toExclusiveEndDate
} from '@/features/reporting/utils/reportFormat'

interface LoadState<T> {
  loading: boolean
  error: string
  data: T | null
  loaded: boolean
}

function createState<T>(): LoadState<T> {
  return { loading: false, error: '', data: null, loaded: false }
}

const { year, month } = currentYearMonth()
const activeTab = ref('monthly')
const monthValue = ref(`${year}-${String(month).padStart(2, '0')}`)
const dateRange = ref<[string, string]>(monthRange(year, month))
const yearValue = ref(String(year))

const monthly = reactive(createState<MonthlyProfitReport>())
const cashflow = reactive(createState<CashflowReport>())
const project = reactive(createState<ProjectProfitReport>())
const person = reactive(createState<PersonCostReport>())
const supplier = reactive(createState<SupplierExpenseReport>())
const annual = reactive(createState<AnnualOverviewReport>())

async function runLoad<T>(state: LoadState<T>, request: () => Promise<{ data: { data: T } }>, fallback: string) {
  state.loading = true
  state.error = ''
  try {
    const response = await request()
    state.data = response.data.data
    state.loaded = true
  } catch {
    state.error = fallback
    state.data = null
  } finally {
    state.loading = false
  }
}

function parseYearMonth(value: string): { year: number; month: number } | null {
  const match = /^(\d{4})-(\d{2})$/.exec(value || '')
  if (!match) return null
  return { year: Number(match[1]), month: Number(match[2]) }
}

async function loadMonthly() {
  const parsed = parseYearMonth(monthValue.value)
  if (!parsed) {
    monthly.error = '请选择有效月份'
    return
  }
  await runLoad(
    monthly,
    () => getMonthlyProfitReport(parsed.year, parsed.month),
    '加载月度利润失败'
  )
}

async function loadCashflow() {
  const [startDate, endDate] = dateRange.value || []
  if (!startDate || !endDate) {
    cashflow.error = '请选择日期范围'
    return
  }
  await runLoad(
    cashflow,
    () => getCashflowReport(startDate, toExclusiveEndDate(endDate)),
    '加载现金流失败'
  )
}

async function loadProject() {
  await runLoad(project, () => getProjectProfitReport(), '加载项目利润失败')
}

async function loadPerson() {
  await runLoad(person, () => getPersonCostReport(), '加载人员成本失败')
}

async function loadSupplier() {
  await runLoad(supplier, () => getSupplierExpenseReport(), '加载供应商费用失败')
}

async function loadAnnual() {
  const yearNumber = Number(yearValue.value)
  if (!Number.isInteger(yearNumber) || yearNumber < 1) {
    annual.error = '请选择有效年份'
    return
  }
  await runLoad(
    annual,
    () => getAnnualOverviewReport(yearNumber),
    '加载年度总览失败'
  )
}

const loaders: Record<string, () => Promise<void>> = {
  monthly: loadMonthly,
  cashflow: loadCashflow,
  project: loadProject,
  person: loadPerson,
  supplier: loadSupplier,
  annual: loadAnnual
}

const states: Record<string, LoadState<unknown>> = {
  monthly,
  cashflow,
  project,
  person,
  supplier,
  annual
}

function handleTabChange(name: string | number) {
  const key = String(name)
  if (!states[key]?.loaded) {
    loaders[key]?.()
  }
}

onMounted(() => {
  loadMonthly()
})
</script>

<style scoped>
.page-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 20px;
}

.page-title {
  font-size: 20px;
  font-weight: 600;
  margin: 0 0 8px;
}

.page-desc {
  color: var(--text-placeholder);
  margin: 0;
}

.search-section {
  background: var(--bg-card);
  border-radius: 12px;
  padding: 20px 20px 4px;
  margin-bottom: 16px;
  box-shadow: 0 1px 3px rgba(0, 0, 0, 0.04);
}

.search-section :deep(.el-form-item__label) {
  color: var(--text-secondary);
  font-size: 13px;
}

.report-alert {
  margin-bottom: 16px;
}

.table-section {
  background: var(--bg-card);
  border-radius: 12px;
  padding: 16px 20px 20px;
  margin-bottom: 16px;
  box-shadow: 0 1px 3px rgba(0, 0, 0, 0.04);
}

.section-title {
  margin: 0 0 12px;
  font-size: 15px;
  font-weight: 600;
  color: var(--text-primary);
}
</style>
