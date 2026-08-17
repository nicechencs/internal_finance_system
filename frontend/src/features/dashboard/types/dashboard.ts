// 仪表盘汇总数据
export interface DashboardSummary {
  totalIncome: number
  totalExpense: number
  netProfit: number
  totalBalance: number
  accountCount: number
  transactionCount: number
  projectCount: number
}

// 月度统计
export interface MonthlyStats {
  month: string // "2026-01"
  income: number
  expense: number
  net: number
}

// 分类统计
export interface CategoryStats {
  categoryName: string
  amount: number
  percentage: number
}

// 近期交易
export interface RecentTransaction {
  id: number
  transactionDate: string
  transactionType: 'Income' | 'Expense' | 'Transfer'
  amount: number
  accountName: string
  categoryName?: string
  counterpartyName?: string
  description?: string
}
