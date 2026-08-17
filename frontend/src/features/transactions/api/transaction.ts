import request from '@/shared/utils/request'
import type { ApiResponse } from '@/shared/utils/request'
import type { PageRequest, PageResponse } from '@/shared/types/common'
import type {
  Transaction,
  CreateTransactionRequest,
  UpdateTransactionRequest,
  CreateTransferRequest,
  ConvertTransactionToTransferRequest,
  TransferResult,
  TransactionStatistics,
  RelatedFinanceRecord
} from '@/features/transactions/types/transaction'
import {
  normalizeTransaction,
  normalizeTransactions
} from '@/features/transactions/utils/normalizeTransaction'

export const getTransactions = (params: PageRequest) => {
  return request<ApiResponse<PageResponse<Transaction>>>({
    url: '/transactions',
    method: 'get',
    params
  }).then((response) => {
    response.data.data.items = normalizeTransactions(response.data.data.items)
    return response
  })
}

export const getTransactionById = (id: number) => {
  return request<ApiResponse<Transaction>>({
    url: `/transactions/${id}`,
    method: 'get'
  }).then((response) => {
    response.data.data = normalizeTransaction(response.data.data)
    return response
  })
}

export const createTransaction = (data: CreateTransactionRequest) => {
  return request<ApiResponse<Transaction>>({
    url: '/transactions',
    method: 'post',
    data
  }).then((response) => {
    response.data.data = normalizeTransaction(response.data.data)
    return response
  })
}

export const updateTransaction = (id: number, data: UpdateTransactionRequest) => {
  return request<ApiResponse<Transaction>>({
    url: `/transactions/${id}`,
    method: 'put',
    data
  }).then((response) => {
    response.data.data = normalizeTransaction(response.data.data)
    return response
  })
}

export const deleteTransaction = (id: number) => {
  return request<ApiResponse<void>>({
    url: `/transactions/${id}`,
    method: 'delete'
  })
}

export const getTransactionsByAccount = (accountId: number) => {
  return request<ApiResponse<Transaction[]>>({
    url: `/transactions/by-account/${accountId}`,
    method: 'get'
  }).then((response) => {
    response.data.data = normalizeTransactions(response.data.data)
    return response
  })
}

export const getTransactionsByProject = (projectId: number) => {
  return request<ApiResponse<Transaction[]>>({
    url: `/transactions/by-project/${projectId}`,
    method: 'get'
  }).then((response) => {
    response.data.data = normalizeTransactions(response.data.data)
    return response
  })
}

export const getTransactionsByCategory = (categoryId: number) => {
  return request<ApiResponse<Transaction[]>>({
    url: `/transactions/by-category/${categoryId}`,
    method: 'get'
  }).then((response) => {
    response.data.data = normalizeTransactions(response.data.data)
    return response
  })
}

export const getTransactionsByCustomer = (customerId: number) => {
  return request<ApiResponse<Transaction[]>>({
    url: `/transactions/by-customer/${customerId}`,
    method: 'get'
  }).then((response) => {
    response.data.data = normalizeTransactions(response.data.data)
    return response
  })
}

export const getTransactionsBySupplier = (supplierId: number) => {
  return request<ApiResponse<Transaction[]>>({
    url: `/transactions/by-supplier/${supplierId}`,
    method: 'get'
  }).then((response) => {
    response.data.data = normalizeTransactions(response.data.data)
    return response
  })
}

export const getTransactionsByPerson = (personId: number) => {
  return request<ApiResponse<Transaction[]>>({
    url: `/transactions/by-person/${personId}`,
    method: 'get'
  }).then((response) => {
    response.data.data = normalizeTransactions(response.data.data)
    return response
  })
}

export const getAccountBalance = (accountId: number) => {
  return request<ApiResponse<number>>({
    url: `/transactions/account-balance/${accountId}`,
    method: 'get'
  })
}

export const createTransfer = (data: CreateTransferRequest) => {
  return request<ApiResponse<TransferResult>>({
    url: '/transactions/transfer',
    method: 'post',
    data
  }).then((response) => {
    response.data.data = {
      outTransaction: normalizeTransaction(response.data.data.outTransaction),
      inTransaction: normalizeTransaction(response.data.data.inTransaction),
      fixedDepositLinkage: response.data.data.fixedDepositLinkage
    }
    return response
  })
}

export const getTransferCandidates = (transactionId: number, targetAccountId: number) => {
  return request<ApiResponse<Transaction[]>>({
    url: `/transactions/${transactionId}/transfer-candidates`,
    method: 'get',
    params: { targetAccountId }
  }).then((response) => {
    response.data.data = normalizeTransactions(response.data.data)
    return response
  })
}

export const convertTransactionToTransfer = (transactionId: number, data: ConvertTransactionToTransferRequest) => {
  return request<ApiResponse<TransferResult>>({
    url: `/transactions/${transactionId}/convert-to-transfer`,
    method: 'post',
    data
  }).then((response) => {
    response.data.data = {
      outTransaction: normalizeTransaction(response.data.data.outTransaction),
      inTransaction: normalizeTransaction(response.data.data.inTransaction)
    }
    return response
  })
}

/**
 * 获取交易统计数据（支持筛选参数）
 * @param params - 可选的筛选参数（accountId, categoryId, projectId, startDate, endDate, transactionType）
 * @returns 包含总收入、总支出、净利润等统计信息
 */
export const getTransactionStatistics = (params?: Record<string, any>) => {
  return request<ApiResponse<TransactionStatistics>>({
    url: '/transactions/statistics',
    method: 'get',
    params
  })
}

/**
 * 获取指定账户的交易统计数据
 * @param accountId - 账户 ID
 * @returns 该账户的交易统计信息
 */
export const getAccountTransactionStatistics = (accountId: number) => {
  return request<ApiResponse<TransactionStatistics>>({
    url: `/transactions/account/${accountId}/statistics`,
    method: 'get'
  })
}

/**
 * 获取指定客户的交易统计数据
 * @param customerId - 客户 ID
 * @returns 该客户的交易统计信息（通常为收入类型交易）
 */
export const getCustomerTransactionStatistics = (customerId: number) => {
  return request<ApiResponse<TransactionStatistics>>({
    url: `/transactions/customer/${customerId}/statistics`,
    method: 'get'
  })
}

/**
 * 获取指定供应商的交易统计数据
 * @param supplierId - 供应商 ID
 * @returns 该供应商的交易统计信息
 */
export const getSupplierTransactionStatistics = (supplierId: number) => {
  return request<ApiResponse<TransactionStatistics>>({
    url: `/transactions/supplier/${supplierId}/statistics`,
    method: 'get'
  })
}

/**
 * 获取指定人员的交易统计数据
 * @param personId - 人员 ID
 * @returns 该人员的交易统计信息
 */
export const getPersonTransactionStatistics = (personId: number) => {
  return request<ApiResponse<TransactionStatistics>>({
    url: `/transactions/person/${personId}/statistics`,
    method: 'get'
  })
}

export const getRelatedFinanceRecords = (transactionId: number) => {
  return request<ApiResponse<RelatedFinanceRecord>>({
    url: `/transactions/${transactionId}/related`,
    method: 'get'
  })
}

/**
 * 获取可用于应收款绑定的收入交易
 */
export async function getAvailableTransactionsForReceivable(params?: {
  projectId?: number
  customerId?: number
  supplierId?: number
  personId?: number
  showAll?: boolean
  keyword?: string
}): Promise<Transaction[]> {
  const response = await request.get<ApiResponse<Transaction[]>>('/transactions/available-for-receivable', {
    params
  })
  return normalizeTransactions(response.data.data)
}

/**
 * 获取可用于应付款绑定的支出交易
 */
export async function getAvailableTransactionsForPayable(params?: {
  projectId?: number
  supplierId?: number
  customerId?: number
  personId?: number
  showAll?: boolean
  keyword?: string
}): Promise<Transaction[]> {
  const response = await request.get<ApiResponse<Transaction[]>>('/transactions/available-for-payable', {
    params
  })
  return normalizeTransactions(response.data.data)
}

