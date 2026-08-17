import { createCrudApi } from '@/shared/api/base/crudFactory'
import request from '@/shared/utils/request'
import type { ApiResponse } from '@/shared/utils/request'
import type { PageResponse } from '@/shared/types/common'
import type {
  Receivable,
  ReceivableSummary,
  ReceivableTrend,
  ReceivableAging,
  ReceivableStatistics,
  CreateReceivableRequest,
  UpdateReceivableRequest,
  ReceivePaymentRequest,
  ReceivableType,
  CreateReceivableTypeRequest,
  UpdateReceivableTypeRequest
} from '@/features/finance/types/receivable'

const api = createCrudApi<Receivable, CreateReceivableRequest, UpdateReceivableRequest>({
  baseUrl: '/receivables',
})

// 向后兼容具名导出
export const getReceivables = api.getList
export const getReceivableById = api.getById
export const createReceivable = api.create
export const updateReceivable = api.update
export const deleteReceivable = api.remove

// 特殊业务方法
export const receivePayment = (id: number, data: ReceivePaymentRequest) =>
  request<ApiResponse<Receivable>>({ url: `/receivables/${id}/receive`, method: 'post', data })

export const getReceivableSummary = () =>
  request<ApiResponse<ReceivableSummary>>({ url: '/receivables/summary', method: 'get' })

export const getReceivableTrend = (params?: { startDate?: string; endDate?: string }) =>
  request<ApiResponse<ReceivableTrend>>({ url: '/receivables/trend', method: 'get', params })

export const getReceivableAging = () =>
  request<ApiResponse<ReceivableAging>>({ url: '/receivables/aging', method: 'get' })

export const getReceivablesByProject = (projectId: number) =>
  request<ApiResponse<Receivable[]>>({ url: `/receivables/project/${projectId}`, method: 'get' })

export const getReceivablesByCustomer = (customerId: number) =>
  request<ApiResponse<Receivable[]>>({ url: `/receivables/customer/${customerId}`, method: 'get' })

export const getReceivablesBySupplier = (supplierId: number) =>
  request<ApiResponse<Receivable[]>>({ url: `/receivables/supplier/${supplierId}`, method: 'get' })

export const getReceivablesByPerson = (personId: number) =>
  request<ApiResponse<Receivable[]>>({ url: `/receivables/person/${personId}`, method: 'get' })

export const getReceivableStatistics = (params?: Record<string, any>) =>
  request<ApiResponse<ReceivableStatistics>>({
    url: '/receivables/statistics',
    method: 'get',
    params
  })

export const getAvailableReceivablesForTransaction = (transactionId: number, keyword?: string) =>
  request<ApiResponse<Receivable[]>>({
    url: '/receivables/available-for-transaction',
    method: 'get',
    params: { transactionId, keyword }
  })

// 应收款业务类型管理
export const getReceivableTypesPaged = (params?: { page?: number; pageSize?: number; name?: string; isActive?: boolean }) =>
  request<ApiResponse<PageResponse<ReceivableType>>>({
    url: '/receivable-types',
    method: 'get',
    params: params || { page: 1, pageSize: 200 }
  })

export const getReceivableTypes = () =>
  request<ApiResponse<ReceivableType[]>>({ url: '/receivable-types/active', method: 'get' })

export const createReceivableType = (data: CreateReceivableTypeRequest) =>
  request<ApiResponse<ReceivableType>>({ url: '/receivable-types', method: 'post', data })

export const updateReceivableType = (id: number, data: UpdateReceivableTypeRequest) =>
  request<ApiResponse<ReceivableType>>({ url: `/receivable-types/${id}`, method: 'put', data })

export const deleteReceivableType = (id: number) =>
  request<ApiResponse<void>>({ url: `/receivable-types/${id}`, method: 'delete' })
