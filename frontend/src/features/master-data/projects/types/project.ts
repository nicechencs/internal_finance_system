export interface CostBreakdownItem {
  categoryName: string
  amount: number
}

export interface Project {
  id: number
  name: string
  projectCode?: string
  customerId?: number
  customerName?: string
  customer?: ProjectCustomer | null
  contractAmount: number
  receivedAmount: number
  receivableAmount: number
  totalCost: number
  profitAmount: number
  profitRate: number
  startDate?: string
  endDate?: string
  status: 'Active' | 'Completed' | 'Cancelled'
  description?: string
  tags: TagItem[]
  costBreakdown?: CostBreakdownItem[]
  createdAt: string
  updatedAt: string
}

export interface ProjectCustomer {
  id: number
  name: string
}

export interface TagItem {
  tagId: number
  tagName: string
  tagColor?: string
}

export interface ProjectStatistics {
  totalCount: number
  totalContractAmount: number
  totalProfit: number
  totalReceivable: number
}

export interface ProjectProfitReport {
  projectId: number
  projectName: string
  projectCode?: string
  contractAmount: number
  receivedAmount: number
  receivableAmount: number
  totalIncome: number
  totalExpense: number
  totalCost: number
  profitAmount: number
  profitRate: number
  startDate?: string
  endDate?: string
  status: string
}

export interface CreateProjectRequest {
  name: string
  projectCode?: string
  customerId?: number
  contractAmount: number
  startDate?: string
  endDate?: string
  status?: string
  description?: string
}

export interface UpdateProjectRequest {
  name: string
  projectCode?: string
  customerId?: number
  contractAmount: number
  startDate?: string
  endDate?: string
  status?: string
  description?: string
}

export interface MonthlyProfitItem {
  month: string
  income: number
  expense: number
  profit: number
}

export interface ExpenseCategoryItem {
  categoryName: string
  amount: number
  percentage: number
}

export interface ProfitAnalysisResponse {
  monthlyData: MonthlyProfitItem[]
  expenseCategories: ExpenseCategoryItem[]
  totalIncome: number
  totalExpense: number
  totalProfit: number
}
