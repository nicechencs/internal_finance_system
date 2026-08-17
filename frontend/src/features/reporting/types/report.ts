// ECharts 图表数据结构
export interface ChartData {
  xAxis: string[]
  series: ChartSeries[]
}

export interface ChartSeries {
  name: string
  data: number[]
  type?: 'line' | 'bar' | 'pie'
}

// 月度利润报表
export interface MonthlyProfitReport {
  year: number
  month: number
  totalIncome: number
  totalExpense: number
  netProfit: number
  profitMargin: number // 利润率
  incomeByCategory: CategoryAmount[]
  expenseByCategory: CategoryAmount[]
  chartData: ChartData
}

export interface CategoryAmount {
  categoryName: string
  amount: number
  percentage: number
}

// 现金流报表
export interface CashflowReport {
  startDate: string
  endDate: string
  openingBalance: number
  closingBalance: number
  totalInflow: number
  totalOutflow: number
  netCashflow: number
  inflowByAccount: AccountCashflow[]
  outflowByAccount: AccountCashflow[]
  dailyCashflow: DailyCashflow[]
  chartData: ChartData
}

export interface AccountCashflow {
  accountName: string
  amount: number
}

export interface DailyCashflow {
  date: string
  inflow: number
  outflow: number
  net: number
}

// 项目利润报表
export interface ProjectProfitReport {
  projects: ProjectProfit[]
  totalIncome: number
  totalExpense: number
  totalProfit: number
  chartData: ChartData
}

export interface ProjectProfit {
  projectId: number
  projectName: string
  income: number
  expense: number
  profit: number
  profitMargin: number
  status: string
}

// 人员成本报表
export interface PersonCostReport {
  persons: PersonCost[]
  totalCost: number
  averageCost: number
  chartData: ChartData
}

export interface PersonCost {
  personId: number
  personName: string
  department?: string
  totalCost: number
  projectCount: number
  projects: PersonProjectCost[]
}

export interface PersonProjectCost {
  projectName: string
  cost: number
}

// 供应商费用报表
export interface SupplierExpenseReport {
  suppliers: SupplierExpense[]
  totalExpense: number
  chartData: ChartData
}

export interface SupplierExpense {
  supplierId: number
  supplierName: string
  totalExpense: number
  transactionCount: number
  categories: SupplierCategoryExpense[]
}

export interface SupplierCategoryExpense {
  categoryName: string
  amount: number
}

// 年度总览报表
export interface AnnualOverviewReport {
  year: number
  totalIncome: number
  totalExpense: number
  netProfit: number
  profitMargin: number
  monthlyData: MonthlyOverview[]
  incomeByCategory: CategoryAmount[]
  expenseByCategory: CategoryAmount[]
  topProjects: ProjectProfit[]
  topSuppliers: SupplierExpense[]
  chartData: {
    monthlyTrend: ChartData
    incomeDistribution: ChartData
    expenseDistribution: ChartData
  }
}

export interface MonthlyOverview {
  month: number
  income: number
  expense: number
  profit: number
}

