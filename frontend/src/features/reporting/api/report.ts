import request from '@/shared/utils/request'
import type { ApiResponse } from '@/shared/utils/request'
import type {
  MonthlyProfitReport,
  CashflowReport,
  ProjectProfitReport,
  PersonCostReport,
  SupplierExpenseReport,
  AnnualOverviewReport
} from '@/features/reporting/types/report'

// 获取月度利润报表
export const getMonthlyProfitReport = (year: number, month: number) => {
  return request<ApiResponse<MonthlyProfitReport>>({
    url: '/reports/monthly-profit',
    method: 'get',
    params: { year, month }
  })
}

// 获取现金流报表
export const getCashflowReport = (startDate: string, endDate: string) => {
  return request<ApiResponse<CashflowReport>>({
    url: '/reports/cashflow',
    method: 'get',
    params: { startDate, endDate }
  })
}

// 获取项目利润报表
export const getProjectProfitReport = () => {
  return request<ApiResponse<ProjectProfitReport>>({
    url: '/reports/project-profit',
    method: 'get'
  })
}

// 获取人员成本报表
export const getPersonCostReport = () => {
  return request<ApiResponse<PersonCostReport>>({
    url: '/reports/person-cost',
    method: 'get'
  })
}

// 获取供应商费用报表
export const getSupplierExpenseReport = () => {
  return request<ApiResponse<SupplierExpenseReport>>({
    url: '/reports/supplier-expense',
    method: 'get'
  })
}

// 获取年度总览报表
export const getAnnualOverviewReport = (year: number) => {
  return request<ApiResponse<AnnualOverviewReport>>({
    url: '/reports/annual-overview',
    method: 'get',
    params: { year }
  })
}

