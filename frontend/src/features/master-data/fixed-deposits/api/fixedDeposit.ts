import request from '@/shared/utils/request'
import type { ApiResponse } from '@/shared/utils/request'
import type {
  FixedDeposit,
  CreateFixedDepositRequest,
  UpdateFixedDepositRequest,
  WithdrawFixedDepositRequest,
  FixedDepositStatistics
} from '@/features/master-data/fixed-deposits/types/fixedDeposit'
import type { Transaction } from '@/features/transactions/types/transaction'

export const createFixedDeposit = (data: CreateFixedDepositRequest) => {
  return request<ApiResponse<FixedDeposit>>({
    url: '/fixed-deposits',
    method: 'post',
    data
  })
}

export const updateFixedDeposit = (id: number, data: UpdateFixedDepositRequest) => {
  return request<ApiResponse<FixedDeposit>>({
    url: `/fixed-deposits/${id}`,
    method: 'put',
    data
  })
}

// 批量查询
export const getFixedDeposits = (params?: {
  accountIds?: number[]
  status?: string
}) => {
  return request.get<ApiResponse<FixedDeposit[]>>('/fixed-deposits', { params })
}

// 统计查询
export const getFixedDepositStatistics = (params?: {
  accountIds?: number[]
  status?: string
}) => {
  return request.get<ApiResponse<FixedDepositStatistics>>('/fixed-deposits/statistics', { params })
}

export const getFixedDepositsByAccount = (accountId: number) => {
  return request<ApiResponse<FixedDeposit[]>>({
    url: `/fixed-deposits/account/${accountId}`,
    method: 'get'
  })
}

export const getFixedDepositById = (id: number) => {
  return request<ApiResponse<FixedDeposit>>({
    url: `/fixed-deposits/${id}`,
    method: 'get'
  })
}

export const withdrawFixedDeposit = (id: number, data: WithdrawFixedDepositRequest) => {
  return request<ApiResponse<FixedDeposit>>({
    url: `/fixed-deposits/${id}/withdraw`,
    method: 'post',
    data
  })
}

export const getMaturingFixedDeposits = (days: number = 30) => {
  return request<ApiResponse<FixedDeposit[]>>({
    url: '/fixed-deposits/maturing',
    method: 'get',
    params: { days }
  })
}

export const deleteFixedDeposit = (id: number) => {
  return request<ApiResponse<null>>({
    url: `/fixed-deposits/${id}`,
    method: 'delete'
  })
}

export const getWithdrawalCandidates = (id: number) => {
  return request<ApiResponse<Transaction[]>>({
    url: `/fixed-deposits/${id}/withdrawal-candidates`,
    method: 'get'
  })
}
