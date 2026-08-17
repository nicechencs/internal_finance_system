import { createCrudApi } from '@/shared/api/base/crudFactory'
import request from '@/shared/utils/request'
import type { ApiResponse } from '@/shared/utils/request'
import type { Account, AccountStatistics, CreateAccountRequest, UpdateAccountRequest, BalanceTrendResponse } from '@/features/master-data/accounts/types/account'

const api = createCrudApi<Account, CreateAccountRequest, UpdateAccountRequest>({
  baseUrl: '/accounts',
  features: { getActive: true }
})

// 向后兼容具名导出
export const getAccounts = api.getList
export const getAccountById = api.getById
export const createAccount = api.create
export const updateAccount = api.update
export const deleteAccount = api.remove
export const getActiveAccounts = api.getActive

// 特殊业务方法
export const getMaturingAccounts = (days: number = 30) =>
  request<ApiResponse<Account[]>>({ url: '/accounts/maturing', method: 'get', params: { days } })

export const getAccountBalanceTrend = (id: number, months: number = 6) =>
  request<ApiResponse<BalanceTrendResponse>>({ url: `/accounts/${id}/balance-trend`, method: 'get', params: { months } })

export const getAccountStatistics = () =>
  request<ApiResponse<AccountStatistics>>({ url: '/accounts/statistics', method: 'get' })
