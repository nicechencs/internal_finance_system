// 月度利润报表
export interface MonthlyProfitReport {
  year: number
  month: number
  totalIncome: number
  totalExpense: number
  netProfit: number
  /** 利润率，已是百分数（80 表示 80%） */
  profitRate: number
  incomeByCategory: CategoryAmount[]
  expenseByCategory: CategoryAmount[]
}

export interface CategoryAmount {
  categoryName: string
  amount: number
}

// 现金流报表
export interface CashflowReport {
  startDate: string
  endDate: string
  openingBalance: number
  totalIncome: number
  totalExpense: number
  closingBalance: number
  monthlyDetail: CashflowMonth[]
}

export interface CashflowMonth {
  month: string
  openingBalance: number
  income: number
  expense: number
  closingBalance: number
}

// 项目利润报表
export interface ProjectProfitReport {
  projects: ProjectProfit[]
  summary: ProjectProfitSummary
}

export interface ProjectProfit {
  projectId: number
  projectName: string
  customerName: string
  contractAmount: number
  receivedAmount: number
  totalCost: number
  profitAmount: number
  /** 利润率，已是百分数 */
  profitRate: number
}

export interface ProjectProfitSummary {
  totalContract: number
  totalReceived: number
  totalCost: number
  totalProfit: number
  /** 平均利润率，已是百分数 */
  avgProfitRate: number
}

// 人员成本报表
export interface PersonCostReport {
  persons: PersonCost[]
  summary: PersonCostSummary
}

export interface PersonCost {
  personId: number
  personName: string
  personType: string
  salary: number
  commission: number
  reimbursement: number
  dividend: number
  totalCost: number
}

export interface PersonCostSummary {
  totalSalary: number
  totalCommission: number
  totalReimbursement: number
  totalCost: number
}

// 供应商费用报表
export interface SupplierExpenseReport {
  suppliers: SupplierExpense[]
  summary: SupplierExpenseSummary
}

export interface SupplierExpense {
  supplierId: number
  supplierName: string
  totalExpense: number
  transactionCount: number
  rank: number
}

export interface SupplierExpenseSummary {
  totalExpense: number
  supplierCount: number
}

// 年度总览报表
export interface AnnualOverviewReport {
  year: number
  totalIncome: number
  totalExpense: number
  netProfit: number
  /** 利润率，已是百分数 */
  profitRate: number
  totalReceivable: number
  totalPayable: number
  monthlyTrend: MonthlyOverview[]
  topProjects: TopItem[]
  topCustomers: TopItem[]
  topSuppliers: TopItem[]
}

export interface MonthlyOverview {
  month: number
  income: number
  expense: number
  profit: number
}

export interface TopItem {
  id: number
  name: string
  amount: number
}
