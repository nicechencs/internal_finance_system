import request from '@/shared/utils/request'
import type { ApiResponse } from '@/shared/utils/request'
import type {
  DashboardSummary,
  MonthlyStats,
  CategoryStats,
  RecentTransaction
} from '@/features/dashboard/types/dashboard'
import { normalizeRecentTransactions } from '@/features/dashboard/utils/normalizeRecentTransaction'

export const getDashboardSummary = () => {
  return request<ApiResponse<DashboardSummary>>({
    url: '/dashboard/summary',
    method: 'get'
  })
}

export const getMonthlyStats = (months: number = 12) => {
  return request<ApiResponse<MonthlyStats[]>>({
    url: '/dashboard/monthly-stats',
    method: 'get',
    params: { months }
  })
}

export const getExpenseByCategory = (startDate?: string, endDate?: string) => {
  return request<ApiResponse<CategoryStats[]>>({
    url: '/dashboard/expense-by-category',
    method: 'get',
    params: { startDate, endDate }
  })
}

export const getIncomeByCategory = (startDate?: string, endDate?: string) => {
  return request<ApiResponse<CategoryStats[]>>({
    url: '/dashboard/income-by-category',
    method: 'get',
    params: { startDate, endDate }
  })
}

export const getRecentTransactions = (count: number = 10) => {
  return request<ApiResponse<RecentTransaction[]>>({
    url: '/dashboard/recent-transactions',
    method: 'get',
    params: { count }
  }).then((response) => {
    response.data.data = normalizeRecentTransactions(response.data.data as any)
    return response
  })
}
